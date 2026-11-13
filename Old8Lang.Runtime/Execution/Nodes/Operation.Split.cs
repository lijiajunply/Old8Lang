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
/// Operation 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class Operation
{
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
}
