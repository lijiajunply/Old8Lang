namespace Old8Lang.AST;

public interface IVisitor<out T>
{
    // 表达式节点访问方法
    T Visit(Expression.Operation node);
    T Visit(Expression.LangId node);
    T Visit(Expression.ClassMemberId node);
    T Visit(Expression.TernaryExpression node);

    // 中间表达式节点
    T Visit(Expression.Intermediates.ArgList node);
    T Visit(Expression.Intermediates.ErrorLangValue node);
    T Visit(Expression.Intermediates.ILangList node);
    T Visit(Expression.Intermediates.IdList node);
    T Visit(Expression.Intermediates.ImportInfo node);
    T Visit(Expression.Value.Instance node);
    T Visit(Expression.Intermediates.NativeAnyLangValue node);
    T Visit(Expression.Intermediates.NativeStaticAny node);
    T Visit(Expression.Intermediates.RangeLangValue node);
    T Visit(Expression.Intermediates.SliceLangValue node);
    T Visit(Expression.Intermediates.StringTreeList node);
    T Visit(Expression.Intermediates.VoidLangValue node);

    // 值表达式节点
    T Visit(Expression.Value.AnyLangValue node);
    T Visit(Expression.Value.ArrayLangValue node);
    T Visit(Expression.Value.BoolLangValue node);
    T Visit(Expression.Value.CharLangValue node);
    T Visit(Expression.Value.DictionaryLangValue node);
    T Visit(Expression.Value.DoubleLangValue node);
    T Visit(Expression.Value.FuncLangValue node);
    T Visit(Expression.Value.IntLangValue node);
    T Visit(Expression.Value.LangListItem node);
    T Visit(Expression.Value.ListComprehension node);
    T Visit(Expression.Value.ListLangValue node);
    T Visit(Expression.Value.NullLangValue node);
    T Visit(Expression.Value.StringLangValue node);
    T Visit(Expression.Value.TupleLangValue node);
    T Visit(Expression.Value.TypeLangValue node);
    T Visit(Expression.Value.TypeTemplate node);

    // 语句节点访问方法
    T Visit(Statement.BlockStatement node);
    T Visit(Statement.BreakStatement node);
    T Visit(Statement.ClassFieldSetStatement node);
    T Visit(Statement.ClassFuncInitStatement node);
    T Visit(Statement.ClassInit node);
    T Visit(Statement.ContinueStatement node);
    T Visit(Statement.ForInStatement node);
    T Visit(Statement.ForStatement node);
    T Visit(Statement.FuncInit node);
    T Visit(Statement.FuncRunStatement node);
    T Visit(Statement.IfStatement node);
    T Visit(Statement.ImportStatement node);
    T Visit(Statement.NativeStatement node);
    T Visit(Statement.OldIf node);
    T Visit(Statement.ReturnStatement node);
    T Visit(Statement.SetStatement node);
    T Visit(Statement.SwitchStatement node);
    T Visit(Statement.OldCase node);
    T Visit(Statement.ThrowStatement node);
    T Visit(Statement.TryStatement node);
    T Visit(Statement.WhileStatement node);
    T Visit(MockLangTree mock);
}

public interface IOldLangTree
{
    /// <summary>
    /// 源代码位置信息
    /// </summary>
    SourcePosition Position { get; }

    /// <summary>
    /// 接受访问者，实现访问者模式
    /// </summary>
    /// <typeparam name="T">访问者返回类型</typeparam>
    /// <param name="visitor">访问者实例</param>
    /// <returns>访问者处理结果</returns>
    T Accept<T>(IVisitor<T> visitor);
}

public class MockLangTree : IOldLangTree
{
    public SourcePosition Position { get; }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}