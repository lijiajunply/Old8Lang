using Old8Lang.Error;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using System.Reflection.Emit;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 函数调用表达式，用于处理 expression(arguments) 语法
/// 例如：cla[0]("World"), obj.method()(args)
/// </summary>
public class FunctionCallExpression : LangExpression
{
    /// <summary>
    /// 要调用的函数表达式
    /// </summary>
    public readonly LangExpression FunctionExpression;

    /// <summary>
    /// 函数调用参数列表
    /// </summary>
    public readonly List<LangExpression> Arguments;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="functionExpression">要调用的函数表达式</param>
    /// <param name="arguments">函数调用参数列表</param>
    /// <param name="position">位置信息</param>
    public FunctionCallExpression(LangExpression functionExpression, List<LangExpression> arguments, SourcePosition position = default)
        : base(position)
    {
        FunctionExpression = functionExpression;
        Arguments = arguments;
    }

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        // 1. 运行函数表达式获取函数对象
        var functionValue = FunctionExpression.Run(manager);

        // 2. 检查获取到的是否为函数
        if (functionValue is not FuncLangValue func)
        {
            throw new InvalidOperationError(this, $"表达式 '{FunctionExpression}' 的结果不是函数，无法调用");
        }

        // 3. 调用函数，传入表达式列表而不是值列表
        return func.Run(manager, Arguments);
    }

    public override string ToString()
    {
        var argsStr = string.Join(", ", Arguments.Select(arg => arg.ToString()));
        return $"{FunctionExpression}({argsStr})";
    }

    public override Type? OutputType(LocalManager local)
    {
        // 编译器模式下的类型推断
        // 由于函数表达式可能很复杂，暂时返回 object 类型
        // 实际的编译器实现会更复杂，需要分析函数表达式的返回类型
        return typeof(object);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器模式下的IL生成
        // 这是一个简化实现，实际需要更复杂的处理逻辑

        // 1. 生成函数表达式的IL代码
        FunctionExpression.LoadIlValue(ilGenerator, local);

        // 2. 生成参数的IL代码
        foreach (var arg in Arguments)
        {
            arg.LoadIlValue(ilGenerator, local);
        }

        // 3. 由于函数调用的复杂性，暂时抛出异常提示不支持
        throw new InvalidOperationError(this, "编译器模式下暂时不支持表达式函数调用，请使用解释器模式");
    }
}