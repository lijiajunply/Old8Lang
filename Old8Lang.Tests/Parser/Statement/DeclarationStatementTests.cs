using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// 声明语句测试，测试各种声明语句（函数、类、标识符等）和其他语法的错误
/// </summary>
[Collection("Sequential")]
public class DeclarationStatementTests
{
    /// <summary>
    /// 测试无效标识符 - 以数字开头
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidIdentifierStartWithNumber_ThrowsSyntaxError()
    {
        // Arrange
        var code = "123invalid <- 10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的函数声明 - 函数名以数字开头
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidFunctionNameStartWithNumber_ThrowsSyntaxError()
    {
        // Arrange
        var code = "func 123func() { return 0 }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的字典语法 - 缺少冒号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidDictionaryMissingColon_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_dict <- {\"name\" \"Alice\", \"age\" 30}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的范围表达式 - 缺少结束值
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidRangeMissingEndValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_range <- [1~]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的范围表达式 - 缺少起始值
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidRangeMissingStartValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_range <- [~10]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的函数调用 - 多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidFunctionCallExtraComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "func_call(1, 2, )";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的类声明 - 类名以数字开头
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidClassNameStartWithNumber_ThrowsSyntaxError()
    {
        // Arrange
        var code = "class 123Class { var a <- 10 }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的数组初始化 - 多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidArrayInitExtraComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_array <- [1, 2, ]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的列表初始化 - 多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidListInitExtraComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_list <- list[1, 2, ]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字典尾随逗号 - 现在支持尾随逗号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidDictionaryInitExtraComma_ThrowsSyntaxError()
    {
        // Arrange - 尾随逗号现在是允许的
        var code = "valid_dict <- {\"name\": \"Alice\", \"age\": 30, }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert - 应该成功解析，不再抛出错误
        var result = parser.ParseProgram();
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试无效的lambda表达式 - 缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidLambdaMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_lambda <- (a, b) ->";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的字符串模板 - 未闭合的占位符
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidStringTemplateUnclosedPlaceholder_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_template <- $\"Hello {name";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的导入语句 - 缺少模块名
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidImportMissingModuleName_ThrowsSyntaxError()
    {
        // Arrange
        var code = "import";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的native语句 - 缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidNativeMissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "[import \"Old8LangLib\" Time GetTimeNow";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的算术表达式 - 缺少操作数
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidArithmeticMissingOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- 10 +";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的case语句 - 缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidCaseMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "switch (a) { case { PrintLine(\"One\") } }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试throw语句缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidThrowMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "throw";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试lrBlock缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidLrBlockMissingRightParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "(a <- 10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试lrBlock缺少左括号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidLrBlockMissingLeftParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 10)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试forInStatement缺少in关键字
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidForInMissingInKeyword_ThrowsSyntaxError()
    {
        // Arrange
        var code = "for i [1, 2, 3] { PrintLine(i) }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试accessModifier无效
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidAccessModifier_ThrowsSyntaxError()
    {
        // Arrange
        var code = "protected func test() { return 0 }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试asStatement缺少类型名
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidAsStatementMissingTypeName_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a as";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试slice缺少冒号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidSliceMissingColon_ThrowsSyntaxError()
    {
        // Arrange
        var code = "array[0 10]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试slice缺少结束索引
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidSliceMissingEndIndex_ThrowsSyntaxError()
    {
        // Arrange
        var code = "array[0:]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试tuple缺少逗号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidTupleMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "(1 2)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试list缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidListMissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "list[1, 2, 3";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试dictionary缺少右大括号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidDictionaryMissingRightBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "{\"name\": \"Alice\", \"age\": 30";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试stringTree缺少右大括号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidStringTreeMissingRightBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "$\"Hello {name";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试instantiate缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidInstantiateMissingRightParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "TestClass(1, 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试notPrefix缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidNotPrefixMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "not";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试minusPrefix缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidMinusPrefixMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "-";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试二进制表达式缺少右操作数
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidBinaryExpressionMissingRightOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a >";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试数字操作表达式缺少右操作数
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidNumberOperaMissingRightOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a +";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试布尔操作表达式缺少右操作数
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidBoolOperaMissingRightOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a and";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试函数声明缺少参数列表
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidFuncDeclarationMissingParamList_ThrowsSyntaxError()
    {
        // Arrange
        var code = "func test { return 0 }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试类声明缺少类块
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidClassDeclarationMissingClassBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = "class TestClass";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试使用关键字作为变量名
    /// </summary>
    [Fact]
    public void ParseProgram_KeywordAsVariable_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if <- 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
}
