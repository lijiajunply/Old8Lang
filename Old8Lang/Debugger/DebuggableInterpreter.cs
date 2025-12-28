using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Debugger;

/// <summary>
/// 可调试的解释器包装器 - 简化版本
/// </summary>
public class DebuggableInterpreter(LangInterpreter interpreter, Debugger debugger)
{
    /// <summary>
    /// 执行带有调试功能的AST
    /// </summary>
    /// <param name="statement">要执行的语句</param>
    public void Execute(BlockStatement statement)
    {
        try
        {
            // 进入主函数
            debugger.EnterFunction("main", interpreter.Manager.Path, statement.Position, interpreter.Manager);

            // 执行整个块（简化版本）
            statement.Run(interpreter.Manager);

            // 离开主函数
            debugger.ExitFunction();
        }
        catch (Exception ex)
        {
            debugger.HandleError(ex, statement.Position, "main");
        }
    }

    /// <summary>
    /// 创建调试版的变量管理器
    /// </summary>
    /// <param name="original">原始变量管理器</param>
    /// <returns>调试版变量管理器</returns>
    public static VariateManager CreateDebuggableManager(VariateManager original, Debugger debugger)
    {
        // 简化版本：返回原始管理器
        // 在完整实现中，需要创建包装器类
        return original;
    }
}