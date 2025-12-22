using Old8Lang.AST.Visitor;

namespace Old8Lang.AST;

public interface IOldLangTree
{
    /// <summary>
    /// 源代码位置信息
    /// </summary>
    SourcePosition Position { get; }

    /// <summary>
    /// 接受 Visitor 访问（Visitor 模式）
    /// </summary>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="visitor">访问者对象</param>
    /// <returns>访问结果</returns>
    TResult Accept<TResult>(IVisitor<TResult> visitor);
}

public class MockLangTree : IOldLangTree
{
    public SourcePosition Position { get; } = new();

    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException("MockLangTree 不支持 Accept 方法");
    }
}