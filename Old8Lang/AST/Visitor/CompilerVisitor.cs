using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 编译器 Visitor - 替代原有的 GenerateIl() 方法
/// </summary>
public partial class CompilerVisitor(ILGenerator ilGenerator, LocalManager local) : IVisitor<object?>
{
    // Statement 访问方法将在后续实现
    // Expression 访问方法将在后续实现
    // Value 访问方法将在后续实现
}