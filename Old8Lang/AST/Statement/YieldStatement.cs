using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// yield语句，用于生成器函数中暂停执行并返回值
/// </summary>
public class YieldStatement(LangExpression yieldExpression, SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// yield表达式
    /// </summary>
    public LangExpression YieldExpression { get; init; } = yieldExpression;

    /// <summary>
    /// 运行yield语句
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public override void Run(VariateManager manager)
    {
        // 计算yield表达式的值
        var yieldValue = YieldExpression.Run(manager);

        // 检查是否有生成器上下文
        var genContext = manager.GeneratorContext;
        if (genContext != null)
        {
            // 新架构：通过生成器上下文设置yield值和标志
            genContext.CurrentValue = yieldValue;
            genContext.HasYielded = true;
        }
        else
        {
            // 旧架构（向后兼容）：使用全局标志
            manager.Result = yieldValue;
            manager.IsYield = true;
        }
    }

    /// <summary>
    /// 生成IL代码
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 生成器的IL代码生成（后续实现）
        // 这里需要实现状态机逻辑
    }

    /// <summary>
    /// 获取子语句
    /// </summary>
    /// <param name="index">索引</param>
    /// <returns>子语句</returns>
    public override OldStatement? this[int index] => null;

    /// <summary>
    /// 获取语句数量
    /// </summary>
    public override int Count => 0;

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>yield语句的字符串表示</returns>
    public override string ToString() => $"yield {YieldExpression}";
}