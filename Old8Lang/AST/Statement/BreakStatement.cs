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
    
    public override void Run(VariateManager manager)
    {
        // 优化：使用标志位替代异常处理，减少性能开销
        manager.BreakFlag = true;
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