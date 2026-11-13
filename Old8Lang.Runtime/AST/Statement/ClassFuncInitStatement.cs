using Old8Lang.AST.Visitor;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 带有修饰符的类函数声明语句
/// </summary>
public class ClassFuncInitStatement : OldStatement
{
    public readonly ClassMemberId Id;
    public readonly FuncLangValue FuncValue;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">类成员ID，包含修饰符信息</param>
    /// <param name="funcValue">函数值</param>
    /// <param name="position">位置信息</param>
    public ClassFuncInitStatement(ClassMemberId id, FuncLangValue funcValue, SourcePosition position = default) : base(position)
    {
        Id = id;
        FuncValue = funcValue;
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"{Id} {FuncValue}";

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("ClassFuncInitStatement 暂不支持 Visitor 模式访问");
    }
}