using Old8Lang.Error;
using Old8Lang.AST.Expression.Value;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 函数调用表达式，用于处理 expression(arguments) 语法
/// 例如：cla[0]("World"), obj.method()(args)
/// 支持命名参数：func(a: 1, b: 2) 或混合使用：func(1, b: 2)
/// </summary>
public partial class FunctionCallExpression : LangExpression
{
    /// <summary>
    /// 要调用的函数表达式
    /// </summary>
    public readonly LangExpression FunctionExpression;

    /// <summary>
    /// 函数调用参数列表（位置参数）
    /// </summary>
    public readonly List<LangExpression> Arguments;

    /// <summary>
    /// 命名参数列表
    /// </summary>
    public readonly List<NamedArgument> NamedArguments;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="functionExpression">要调用的函数表达式</param>
    /// <param name="arguments">函数调用参数列表</param>
    /// <param name="position">位置信息</param>
    public FunctionCallExpression(LangExpression functionExpression, List<LangExpression> arguments,
        SourcePosition position = default)
        : base(position)
    {
        FunctionExpression = functionExpression;
        Arguments = arguments;
        NamedArguments = [];
    }

    /// <summary>
    /// 构造函数（支持命名参数）
    /// </summary>
    /// <param name="functionExpression">要调用的函数表达式</param>
    /// <param name="arguments">位置参数列表</param>
    /// <param name="namedArguments">命名参数列表</param>
    /// <param name="position">位置信息</param>
    public FunctionCallExpression(LangExpression functionExpression, List<LangExpression> arguments,
        List<NamedArgument> namedArguments, SourcePosition position = default)
        : base(position)
    {
        FunctionExpression = functionExpression;
        Arguments = arguments;
        NamedArguments = namedArguments;
    }

    public override string ToString()
    {
        var args = new List<string>();
        args.AddRange(Arguments.Select(arg => arg.ToString())!);
        args.AddRange(NamedArguments.Select(na => na.ToString()));
        var argsStr = string.Join(", ", args);
        return $"{FunctionExpression}({argsStr})";
    }

    /// <summary>
    /// 调用运行期才可确定类型的函数值。
    /// </summary>
    /// <param name="callable">待调用的函数值。</param>
    /// <param name="arguments">位置参数。</param>
    /// <returns>委托调用结果。</returns>
    /// <exception cref="InvalidOperationException">当值不是委托时抛出。</exception>
    public static object? InvokeDynamicDelegate(object? callable, object?[] arguments)
    {
        if (callable is not Delegate function)
        {
            throw new InvalidOperationException("表达式结果不是可调用的函数值");
        }

        return function.DynamicInvoke(arguments);
    }
}
