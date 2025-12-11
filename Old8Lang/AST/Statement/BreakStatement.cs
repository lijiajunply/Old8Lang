using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// break语句
/// </summary>
public class BreakStatement(SourcePosition position = default) : OldStatement(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override void Run(VariateManager manager)
    {
        // 使用异常来处理break跳转
        throw new BreakException();
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器会在循环语句中处理break的标签
        // 这里直接跳转到循环结束标签
        if (local.BreakLabel.HasValue)
        {
            ilGenerator.Emit(OpCodes.Br, local.BreakLabel.Value);
        }
        else
        {
            throw new InvalidOperationError(new SourcePosition(), "Break statement outside of loop",
                "break语句只能在循环内部使用");
        }
    }

    public override OldStatement this[int index] => throw new InvalidOperationError(new SourcePosition(),
        "Indexer not implemented for BreakStatement", "BreakStatement不支持索引访问");

    public override int Count => 0;

    public override string ToString() => "break";
}

/// <summary>
/// break异常，用于解释器中的跳转处理
/// </summary>
public class BreakException() : Exception("Break exception");