using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 运行时类型错误测试 - 覆盖空引用访问、正负越界访问、非法类型转换与非法调用
/// </summary>
[Collection("Sequential")]
public class RuntimeTypeErrorTests
{
    #region 空引用错误

    [Fact]
    public void RuntimeError_NullMemberAccess_ThrowsOld8Exception()
    {
        var code = @"
x <- null
y <- x.name
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_NullMethodCall_ThrowsOld8Exception()
    {
        var code = @"
x <- null
y <- x.nonExistent()
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_NullIndexAccess_ThrowsOld8Exception()
    {
        var code = @"
x <- null
y <- x[0]
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_NullArithmetic_ThrowsOld8Exception()
    {
        var code = @"
x <- null
y <- x * 2
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager));
    }

    #endregion

    #region 越界访问错误

    [Theory]
    [InlineData("arr <- [1, 2, 3]\nv <- arr[5]")]
    [InlineData("arr <- [1, 2, 3]\nv <- arr[-4]")]
    [InlineData("list <- {10, 20}\nv <- list[2]")]
    [InlineData("list <- {10, 20}\nv <- list[-3]")]
    [InlineData("s <- \"abc\"\nv <- s[5]")]
    [InlineData("s <- \"abc\"\nv <- s[-4]")]
    public void RuntimeError_CollectionIndexOutOfBounds_ThrowsIndexError(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<IndexError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_TupleIndexOutOfBounds_ThrowsInvalidOperationError()
    {
        var code = @"
t <- (1, 2)
v <- t[3]
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_NegativeIndexOutOfBounds_MessageReportsOriginalNegativeIndex()
    {
        var code = @"
arr <- [1, 2, 3]
v <- arr[-5]
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var ex = Assert.Throws<IndexError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("-5", ex.Message);
    }

    #endregion

    #region 类型转换与调用错误

    [Fact]
    public void RuntimeError_InvalidStringToIntCast_ThrowsFormatOrTypeError()
    {
        var code = @"
v <- ""not_an_int"" as int
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_CallUndefinedFunction_ThrowsNameError()
    {
        var code = @"
res <- noSuchFunction_Old8(1)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_FunctionParameterTypeMismatch_ThrowsTypeError()
    {
        var code = @"
func addOne(x:int) -> int {
    return x + 1
}
res <- addOne(""hello"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<TypeError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void RuntimeError_FunctionArgumentCountMismatch_ThrowsArgumentError()
    {
        var code = @"
func pair(a:int, b:int) -> int {
    return a + b
}
res <- pair(1)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<ArgumentError>(() => ast.Run(interpreter.Manager));
    }

    #endregion
}
