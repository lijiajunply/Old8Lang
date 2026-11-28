using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Generators;

/// <summary>
/// GeneratorLangValue 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class GeneratorLangValue
{
    /// <summary>
    /// 作为可调用对象执行，返回下一个值
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="args">参数列表（生成器调用不需要参数）</param>
    /// <param name="obj">对象实例（生成器调用不需要）</param>
    /// <returns>生成器的下一个值</returns>
    public LangValueType Run(VariateManager manager, List<LangExpression> args, object? obj = null)
    {
        // 生成器调用不需要参数，忽略args
        return Accept(RuntimeVisitors.CreateInterpreter(manager));
    }
}
