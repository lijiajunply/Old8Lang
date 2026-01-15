using Old8Lang.AST.Expression;

namespace Old8Lang.Generators;

/// <summary>
/// 生成器执行上下文（新架构）
/// 用于在生成器执行期间保存和恢复状态，替代全局的IsYield和IsInGenerator标志
/// 参考C#的生成器状态机设计，每个生成器实例都有独立的执行上下文
///
/// 新架构基于路径的状态恢复机制，避免依赖全局索引
/// </summary>
public class GeneratorExecutionContext
{
    /// <summary>
    /// 当前执行路径
    /// 记录从函数体根节点到当前执行位置的完整路径
    /// 例如："/block[0]/for-in/block[1]/yield"
    /// 用于在yield后精确恢复到上次执行位置
    /// </summary>
    public string? ExecutionPath { get; set; }

    /// <summary>
    /// 循环状态字典
    /// Key: 循环路径（如 "/block[0]/for-in"）
    /// Value: 当前迭代的索引
    /// 用于在for-in和while循环中保存和恢复迭代位置
    /// </summary>
    public Dictionary<string, int> LoopStates { get; set; } = new();

    /// <summary>
    /// 异步流缓存字典
    /// Key: 循环路径（如 "/block[0]/async-for-in"）
    /// Value: 缓存的异步流实例
    /// 用于在生成器上下文中保存异步流实例，避免重复创建
    /// </summary>
    public Dictionary<string, object> AsyncStreamCache { get; set; } = new();

    /// <summary>
    /// 是否遇到了yield语句
    /// </summary>
    public bool HasYielded { get; set; }

    /// <summary>
    /// 当前yield的值
    /// </summary>
    public LangValueType? CurrentValue { get; set; }

    /// <summary>
    /// 是否已完成（遇到return或执行完所有语句）
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 当前执行的路径栈
    /// 用于在执行过程中构建完整路径
    /// 例如：执行到 for-in 循环时会 push "/for-in"，退出时 pop
    /// </summary>
    public Stack<string> PathStack { get; set; } = new();

    /// <summary>
    /// 重置上下文状态
    /// </summary>
    public void Reset()
    {
        HasYielded = false;
        CurrentValue = null;
        IsCompleted = false;
        ExecutionPath = null;
        LoopStates.Clear();
        AsyncStreamCache.Clear();
        PathStack.Clear();
    }

    /// <summary>
    /// 构建当前完整路径
    /// </summary>
    /// <returns>完整的执行路径</returns>
    public string GetCurrentPath()
    {
        return string.Join("", PathStack.Reverse());
    }

    /// <summary>
    /// 检查当前路径是否匹配目标路径
    /// </summary>
    /// <param name="targetPath">目标路径</param>
    /// <returns>如果当前路径是目标路径的前缀，返回true</returns>
    public bool IsPathMatch(string targetPath)
    {
        if (string.IsNullOrEmpty(ExecutionPath))
            return false;

        var currentPath = GetCurrentPath();
        return targetPath.StartsWith(currentPath);
    }

    /// <summary>
    /// 检查是否应该在当前路径恢复执行
    /// </summary>
    /// <returns>如果当前路径等于执行路径，返回true</returns>
    public bool ShouldResumeHere()
    {
        if (string.IsNullOrEmpty(ExecutionPath))
            return false;

        return GetCurrentPath() == ExecutionPath;
    }
}
