using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 操作表达式类，用于表示各种运算符和操作
/// </summary>
/// <param name="left">左操作数</param>
/// <param name="opera">运算符类型</param>
/// <param name="right">右操作数</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类是Old8Lang表达式系统的核心组件，用于表示各种运算符和操作，包括：
/// - 算术运算符：+, -, *, /, %, ^
/// - 比较运算符：>, &lt;, ==, !=, &gt;=, >=
/// - 逻辑运算符：&&, ||, ^
/// - 点运算符：.
/// - 类型转换运算符：as
/// - 成员访问和方法调用
/// - 索引访问
/// - 一元运算符：!, -
/// </remarks>
public partial class Operation(
    LangExpression? left,
    LangTokenType opera,
    LangExpression? right,
    SourcePosition position = default)
    : LangExpression(position)
{

    /// <summary>
    /// 将运算符类型转换为字符串表示
    /// </summary>
    /// <returns>运算符的字符串表示</returns>
    internal string OperaToString()
    {
        if (Opera == LangTokenType.Plus)
            return "+";
        if (Opera == LangTokenType.Minus)
            return "-";
        if (Opera == LangTokenType.Star)
            return "*";
        if (Opera == LangTokenType.Slash)
            return "/";
        if (Opera == LangTokenType.Caret)
            return "^";
        if (Opera == LangTokenType.GreaterThan)
            return ">";
        if (Opera == LangTokenType.LessThan)
            return "<";
        if (Opera == LangTokenType.Equals)
            return "==";
        if (Opera == LangTokenType.NotEquals)
            return "!=";
        if (Opera == LangTokenType.Dot)
            return ".";
        if (Opera == LangTokenType.Percent)
            return "%";
        if (Opera == LangTokenType.LessThanEquals)
            return "<=";
        if (Opera == LangTokenType.GreaterThanEquals)
            return ">=";
        if (Opera == LangTokenType.Xor)
            return "^";
        if (Opera == LangTokenType.And)
            return "&&";
        if (Opera == LangTokenType.Or)
            return "||";
        if (Opera == LangTokenType.As)
            return "as";
        if (Opera == LangTokenType.Is)
            return "is";
        if (Opera == LangTokenType.IsNot)
            return "is not";
        if (Opera == LangTokenType.In)
            return "in";
        if (Opera == LangTokenType.NullishCoalescing)
            return "??";
        if (Opera == LangTokenType.Not)
            return "not";
        return "";
    }

    /// <summary>
    /// 将操作转换为字符串表示
    /// </summary>
    /// <returns>操作的字符串表示</returns>
    public override string ToString() => $"{Left}{OperaToString()}{Right}";

    /// <summary>
    /// 操作的输出类型缓存
    /// </summary>
    public Type? Type { get; internal set; }

    /// <summary>
    /// 左操作数
    /// </summary>
    public LangExpression? Left { get; } = left;

    /// <summary>
    /// 右操作数
    /// </summary>
    public LangExpression? Right { get; } = right;

    /// <summary>
    /// 运算符类型
    /// </summary>
    public LangTokenType Opera { get; } = opera;

    internal static bool IsNumericType(Type type) => type == typeof(int) || type == typeof(double) ||
                                                     type == typeof(float) || type == typeof(long) ||
                                                     type == typeof(short) || type == typeof(byte);

    internal Type OutputType(ILGenerator ilGenerator, LocalManager local)
    {
        if (Opera == LangTokenType.Dot && StandardLibraryILHelper.TryResolve(Left, Right, local, out var library, out var libraryCall))
            return StandardLibraryILHelper.EmitCall(library, libraryCall, ilGenerator, local);

        var leftType = Left?.Accept(new TypeInferenceVisitor(local));
        var rightType = Right?.Accept(new TypeInferenceVisitor(local));

        // 如果leftType是TypeBuilder，需要查找对应的已完成的类型
        if (leftType is TypeBuilder typeBuilder && typeBuilder != local.InClassEnv)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = typeBuilder.Name;
            leftType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        // 如果rightType是TypeBuilder，需要查找对应的已完成的类型
        if (rightType is TypeBuilder rightTypeBuilder)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = rightTypeBuilder.Name;
            rightType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        if (Left is null)
        {
            // 处理单目运算符
            switch (Opera)
            {
                case LangTokenType.Exclamation:
                    Right?.Accept(new CompilerVisitor(ilGenerator, local));
                    ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
                    ilGenerator.Emit(OpCodes.Xor); // 进行异或运算
                    return typeof(bool);
                case LangTokenType.Minus:
                    Right?.Accept(new CompilerVisitor(ilGenerator, local));
                    if (rightType == typeof(object))
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                        rightType = typeof(int);
                    }
                    ilGenerator.Emit(OpCodes.Neg);
                    return rightType ?? throw new InvalidOperationError(this, "右操作数不能为空");

                default:
                    throw new InvalidOperationError(this, $"不支持的一元运算符: {Opera}");
            }
        }

        switch (Opera)
        {
            case LangTokenType.Plus:
                if (leftType == typeof(string) || rightType == typeof(string))
                {
                    // 处理字符串连接 - 需要调用string.Concat方法
                    // 加载左操作数并立即装箱（如果需要）
                    Left?.Accept(new CompilerVisitor(ilGenerator, local));
                    if (leftType != typeof(string))
                    {
                        ilGenerator.Emit(OpCodes.Box, leftType!);
                    }

                    // 加载右操作数并立即装箱（如果需要）
                    Right?.Accept(new CompilerVisitor(ilGenerator, local));
                    if (rightType != typeof(string))
                    {
                        ilGenerator.Emit(OpCodes.Box, rightType!);
                    }

                    // 调用string.Concat(object, object)
                    var concatMethod = typeof(string).GetMethod("Concat", [typeof(object), typeof(object)])!;
                    ilGenerator.Emit(OpCodes.Call, concatMethod);
                    return typeof(string);
                }

                // 处理数值类型加法
                return NumericBinaryOpHelper.GenerateAddition(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Minus:
                return NumericBinaryOpHelper.GenerateSubtraction(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Star:
                return NumericBinaryOpHelper.GenerateMultiplication(Left, Right, ilGenerator, local, leftType, rightType, this);
            case LangTokenType.Slash:
                return NumericBinaryOpHelper.GenerateDivision(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Percent:
                return NumericBinaryOpHelper.GenerateModulo(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Caret:
                return NumericBinaryOpHelper.GeneratePower(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.GreaterThan:
                return ComparisonOpHelper.GenerateGreaterThan(Left, Right, ilGenerator, local);
            case LangTokenType.LessThan:
                return ComparisonOpHelper.GenerateLessThan(Left, Right, ilGenerator, local);
            case LangTokenType.Equals:
                return ComparisonOpHelper.GenerateEquals(Left, Right, ilGenerator, local);
            case LangTokenType.NotEquals:
                return ComparisonOpHelper.GenerateNotEquals(Left, Right, ilGenerator, local);
            case LangTokenType.And:
                return LogicalOpILHelper.GenerateAnd(Left!, Right!, ilGenerator, local);
            case LangTokenType.Or:
                return LogicalOpILHelper.GenerateOr(Left!, Right!, ilGenerator, local);
            case LangTokenType.Xor:
                return LogicalOpILHelper.GenerateXor(Left!, Right!, ilGenerator, local);
            case LangTokenType.LessThanEquals:
                return ComparisonOpHelper.GenerateLessThanEquals(Left, Right, ilGenerator, local);
            case LangTokenType.GreaterThanEquals:
                return ComparisonOpHelper.GenerateGreaterThanEquals(Left, Right, ilGenerator, local);
            case LangTokenType.In:
                return InOperatorHelper.GenerateInOperator(Left!, Right!, ilGenerator, local, rightType, this);
            case LangTokenType.As:
                return TypeCheckILHelper.GenerateAsOperator(Left!, Right!, ilGenerator, local, leftType, this);
            case LangTokenType.Is:
                return TypeCheckILHelper.GenerateIsOperator(Left!, Right!, ilGenerator, local, leftType);
            case LangTokenType.IsNot:
                return TypeCheckILHelper.GenerateIsNotOperator(Left!, Right!, ilGenerator, local, leftType);
            case LangTokenType.NullishCoalescing:
                return NullishCoalescingILHelper.GenerateNullishCoalescing(Left!, Right!, ilGenerator, local, leftType!);
            case LangTokenType.Dot:
                return DotOperatorILHelper.GenerateDotOperator(Left, Right, ilGenerator, local, leftType, rightType, this);
            default:
                throw new InvalidOperationError(this, $"不支持的二元运算符: {Opera}");
        }
    }

    /// <summary>
    /// 检查值是否是指定类型的实例
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="typeName">类型名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>如果值是指定类型的实例则返回true,否则返回false</returns>
    internal bool CheckIsInstance(LangValueType value, string typeName, VariateManager manager)
    {
        // 处理基本类型
        switch (typeName.ToLower())
        {
            case "int":
                return value is IntLangValue;
            case "double":
                return value is DoubleLangValue;
            case "string":
                return value is StringLangValue;
            case "bool":
                return value is BoolLangValue;
            case "char":
                return value is CharLangValue;
            case "list":
                return value is ListLangValue;
            case "array":
                return value is ArrayLangValue;
            case "dictionary":
                return value is DictionaryLangValue;
            case "null":
                return value is NullLangValue;
        }

        // 处理类和接口类型
        if (value is AnyLangValue anyValue)
        {
            // 检查是否是该类的实例(类名匹配)
            if (anyValue.ClassId.IdName == typeName)
            {
                return true;
            }

            // 检查是否实现了指定的接口或继承了指定的类
            // 通过元数据的 IsAssignableTo 方法进行检查
            try
            {
                var targetType = manager.GetAny(new LangId(typeName));
                if (targetType is TypeTemplate targetTypeTemplate)
                {
                    var targetMetadata = targetTypeTemplate.BuildMetadata(manager);
                    return anyValue.Metadata.IsAssignableTo(targetMetadata, manager);
                }
            }
            catch
            {
                // 类型不存在,返回false
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取内置类型方法的返回类型
    /// </summary>
    /// <param name="leftType">左操作数类型（调用方法的对象类型）</param>
    /// <param name="methodName">方法名</param>
    /// <param name="paramCount">参数数量</param>
    /// <returns>方法的返回类型，如果不是内置方法则返回null</returns>
    internal static Type? GetBuiltInMethodReturnType(Type? leftType, string methodName, int paramCount)
    {
        if (leftType is null) return null;

        // 处理无参数的内置方法
        if (paramCount == 0)
        {
            // 通用类型转换方法
            switch (methodName)
            {
                case "ToInt":
                    // int, double, bool, char 都有 ToInt() 方法
                    if (leftType == typeof(int) || leftType == typeof(double) ||
                        leftType == typeof(bool) || leftType == typeof(char))
                        return typeof(int);
                    break;
                case "ToDouble":
                    // int, double, bool 都有 ToDouble() 方法
                    if (leftType == typeof(int) || leftType == typeof(double) || leftType == typeof(bool))
                        return typeof(double);
                    break;
                case "ToBool":
                    // int, double, bool 都有 ToBool() 方法
                    if (leftType == typeof(int) || leftType == typeof(double) || leftType == typeof(bool))
                        return typeof(bool);
                    break;
                case "ToChar":
                    // int, char 都有 ToChar() 方法
                    if (leftType == typeof(int) || leftType == typeof(char))
                        return typeof(char);
                    break;
            }

            // char 类型特有的方法
            if (leftType == typeof(char))
            {
                switch (methodName)
                {
                    case "ToUpper":
                    case "ToLower":
                        return typeof(char);
                    case "IsDigit":
                    case "IsLetter":
                    case "IsWhiteSpace":
                    case "IsUpper":
                    case "IsLower":
                    case "IsLetterOrDigit":
                    case "IsPunctuation":
                    case "IsSymbol":
                    case "IsControl":
                        return typeof(bool);
                    case "GetNumericValue":
                        return typeof(double);
                }
            }

            // string 类型的方法
            if (leftType == typeof(string))
            {
                switch (methodName)
                {
                    case "ToUpper":
                    case "ToLower":
                    case "Trim":
                    case "TrimStart":
                    case "TrimEnd":
                        return typeof(string);
                    case "ToInt":
                        return typeof(int);
                    case "ToDouble":
                        return typeof(double);
                    case "ToBool":
                        return typeof(bool);
                }
            }

            // List 类型的方法
            if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>))
            {
                switch (methodName)
                {
                    case "Count":
                        return typeof(int);
                    case "ToArray":
                        return leftType.GetGenericArguments()[0].MakeArrayType();
                }
            }

            // Array 类型的方法
            if (leftType.IsArray)
            {
                switch (methodName)
                {
                    case "Length":
                        return typeof(int);
                    case "ToList":
                        return typeof(List<>).MakeGenericType(leftType.GetElementType()!);
                }
            }
        }

        // 处理带参数的内置方法
        if (paramCount == 1)
        {
            // char 类型的 CompareTo 方法
            if (leftType == typeof(char) && methodName == "CompareTo")
            {
                return typeof(int);
            }

            // string 类型的方法
            if (leftType == typeof(string))
            {
                switch (methodName)
                {
                    case "Contains":
                    case "StartsWith":
                    case "EndsWith":
                        return typeof(bool);
                    case "IndexOf":
                    case "LastIndexOf":
                        return typeof(int);
                    case "Split":
                        return typeof(string[]);
                    case "Replace":
                        return typeof(string);
                    case "Substring":
                        return typeof(string);
                }
            }
        }

        if (paramCount == 2)
        {
            if (leftType == typeof(string))
            {
                switch (methodName)
                {
                    case "Replace":
                    case "Substring":
                        return typeof(string);
                }
            }
        }

        return null;
    }
}
