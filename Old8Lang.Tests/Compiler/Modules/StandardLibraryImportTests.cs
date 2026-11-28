using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Modules;

/// <summary>
/// IL 模式下标准库模块导入、别名、函数体内调用与并发原语组合端到端测试。
/// </summary>
[Collection("Sequential")]
public class StandardLibraryImportTests
{
    [Fact]
    public void StandardLibrary_ImportMathAndRegex_WorksInIlMode()
    {
        const string code = @"
import Math
import Regex

func compute(x:double) -> double {
    return Math.Sqrt(x) + Math.Abs(-3.0)
}

func isDigits(text:string) -> bool {
    return Regex.RegexIsMatch(text, ""^[0-9]+$"")
}

Assert.Equal(7.0, compute(16.0))
Assert.True(isDigits(""12345""))
Assert.False(isDigits(""abc""))
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var action = Old8Lang.Compiler.Compiler.Compile(ast, "stdlib_import_math_regex", interpreter);
        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void StandardLibrary_ImportWithAlias_WorksInIlMode()
    {
        const string code = @"
import Math as M
import OS as Sys

val <- M.Max(12.0, 25.0)
Assert.Equal(25.0, val)

osInfo <- Sys.OsInfo()
Assert.True(osInfo.Length() > 0)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var action = Old8Lang.Compiler.Compiler.Compile(ast, "stdlib_import_alias", interpreter);
        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void StandardLibrary_CombinedWithConcurrencyPrimitives_WorksInIlMode()
    {
        const string code = @"
import Math

atomicId <- AtomicIntCreate(0)
ch <- ChannelCreateBounded(4)

ids <- {atomicId}
for id in ids {
    v <- Math.Floor(3.9)
    AtomicIntAdd(id, v)
}

ChannelSend(ch, AtomicIntGet(atomicId))
received <- ChannelReceive(ch)
Assert.Equal(3, received)

ChannelClose(ch)
ChannelDispose(ch)
AtomicIntDispose(atomicId)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var action = Old8Lang.Compiler.Compiler.Compile(ast, "stdlib_concurrency_il", interpreter);
        Assert.Null(Record.Exception(() => action()));
    }
}
