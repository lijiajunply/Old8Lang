using Old8Lang;

namespace Old8Lang.AST;

public interface IOldLangTree
{
    /// <summary>
    /// 源代码位置信息
    /// </summary>
    SourcePosition Position { get; }
}