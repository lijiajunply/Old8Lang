using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Execution;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 验证宿主级执行调度入口保留程序行为。
/// </summary>
public class ExecutionModeDispatcherTests
{
    [Fact]
    public void Execute_PreservesProgramResult()
    {
        var interpreter = new LangInterpreter();
        var program = interpreter.Build("result <- 19 + 23");

        ExecutionModeDispatcher.Execute(program, interpreter.Manager);

        var result = Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("result")));
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Compiler_CompilesThroughVisitorDispatch()
    {
        var interpreter = new LangInterpreter();
        var program = interpreter.Build("result <- 19 + 23");

        var action = Old8Lang.Compiler.Compiler.Compile(program, "dispatcher.old8", interpreter);

        var exception = Record.Exception(action);
        Assert.Null(exception);
    }
}
