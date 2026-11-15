using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// IL 模式对同步生成器（<c>yield</c>）的处理。
/// </summary>
/// <remarks>
/// 同步生成器在 IL 后端没有实现（见 <c>Docs/MODE_SUPPORT.md</c> 的 IL 支持矩阵），
/// 之前 <c>yield</c> 不发任何指令，整个方法被 JIT 判为无效 IL。现在在编译期直接报错，
/// 不再把无效 IL 留到运行期。
/// </remarks>
public class GeneratorIlUnsupportedTests
{
    [Fact]
    public void SyncGenerator_ReportsClearErrorInsteadOfInvalidIl()
    {
        var code = @"
            func simpleGen() {
                yield 1
                yield 2
            }

            gen <- simpleGen()
            PrintLine(""created"")
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var error = Assert.ThrowsAny<Exception>(() =>
                Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));

            Assert.DoesNotContain("InvalidProgram", error.GetType().Name);
            Assert.Contains("同步生成器", error.Message);
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }

    [Fact]
    public void AsyncGenerator_StillCompiles()
    {
        // 异步生成器走异步状态机路径，目前仍可编译（语义尚不完整）。
        var code = @"
            async func agen() -> stream {
                yield 1
            }

            s <- agen()
            PrintLine(""created"")
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            Assert.Null(Record.Exception(() => compiledAction()));
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
