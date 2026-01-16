using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Defer语句，用于延迟执行（类似于Go的defer语句）
/// 在函数返回前执行，多个defer按后进先出（LIFO）顺序执行
/// </summary>
/// <param name="statement">要延迟执行的语句</param>
/// <param name="position">位置信息</param>
public partial class DeferStatement(
    OldStatement statement,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 获取要延迟执行的语句
    /// </summary>
    public OldStatement Statement => statement;

    public override void Run(VariateManager manager)
    {
        // defer语句不立即执行，而是注册到defer栈中
        // 由函数执行器负责在函数返回前执行
        manager.RegisterDefer(statement);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器模式下的defer实现
        // 将defer语句添加到LocalManager的defer栈中
        local.RegisterDefer(statement);
    }

    public override OldStatement this[int index]
    {
        get
        {
            // Defer 语句只包含一个子语句
            if (index == 0)
            {
                return statement;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<OldStatement>());
        }
    }

    public override int Count => 1; // 只有一个子语句
}
