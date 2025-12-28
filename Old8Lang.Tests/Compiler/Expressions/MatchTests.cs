using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// Match表达式编译模式测试
/// 测试编译器模式下的Match表达式的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class MatchTests
{
    #region 基础匹配测试

    [Fact]
    public void Match_ValueMatching_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> string {
                result <- match 1 {
                    case 0 -> ""zero""
                    case 1 -> ""one""
                    case 2 -> ""two""
                    case _ -> ""other""
                }
                return result
            }

            Assert.True(test() == ""one"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_IntegerLiterals_CompilesAndMatches()
    {
        // Arrange
        var code = @"
            func getNumber(x:int) -> int {
                return match x {
                    case 0 -> 100
                    case 1 -> 200
                    case 2 -> 300
                    case _ -> -1
                }
            }

            Assert.True(getNumber(2) == 300)
            Assert.True(getNumber(5) == -1)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_StringMatching_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func greet(name:string) -> string {
                return match name {
                    case ""Bob"" -> ""Hello Bob!""
                    case ""Alice"" -> ""Hi Alice!""
                    case _ -> ""Hello stranger!""
                }
            }

            Assert.True(greet(""Alice"") == ""Hi Alice!"")
            Assert.True(greet(""Charlie"") == ""Hello stranger!"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_BooleanMatching_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func boolToString(flag:bool) -> string {
                return match flag {
                    case true -> ""yes""
                    case false -> ""no""
                }
            }

            Assert.True(boolToString(true) == ""yes"")
            Assert.True(boolToString(false) == ""no"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_DoubleMatching_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func identifyConstant(num:double) -> string {
                return match num {
                    case 2.71 -> ""e""
                    case 3.14 -> ""pi""
                    case _ -> ""unknown""
                }
            }

            Assert.True(identifyConstant(3.14) == ""pi"")
            Assert.True(identifyConstant(1.41) == ""unknown"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 通配符测试

    [Fact]
    public void Match_Wildcard_CatchesAllValues()
    {
        // Arrange
        var code = @"
            func categorize(x:int) -> string {
                return match x {
                    case 1 -> ""one""
                    case 2 -> ""two""
                    case _ -> ""many""
                }
            }

            Assert.True(categorize(999) == ""many"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_WildcardOnly_AlwaysMatches()
    {
        // Arrange
        var code = @"
            func always(x:int) -> string {
                return match x {
                    case _ -> ""matched""
                }
            }

            Assert.True(always(1) == ""matched"")
            Assert.True(always(999) == ""matched"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 变量绑定测试

    [Fact]
    public void Match_VariableBinding_BindsAndUsesValue()
    {
        // Arrange
        var code = @"
            func describe(value:int) -> string {
                return match value {
                    case 0 -> ""zero""
                    case x -> ""value is "" + x.ToStr()
                }
            }

            Assert.True(describe(42) == ""value is 42"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 表达式匹配测试

    [Fact]
    public void Match_WithExpression_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            func testSum(x:int, y:int) -> string {
                return match x + y {
                    case 8 -> ""eight""
                    case 10 -> ""ten""
                    case _ -> ""other""
                }
            }

            Assert.True(testSum(5, 3) == ""eight"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_ComplexExpression_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func calculate(a:int, b:int, c:int) -> string {
                return match a * b + c {
                    case 10 -> ""ten""
                    case 20 -> ""twenty""
                    case 30 -> ""thirty""
                    case _ -> ""other""
                }
            }

            Assert.True(calculate(2, 5, 0) == ""ten"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 嵌套匹配测试

    [Fact]
    public void Match_NestedExpression_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func nested(value:int) -> string {
                return match value {
                    case 0 -> ""zero""
                    case x -> match x {
                        case 1 -> ""nested one""
                        case _ -> ""nested other""
                    }
                }
            }

            Assert.True(nested(1) == ""nested one"")
            Assert.True(nested(5) == ""nested other"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 返回不同类型测试

    [Fact]
    public void Match_ReturningDifferentTypes_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func mixedReturn(x:int) -> string {
                result <- match x {
                    case 0 -> 0
                    case 1 -> ""one""
                    case 2 -> true
                    case _ -> 3.14
                }
                return result.ToStr()
            }

            Assert.True(mixedReturn(1) == ""one"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 特殊场景测试

    [Fact]
    public void Match_FirstMatchingCaseExecutes_SubsequentIgnored()
    {
        // Arrange
        var code = @"
            func firstMatch(x:int) -> string {
                return match x {
                    case 1 -> ""first""
                    case 1 -> ""second""
                    case _ -> ""other""
                }
            }

            Assert.True(firstMatch(1) == ""first"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_InLoop_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func getDayNames() -> string {
                result <- """"
                for i <- 1, i <= 3, i <- i + 1 {
                    dayName <- match i {
                        case 1 -> ""Monday""
                        case 2 -> ""Tuesday""
                        case 3 -> ""Wednesday""
                        case _ -> ""Unknown""
                    }
                    result <- result + dayName + "" ""
                }
                return result
            }

            Assert.True(getDayNames() == ""Monday Tuesday Wednesday "")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_InCondition_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func categorizeAndCheck(x:int) -> bool {
                category <- match x {
                    case 0 -> ""zero""
                    case 1 -> ""one""
                    case _ -> ""many""
                }

                if category == ""many"" {
                    return true
                } else {
                    return false
                }
            }

            Assert.True(categorizeAndCheck(10) == true)
            Assert.True(categorizeAndCheck(1) == false)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void Match_EmptyCases_SingleWildcard()
    {
        // Arrange
        var code = @"
            func singleCase(x:int) -> string {
                return match x {
                    case _ -> ""always""
                }
            }

            Assert.True(singleCase(0) == ""always"")
            Assert.True(singleCase(999) == ""always"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Match_ManyCases_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func manyOptions(x:int) -> string {
                return match x {
                    case 0 -> ""zero""
                    case 1 -> ""one""
                    case 2 -> ""two""
                    case 3 -> ""three""
                    case 4 -> ""four""
                    case 5 -> ""five""
                    case _ -> ""many""
                }
            }

            Assert.True(manyOptions(3) == ""three"")
            Assert.True(manyOptions(10) == ""many"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
