using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Classes;

/// <summary>
/// 类体在文件结束处被截断时的解析行为。
/// </summary>
/// <remarks>
/// `ParseClassBlock` 曾经在遇到 EOF 时执行 `CurrentIndex++` 后 `continue`，
/// 但词法的 EOF 位置不会前进，`class A {` 这类截断文件会让解析器死循环
/// （`-s` 也会挂住，只能杀进程）。现在直接报语法错误。
/// </remarks>
[Collection("Sequential")]
public class ClassBlockEofTests
{
    [Fact]
    public void UnclosedClass_ThrowsSyntaxError()
    {
        var interpreter = new LangInterpreter();

        var exception = Assert.ThrowsAny<Exception>(() => interpreter.Build("class A {"));

        Assert.IsType<SyntaxError>(exception);
        Assert.Contains("右花括号", exception.Message);
    }

    [Fact]
    public void UnclosedClassWithField_ThrowsSyntaxError()
    {
        var interpreter = new LangInterpreter();

        var exception = Assert.ThrowsAny<Exception>(() =>
            interpreter.Build("class A {\n    public x:int <- 0"));

        Assert.IsType<SyntaxError>(exception);
        Assert.Contains("右花括号", exception.Message);
    }
}
