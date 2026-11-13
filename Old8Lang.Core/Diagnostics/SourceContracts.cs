using Old8Lang.Error;

namespace Old8Lang;

/// <summary>
/// 提供源代码位置的对象契约。
/// </summary>
/// <remarks>
/// 核心诊断只依赖此契约；AST 可在上层扩展它来提供 Visitor 行为。
/// </remarks>
public interface ISourceLocated
{
    /// <summary>
    /// 源代码位置。
    /// </summary>
    SourcePosition Position { get; }
}

/// <summary>
/// 为诊断提供源代码上下文窗口的宿主契约。
/// </summary>
public interface ISourceContextProvider
{
    /// <summary>
    /// 获取指定位置附近的源代码上下文。
    /// </summary>
    SourceContextWindow GetSourceContextWindow(SourcePosition position);
}
