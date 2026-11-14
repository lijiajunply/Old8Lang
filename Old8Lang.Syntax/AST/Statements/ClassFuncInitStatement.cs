using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 带有修饰符的类函数声明语句（纯语法模型）。
/// </summary>
public class ClassFuncInitStatement : OldStatement
{
    public readonly ClassMemberId Id;
    public readonly FunctionSyntax Function;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">类成员ID，包含修饰符信息</param>
    /// <param name="function">函数语法模型</param>
    /// <param name="position">位置信息</param>
    public ClassFuncInitStatement(ClassMemberId id, FunctionSyntax function, SourcePosition position = default) : base(position)
    {
        Id = id;
        Function = function;
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"{Id} {Function}";

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("ClassFuncInitStatement 暂不支持 Visitor 模式访问");
    }
}
