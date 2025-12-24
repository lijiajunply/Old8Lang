using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 编译器 Visitor - 替代原有的 GenerateIl() 方法
/// </summary>
public partial class CompilerVisitor : IVisitor<object?>
{
    private readonly ILGenerator _ilGenerator;
    private readonly LocalManager _local;

    public CompilerVisitor(ILGenerator ilGenerator, LocalManager local)
    {
        _ilGenerator = ilGenerator;
        _local = local;
    }

    // Statement 访问方法将在后续实现
    // Expression 访问方法将在后续实现
    // Value 访问方法将在后续实现
}
