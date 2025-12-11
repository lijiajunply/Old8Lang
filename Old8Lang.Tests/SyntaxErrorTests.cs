using Old8Lang.Error;
using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Tests;

/// <summary>
    /// 语法错误测试，为每个语法错误问题单独编写测试用例
    /// </summary>
    [Collection("Sequential")]
    public class SyntaxErrorTests
{
    /// <summary>
    /// 测试括号不匹配 - 缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5 { PrintLine(\"Hello\") }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试括号不匹配 - 缺少左括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if a > 5) { PrintLine(\"Hello\") }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试大括号不匹配 - 缺少右大括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5) { PrintLine(\"Hello\")";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试大括号不匹配 - 缺少左大括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5) PrintLine(\"Hello\") }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试方括号不匹配 - 缺少右方括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "array[0 <- 10";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试方括号不匹配 - 缺少左方括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "array]0] <- 10";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效标识符 - 以数字开头
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidIdentifierStartWithNumber_ThrowsSyntaxError()
    {
        // Arrange
        var code = "123invalid <- 10";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效赋值运算符 - 使用等号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidAssignmentOperator_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a = 10";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的if语句 - 缺少条件表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidIfStatementMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if { PrintLine(\"Hello\") }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的elif语句 - 缺少条件表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidElifStatementMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5) { PrintLine(\"Hello\") } elif { PrintLine(\"World\") }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的for循环 - 缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidForLoopMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "for i <- 0, i < 10 { PrintLine(i) }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的while循环 - 缺少条件表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidWhileLoopMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "while { PrintLine(\"Hello\") }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的字典初始化 - 多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidDictionaryInitExtraComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_dict <- {\"name\": \"Alice\", \"age\": 30, }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的类型注解 - 无效类型名
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidTypeAnnotation_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_type <- 10 : invalid_type";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的lambda表达式 - 缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidLambdaMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_lambda <- (a, b) ->";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试无效的switch语句 - 缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidSwitchMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "switch { case 1 { PrintLine(\"One\") } }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试typedIdentifier无效的类型注解
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidTypedIdentifier_ThrowsSyntaxError()
    {
        // Arrange
        var code = "func test(x: 123) { return x }";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
    
    /// <summary>
    /// 测试tuple只有一个元素
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidTupleSingleElement_ThrowsSyntaxError()
    {
        // Arrange
        var code = "(1)";
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
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
        var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);
        
        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
}