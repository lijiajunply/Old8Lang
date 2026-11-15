using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// IL 模式下未标注类型的字段（无注解、无初值）读写。
/// </summary>
/// <remarks>
/// 这类字段在 IL 里是 <c>object</c>。曾经的非 this 写入路径
/// （<c>c.x &lt;- 100</c>）只发 <c>stfld</c>、不做装箱，字段里存的是裸 int32；
/// 之后读取会把该值当作对象引用，运行期直接段错误使测试主机消失。
/// </remarks>
public class UninitializedFieldIlTests
{
    [Fact]
    public void AssignIntThenRead_DoesNotCrash()
    {
        var code = @"
            class C {
                public x
            }

            c <- C()
            c.x <- 100
            Assert.Equal(100, c.x)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void AssignDoubleThenRead_PreservesValue()
    {
        var code = @"
            class C {
                public x
            }

            c <- C()
            c.x <- 1.5
            Assert.Equal(1.5, c.x)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void AssignStringThenRead_PreservesValue()
    {
        var code = @"
            class C {
                public x
            }

            c <- C()
            c.x <- ""text""
            Assert.Equal(""text"", c.x)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void ReadBeforeAssign_ReturnsNullLikeInterpreter()
    {
        var code = @"
            class C {
                public x
            }

            c <- C()
            PrintLine(c.x)
            c.x <- 100
            Assert.Equal(100, c.x)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void FieldValueInStringConcat_DoesNotCrash()
    {
        var code = @"
            class C {
                public x
            }

            c <- C()
            c.x <- 100
            text <- ""x="" + c.x
            Assert.Equal(""x=100"", text)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    private static Exception? CompileAndInvoke(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            return Record.Exception(() => compiledAction());
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
