using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 带有修饰符的类函数声明语句
/// </summary>
public class ClassFuncInitStatement : OldStatement
{
    public readonly ClassMemberId Id;
    public readonly FuncLangValue FuncValue;
    
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

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

    public override void Run(VariateManager manager)
    {
        // 类函数声明直接存储在ClassMemberId中，由BlockStatement的ToAnyData和ToStaticData方法处理
        manager.Set(Id, FuncValue);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // FuncLangValue 没有 GenerateIl 方法，这里暂时留空
        // 编译模式下可能需要实现此方法
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"{Id} {FuncValue}";
}