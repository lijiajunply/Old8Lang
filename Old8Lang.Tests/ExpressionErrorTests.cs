using Old8Lang.Error;

namespace Old8Lang.Tests;

/// <summary>
/// 表达式错误验证测试，测试各种无效的表达式和语法错误
/// </summary>
[Collection("Sequential")]
public class ExpressionErrorTests
{
    #region 算术表达式错误

    /// <summary>
    /// 测试缺少操作数的加法
    /// </summary>
    [Fact]
    public void ParseProgram_IncompletePlusExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 +";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少左操作数的加法
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftOperandPlus_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- + 1";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试连续的运算符
    /// </summary>
    [Fact]
    public void ParseProgram_ConsecutiveOperators_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 + + 2";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的乘法表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteMultiplication_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 5 *";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的除法表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteDivision_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 10 /";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 比较表达式错误

    /// <summary>
    /// 测试不完整的比较表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteComparison_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x >";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的等于表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteEquals_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x ==";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的不等于表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteNotEquals_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x !=";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 逻辑表达式错误

    /// <summary>
    /// 测试不完整的and表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteAnd_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- true and";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的or表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteOr_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- false or";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的xor表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteXor_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- true xor";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试not后缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_NotWithoutExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- not";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 赋值表达式错误

    /// <summary>
    /// 测试缺少赋值运算符
    /// </summary>
    [Fact]
    public void ParseProgram_MissingAssignmentOperator_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a 123";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试赋值缺少右值
    /// </summary>
    [Fact]
    public void ParseProgram_AssignmentMissingRightValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <-";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试给字面量赋值
    /// </summary>
    [Fact]
    public void ParseProgram_AssignToLiteral_ThrowsSyntaxError()
    {
        // Arrange
        var code = "123 <- a";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 括号错误

    /// <summary>
    /// 测试不匹配的左括号
    /// </summary>
    [Fact]
    public void ParseProgram_UnmatchedLeftParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (1 + 2";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不匹配的右括号
    /// </summary>
    [Fact]
    public void ParseProgram_UnmatchedRightParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 + 2)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试空括号
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyParentheses_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- ()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 数组/列表错误

    /// <summary>
    /// 测试数组缺少逗号分隔符
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1 2 3]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试数组缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayMissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1, 2, 3";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试数组末尾多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayTrailingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1, 2, 3,]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 字典错误

    /// <summary>
    /// 测试字典缺少冒号
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryMissingColon_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- {\"key\" \"value\"}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字典缺少值
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryMissingValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- {\"key\":}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字典缺少键
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryMissingKey_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- {:\"value\"}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 函数调用错误

    /// <summary>
    /// 测试函数调用缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionCallMissingRightParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = "PrintLine(\"Hello\"";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试函数参数缺少逗号
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionArgumentsMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "Add(1 2)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试函数调用多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionCallTrailingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "Add(1, 2,)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 成员访问错误

    /// <summary>
    /// 测试点号后缺少成员名
    /// </summary>
    [Fact]
    public void ParseProgram_MemberAccessMissingMember_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- obj.";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试连续的点号
    /// </summary>
    [Fact]
    public void ParseProgram_ConsecutiveDots_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- obj..field";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 索引访问错误

    /// <summary>
    /// 测试索引缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_IndexMissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- array[0";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试索引缺少索引表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IndexMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- array[]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 类型转换错误

    /// <summary>
    /// 测试as后缺少类型
    /// </summary>
    [Fact]
    public void ParseProgram_AsStatementMissingType_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- value as";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 三元表达式错误

    /// <summary>
    /// 测试三元表达式缺少问号
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingQuestionMark_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition 1 : 2";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少冒号
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingColon_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition ? 1";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少条件
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- ? 1 : 2";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少真值
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingTrueValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition ? : 2";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少假值
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingFalseValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition ? 1 :";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region Lambda表达式错误

    /// <summary>
    /// 测试Lambda缺少箭头
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingArrow_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (x, y) x + y";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试Lambda缺少参数括号
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingParentheses_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x, y -> x + y";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试Lambda缺少表达式体
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingBody_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (x, y) ->";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 字符串模板错误

    /// <summary>
    /// 测试字符串模板缺少右花括号
    /// </summary>
    [Fact]
    public void ParseProgram_StringTemplateMissingRightBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- $\"Hello {name\"";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字符串模板花括号内为空
    /// </summary>
    [Fact]
    public void ParseProgram_StringTemplateEmptyBraces_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- $\"Hello {}\"";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 控制流语句错误

    /// <summary>
    /// 测试if缺少条件
    /// </summary>
    [Fact]
    public void ParseProgram_IfMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if {}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试while缺少条件
    /// </summary>
    [Fact]
    public void ParseProgram_WhileMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "while {}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试for缺少分号
    /// </summary>
    [Fact]
    public void ParseProgram_ForMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "for i <- 0 i < 10 i++ {}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试for-in缺少in关键字
    /// </summary>
    [Fact]
    public void ParseProgram_ForInMissingIn_ThrowsSyntaxError()
    {
        // Arrange
        var code = "for item [1, 2, 3] {}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 范围表达式错误

    /// <summary>
    /// 测试范围缺少结束值
    /// </summary>
    [Fact]
    public void ParseProgram_RangeMissingEndValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1~]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试范围缺少开始值
    /// </summary>
    [Fact]
    public void ParseProgram_RangeMissingStartValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [~10]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 其他语法错误

    /// <summary>
    /// 测试使用关键字作为变量名
    /// </summary>
    [Fact]
    public void ParseProgram_KeywordAsVariable_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if <- 1";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试多个连续的赋值运算符
    /// </summary>
    [Fact]
    public void ParseProgram_ConsecutiveAssignmentOperators_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- <- 1";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试语句缺少分隔
    /// </summary>
    [Fact]
    public void ParseProgram_MissingStatementSeparation_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 b <- 2";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 这个可能会解析成功或失败，取决于解析器的实现
        // 如果解析器要求明确的语句分隔符，则应该抛出错误
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
