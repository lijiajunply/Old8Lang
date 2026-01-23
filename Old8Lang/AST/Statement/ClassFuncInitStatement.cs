using Old8Lang.AST.Visitor;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
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

    public override void Run(VariateManager manager)
    {
        // 类函数声明直接存储在ClassMemberId中，由BlockStatement的ToAnyData和ToStaticData方法处理
        manager.Set(Id, FuncValue);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 类方法的IL生成由ClassInit.GenerateIl()中的DefineMethod方法处理
        // 这里不需要生成任何IL代码，因为类方法已经在类定义时编译完成
        // 这个语句主要用于将方法信息添加到类的TypeTemplate中，实际IL生成在ClassInit中完成
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"{Id} {FuncValue}";

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("ClassFuncInitStatement 暂不支持 Visitor 模式访问");
    }
}