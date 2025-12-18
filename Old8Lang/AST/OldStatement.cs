using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST;

public abstract class OldStatement : IOldLangTree
{
    /// <inheritdoc />
    public SourcePosition Position { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    protected OldStatement(SourcePosition position = default)
    {
        Position = position;
    }
    
    public abstract void Run(VariateManager manager);

    public abstract void GenerateIl(ILGenerator ilGenerator, LocalManager local);

    public abstract OldStatement? this[int index] { get; }
    public abstract int Count { get; }
}