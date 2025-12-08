using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// continue语句
/// </summary>
public class ContinueStatement(SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        // 使用异常来处理continue跳转
        throw new ContinueException();
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器会在循环语句中处理continue的标签
        // 这里直接跳转到循环开始标签
        if (local.ContinueLabel.HasValue)
        {
            ilGenerator.Emit(OpCodes.Br, local.ContinueLabel.Value);
        }
        else
        {
            throw new System.Exception("Continue statement outside of loop");
        }
    }

    public override OldStatement this[int index] => throw new System.NotImplementedException();

    public override int Count => 0;

    public override string ToString() => "continue";
}

/// <summary>
/// continue异常，用于解释器中的跳转处理
/// </summary>
public class ContinueException : System.Exception
{
    public ContinueException() : base("Continue exception") { }
}