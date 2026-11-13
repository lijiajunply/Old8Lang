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
