using Old8Lang.AST.Visitor;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 带有修饰符的类字段声明语句
/// </summary>
public class ClassFieldSetStatement : OldStatement
{
    public readonly ClassMemberId Id;
    public readonly LangExpression Value;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">类成员ID，包含修饰符信息</param>
    /// <param name="value">字段值</param>
    /// <param name="position">位置信息</param>
    public ClassFieldSetStatement(ClassMemberId id, LangExpression value, SourcePosition position = default) : base(position)
    {
        Id = id;
        Value = value;
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"{Id} <- {Value}";

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("ClassFieldSetStatement 暂不支持 Visitor 模式访问");
    }
}