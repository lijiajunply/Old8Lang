using Old8Lang.AST;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Parser;

/// <summary>
/// 结构化文档注释解析测试
/// </summary>
[Collection("Sequential")]
public class StructuredDocCommentTests
{
    /// <summary>
    /// 测试 Google Style 文档注释解析
    /// </summary>
    [Fact]
    public void TestGoogleStyleDocComment()
    {
        var code = @"
/// Calculate the sum of two numbers
///
/// Args:
///     a (int): The first number
///     b (int): The second number
///
/// Returns:
///     int: The sum of a and b
func add(a:int, b:int) -> int {
    return a + b
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        // 获取函数定义
        var funcInit = program.GetImportStatement(0) as FuncInit;
        Assert.NotNull(funcInit);
        Assert.NotNull(funcInit.FuncLangValue);

        // 验证文档注释
        var docComment = funcInit.FuncLangValue.DocComment;
        Assert.NotNull(docComment);
        Assert.Equal("Calculate the sum of two numbers", docComment.Summary);

        // 验证参数文档
        Assert.Equal(2, docComment.Parameters.Count);
        Assert.Equal("a", docComment.Parameters[0].Name);
        Assert.Equal("int", docComment.Parameters[0].Type);
        Assert.Contains("first number", docComment.Parameters[0].Description);

        Assert.Equal("b", docComment.Parameters[1].Name);
        Assert.Equal("int", docComment.Parameters[1].Type);
        Assert.Contains("second number", docComment.Parameters[1].Description);

        // 验证返回值文档
        Assert.NotNull(docComment.Returns);
        Assert.Equal("int", docComment.Returns!.Type);
        Assert.Contains("sum", docComment.Returns.Description);
    }

    /// <summary>
    /// 测试 Sphinx Style 文档注释解析
    /// </summary>
    [Fact]
    public void TestSphinxStyleDocComment()
    {
        var code = @"
/// Divide two numbers
///
/// :param numerator: The number to be divided
/// :type numerator: double
/// :param denominator: The number to divide by
/// :type denominator: double
/// :return: The division result
/// :rtype: double
func divide(numerator:double, denominator:double) -> double {
    return numerator / denominator
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var funcInit = program.GetImportStatement(0) as FuncInit;
        Assert.NotNull(funcInit);
        Assert.NotNull(funcInit.FuncLangValue);

        var docComment = funcInit.FuncLangValue.DocComment;
        Assert.NotNull(docComment);
        Assert.Equal("Divide two numbers", docComment.Summary);

        // 验证参数文档
        Assert.Equal(2, docComment.Parameters.Count);
        Assert.Equal("numerator", docComment.Parameters[0].Name);
        Assert.Equal("double", docComment.Parameters[0].Type);
        Assert.Contains("divided", docComment.Parameters[0].Description);

        Assert.Equal("denominator", docComment.Parameters[1].Name);
        Assert.Equal("double", docComment.Parameters[1].Type);
        Assert.Contains("divide by", docComment.Parameters[1].Description);

        // 验证返回值文档
        Assert.NotNull(docComment.Returns);
        Assert.Equal("double", docComment.Returns!.Type);
        Assert.Contains("division result", docComment.Returns.Description);
    }

    /// <summary>
    /// 测试 JavaDoc Style 文档注释解析
    /// </summary>
    [Fact]
    public void TestJavaDocStyleDocComment()
    {
        var code = @"
/// Concatenate two strings
///
/// @param str1 The first string
/// @param str2 The second string
/// @return The concatenated string
func concat(str1:string, str2:string) -> string {
    return str1 + str2
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var funcInit = program.GetImportStatement(0) as FuncInit;
        Assert.NotNull(funcInit);
        Assert.NotNull(funcInit.FuncLangValue);

        var docComment = funcInit.FuncLangValue.DocComment;
        Assert.NotNull(docComment);
        Assert.Equal("Concatenate two strings", docComment.Summary);

        // 验证参数文档
        Assert.Equal(2, docComment.Parameters.Count);
        Assert.Equal("str1", docComment.Parameters[0].Name);
        Assert.Contains("first string", docComment.Parameters[0].Description);

        Assert.Equal("str2", docComment.Parameters[1].Name);
        Assert.Contains("second string", docComment.Parameters[1].Description);

        // 验证返回值文档
        Assert.NotNull(docComment.Returns);
        Assert.Contains("concatenated", docComment.Returns!.Description);
    }

    /// <summary>
    /// 测试中文风格文档注释解析
    /// </summary>
    [Fact]
    public void TestChineseStyleDocComment()
    {
        var code = @"
/// 计算圆的面积
///
/// 参数:
///   - radius (double): 圆的半径
///
/// 返回:
///   圆的面积值
func calculateCircleArea(radius:double) -> double {
    return 3.14159 * radius * radius
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var funcInit = program.GetImportStatement(0) as FuncInit;
        Assert.NotNull(funcInit);
        Assert.NotNull(funcInit.FuncLangValue);

        var docComment = funcInit.FuncLangValue.DocComment;
        Assert.NotNull(docComment);
        Assert.Equal("计算圆的面积", docComment.Summary);

        // 验证参数文档
        Assert.Single(docComment.Parameters);
        Assert.Equal("radius", docComment.Parameters[0].Name);
        Assert.Equal("double", docComment.Parameters[0].Type);
        Assert.Contains("半径", docComment.Parameters[0].Description);

        // 验证返回值文档
        Assert.NotNull(docComment.Returns);
        Assert.Contains("面积", docComment.Returns!.Description);
    }

    /// <summary>
    /// 测试异步函数的文档注释
    /// </summary>
    [Fact]
    public void TestAsyncFunctionDocComment()
    {
        var code = @"
/// Asynchronous delay function
///
/// @param milliseconds The delay duration
/// @return A task
async func delay(milliseconds:int) {
    return milliseconds
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var asyncFuncInit = program.GetImportStatement(0) as AsyncFuncInit;
        Assert.NotNull(asyncFuncInit);
        Assert.NotNull(asyncFuncInit.AsyncFuncValue);

        var asyncFunc = asyncFuncInit.AsyncFuncValue;
        Assert.NotNull(asyncFunc);

        var docComment = asyncFunc.DocComment;
        Assert.NotNull(docComment);
        Assert.Equal("Asynchronous delay function", docComment.Summary);

        // 验证参数文档
        Assert.Single(docComment.Parameters);
        Assert.Equal("milliseconds", docComment.Parameters[0].Name);
        Assert.Contains("delay duration", docComment.Parameters[0].Description);

        // 验证返回值文档
        Assert.NotNull(docComment.Returns);
        Assert.Contains("task", docComment.Returns!.Description);
    }

    /// <summary>
    /// 测试类的文档注释（简单测试）
    /// </summary>
    [Fact]
    public void TestClassDocComment()
    {
        var code = @"
/// A simple calculator class
///
/// Args:
///     initialValue (int): The initial value
class Calculator {
    private value:int
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var classInit = program.GetImportStatement(0) as ClassInit;
        Assert.NotNull(classInit);
        Assert.NotNull(classInit.AnyLangValue);

        var typeTemplate = classInit.AnyLangValue;
        Assert.NotNull(typeTemplate);

        var docComment = typeTemplate.DocComment;
        Assert.NotNull(docComment);
        Assert.Equal("A simple calculator class", docComment.Summary);

        // 验证参数文档（虽然类不应该有参数，但我们测试解析器能否处理）
        Assert.Single(docComment.Parameters);
        Assert.Equal("initialValue", docComment.Parameters[0].Name);
        Assert.Equal("int", docComment.Parameters[0].Type);
    }

    /// <summary>
    /// 测试无文档注释的函数
    /// </summary>
    [Fact]
    public void TestFunctionWithoutDocComment()
    {
        var code = @"
func add(a:int, b:int) -> int {
    return a + b
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var funcInit = program.GetImportStatement(0) as FuncInit;
        Assert.NotNull(funcInit);
        Assert.NotNull(funcInit.FuncLangValue);

        // 验证没有文档注释时返回 null
        var docComment = funcInit.FuncLangValue.DocComment;
        Assert.Null(docComment);
    }

    /// <summary>
    /// 测试默认风格（无结构化标记）的文档注释
    /// </summary>
    [Fact]
    public void TestDefaultStyleDocComment()
    {
        var code = @"
/// This is a simple function
/// that adds two numbers together
/// without any special formatting.
func add(a:int, b:int) -> int {
    return a + b
}
";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var funcInit = program.GetImportStatement(0) as FuncInit;
        Assert.NotNull(funcInit);
        Assert.NotNull(funcInit.FuncLangValue);

        var docComment = funcInit.FuncLangValue.DocComment;
        Assert.NotNull(docComment);

        // 默认风格应该将所有内容作为摘要
        Assert.Contains("simple function", docComment.Summary);
        Assert.Contains("adds two numbers", docComment.Summary);

        // 没有结构化的参数和返回值信息
        Assert.Empty(docComment.Parameters);
        Assert.Null(docComment.Returns);
    }
}
