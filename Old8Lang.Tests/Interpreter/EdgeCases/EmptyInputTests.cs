using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 空输入测试
/// </summary>
public class EmptyInputTests
{
    [Fact]
    public void EmptyInput_EmptyCode_HandlesEmptyCode()
    {
        // Arrange
        var code = @"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        // Should handle empty code gracefully without errors
    }

    [Fact]
    public void EmptyInput_EmptyStatement_HandlesEmptyStatement()
    {
        // Arrange
        var code = @"

        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        // Should handle whitespace-only code gracefully
    }

    [Fact]
    public void EmptyInput_EmptyStringLiteral_HandlesEmptyString()
    {
        // Arrange
        var code = @"
            emptyString <- """"
            result <- emptyString.Length
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyArray_HandlesEmptyArray()
    {
        // Arrange
        var code = @"
            emptyArray <- []
            result <- emptyArray.Length
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyList_HandlesEmptyList()
    {
        // Arrange
        var code = @"
            emptyList <- {}
            result <- emptyList.Length
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyDictionary_HandlesEmptyDictionary()
    {
        // Arrange
        var code = @"
            emptyDict <- {}
            result <- emptyDict.Count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyTuple_HandlesEmptyTuple()
    {
        // Arrange
        var code = @"
            emptyTuple <- ()
            result <- emptyTuple.Length
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyRange_HandlesEmptyRange()
    {
        // Arrange
        var code = @"
            emptyRange <- 5..<5
            count <- 0
            for i in emptyRange {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyFunction_HandlesEmptyFunction()
    {
        // Arrange
        var code = @"
            func emptyFunc() -> void {
                // Empty function body
            }
            emptyFunc()
            result <- ""function called""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("function called", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyClass_HandlesEmptyClass()
    {
        // Arrange
        var code = @"
            class EmptyClass {
                // Empty class body
            }
            emptyObj <- EmptyClass()
            result <- emptyObj != null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyInterface_HandlesEmptyInterface()
    {
        // Arrange
        var code = @"
            interface EmptyInterface {
                // Empty interface
            }
            class TestClass < EmptyInterface {
                // Implementation
            }
            testObj <- TestClass()
            result <- testObj != null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyLambda_HandlesEmptyLambda()
    {
        // Arrange
        var code = @"
            emptyLambda <- () -> void
            emptyLambda()
            result <- ""lambda called""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("lambda called", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyTryCatch_HandlesEmptyTryCatch()
    {
        // Arrange
        var code = @"
            try {
                // Empty try block
            } catch {
                // Empty catch block
            }
            result <- ""try-catch executed""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("try-catch executed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyFinally_HandlesEmptyFinally()
    {
        // Arrange
        var code = @"
            try {
                result <- ""try""
            } finally {
                // Empty finally block
            }
            finalResult <- result + "" + ""finally""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var finalResult = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(finalResult);
        Assert.IsType<StringLangValue>(finalResult);
        Assert.Equal("try finally", ((StringLangValue)finalResult).Value);
    }

    [Fact]
    public void EmptyInput_EmptyLoop_HandlesEmptyLoopBody()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in 1..5 {
                // Empty loop body
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyWhile_HandlesEmptyWhileBody()
    {
        // Arrange
        var code = @"
            counter <- 0
            while counter < 3 {
                counter <- counter + 1
                // Empty additional body
            }
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptySwitch_HandlesEmptySwitch()
    {
        // Arrange
        var code = @"
            value <- 5
            switch value {
                case 1:
                case 2:
                case 3:
                default:
                    result <- ""default case""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("default case", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyIf_HandlesEmptyIfBranches()
    {
        // Arrange
        var code = @"
            condition <- true
            if condition {
                // Empty if block
            } else {
                // Empty else block
            }
            result <- ""if-else executed""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("if-else executed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyBlock_HandlesEmptyBlockStatement()
    {
        // Arrange
        var code = @"
            {
                // Empty block
            }
            result <- ""block executed""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("block executed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_CommentsOnly_HandlesCommentsOnlyCode()
    {
        // Arrange
        var code = @"
            // This is a comment
            /* This is a block comment */
            result <- ""comments processed""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("comments processed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyStringTemplate_HandlesEmptyStringTemplate()
    {
        // Arrange
        var code = @"
            name <- """"
            result <- $""Hello {name}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello ", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyListIteration_HandlesEmptyListIteration()
    {
        // Arrange
        var code = @"
            emptyList <- {}
            sum <- 0
            for item in emptyList {
                sum <- sum + item
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyDictionaryIteration_HandlesEmptyDictIteration()
    {
        // Arrange
        var code = @"
            emptyDict <- {}
            count <- 0
            for key in emptyDict.Keys {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyStringIndexing_HandlesEmptyStringIndexing()
    {
        // Arrange
        var code = @"
            emptyString <- """"
            try {
                char <- emptyString[0]
                result <- ""has char""
            } catch {
                result <- ""index error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("index error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyArrayAccess_HandlesEmptyArrayAccess()
    {
        // Arrange
        var code = @"
            emptyArray <- []
            try {
                value <- emptyArray[0]
                result <- ""has value""
            } catch {
                result <- ""access error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("access error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyListAccess_HandlesEmptyListAccess()
    {
        // Arrange
        var code = @"
            emptyList <- {}
            try {
                value <- emptyList[0]
                result <- ""has value""
            } catch {
                result <- ""access error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("access error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyParameterList_HandlesEmptyParameterList()
    {
        // Arrange
        var code = @"
            func noParams() -> string {
                return ""no parameters""
            }
            result <- noParams()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("no parameters", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyConstructor_HandlesEmptyConstructor()
    {
        // Arrange
        var code = @"
            class TestClass {
                func Init() {
                    // Empty constructor
                }
            }
            testObj <- TestClass()
            result <- testObj != null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyMethod_HandlesEmptyMethod()
    {
        // Arrange
        var code = @"
            class TestClass {
                func EmptyMethod() -> void {
                    // Empty method
                }
            }
            testObj <- TestClass()
            testObj.EmptyMethod()
            result <- ""method called""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("method called", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyInput_EmptyReturn_HandlesEmptyReturn()
    {
        // Arrange
        var code = @"
            func testReturn() -> void {
                return
            }
            testReturn()
            result <- ""return executed""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("return executed", ((StringLangValue)result).Value);
    }
}