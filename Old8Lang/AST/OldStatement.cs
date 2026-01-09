using System.Reflection.Emit;
using Old8Lang.AST.Visitor;
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

    /// <summary>
    /// 解释器模式执行
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <remarks>
    /// 注意：Visitor 模式已实现。推荐使用 Accept 方法配合 InterpreterVisitor。
    /// </remarks>
    public abstract void Run(VariateManager manager);

    /// <summary>
    /// 编译器模式IL代码生成
    /// </summary>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <remarks>
    /// 注意：Visitor 模式已实现。推荐使用 Accept 方法配合 CompilerVisitor。
    /// </remarks>
    public abstract void GenerateIl(ILGenerator ilGenerator, LocalManager local);

    public abstract OldStatement? this[int index] { get; }
    public abstract int Count { get; }

    /// <inheritdoc />
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
}