namespace Old8Lang.AST;

public interface IOldLangTree
{
    /// <summary>
    /// 源代码位置信息
    /// </summary>
    SourcePosition Position { get; }
}

public class MockLangTree : IOldLangTree
{
    public SourcePosition Position { get; } = new();
}