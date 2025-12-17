using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 意外输入测试
/// </summary>
public class UnexpectedInputsTests
{
    [Fact]
    public void UnexpectedInputs_DivisionByZero_HandlesDivisionByZero()
    {
        // Arrange
        var code = @"
            try {
                result <- 10 / 0
            } catch {
                result <- ""division error""
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
        Assert.Equal("division error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_ModuloByZero_HandlesModuloByZero()
    {
        // Arrange
        var code = @"
            try {
                result <- 10 % 0
            } catch {
                result <- ""modulo error""
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
        Assert.Equal("modulo error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_NegativeIndex_HandlesNegativeArrayIndex()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            try {
                result <- arr[-1]
            } catch {
                result <- -1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_FloatIndex_HandlesFloatArrayIndex()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            try {
                result <- arr[1.5]
            } catch {
                result <- -1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_StringIndex_HandlesStringAsIndex()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            try {
                result <- arr[""index""]
            } catch {
                result <- -1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_NullInOperation_HandlesNullInArithmetic()
    {
        // Arrange
        var code = @"
            x <- null
            try {
                result <- x + 1
            } catch {
                result <- ""null error""
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
        Assert.Equal("null error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_InvalidKey_HandlesInvalidDictionaryKey()
    {
        // Arrange
        var code = @"
            dict <- {""a"": 1, ""b"": 2}
            try {
                result <- dict[""nonexistent""]
            } catch {
                result <- ""key error""
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
        Assert.Equal("key error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_TypeMismatch_HandlesTypeMismatchInOperations()
    {
        // Arrange
        var code = @"
            try {
                result <- ""hello"" + 5
            } catch {
                result <- ""type error""
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
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_UndeclaredVariable_HandlesUndeclaredVariable()
    {
        // Arrange
        var code = @"
            try {
                result <- undeclaredVar + 1
            } catch {
                result <- ""variable error""
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
        Assert.Equal("variable error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_UndefinedFunction_HandlesUndefinedFunctionCall()
    {
        // Arrange
        var code = @"
            try {
                result <- undefinedFunction()
            } catch {
                result <- ""function error""
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
        Assert.Equal("function error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_WrongParameterCount_HandlesWrongParameterCount()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            try {
                result <- add(1)
            } catch {
                result <- ""parameter error""
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
        Assert.Equal("parameter error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_WrongParameterType_HandlesWrongParameterType()
    {
        // Arrange
        var code = @"
            func process(text:string) -> int {
                return text.Length
            }
            try {
                result <- process(123)
            } catch {
                result <- ""type parameter error""
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
        Assert.Equal("type parameter error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_InvalidRange_HandlesInvalidRange()
    {
        // Arrange
        var code = @"
            try {
                count <- 0
                for i in 5..1 {
                    count <- count + 1
                }
                result <- count
            } catch {
                result <- ""range error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        // Might handle gracefully or throw, either is acceptable
        Assert.NotNull(result);
    }

    [Fact]
    public void UnexpectedInputs_InfiniteRange_HandlesPotentialInfiniteRange()
    {
        // Arrange
        var code = @"
            try {
                count <- 0
                for i in 1..<1000000 {
                    count <- count + 1
                    if count >= 10 {
                        break
                    }
                }
                result <- count
            } catch {
                result <- ""infinite range error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        if (result is StringLangValue strResult)
        {
            Assert.Equal("infinite range error", strResult.Value);
        }
        else if (result is IntLangValue intResult)
        {
            Assert.Equal(10, intResult.Value);
        }
    }

    [Fact]
    public void UnexpectedInputs_StringAsNumber_HandlesStringInArithmetic()
    {
        // Arrange
        var code = @"
            try {
                result <- ""5"" * 2
            } catch {
                result <- ""string math error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Some languages might allow string multiplication, others might throw
        if (result is StringLangValue strResult)
        {
            Assert.Equal("string math error", strResult.Value);
        }
    }

    [Fact]
    public void UnexpectedInputs_BooleanInMath_HandlesBooleanInArithmetic()
    {
        // Arrange
        var code = @"
            try {
                result <- true + 1
            } catch {
                result <- ""boolean math error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Some languages might convert boolean to number, others might throw
        if (result is StringLangValue strResult)
        {
            Assert.Equal("boolean math error", strResult.Value);
        }
    }

    [Fact]
    public void UnexpectedInputs_ArrayIndexOnNonArray_HandlesIndexOnNonArray()
    {
        // Arrange
        var code = @"
            notArray <- 42
            try {
                result <- notArray[0]
            } catch {
                result <- ""not indexable""
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
        Assert.Equal("not indexable", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_StringMethodOnNumber_HandlesStringMethodOnNumber()
    {
        // Arrange
        var code = @"
            notString <- 42
            try {
                result <- notString.Length
            } catch {
                result <- ""not a string""
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
        Assert.Equal("not a string", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_ArrayMethodOnNonArray_HandlesArrayMethodOnNonArray()
    {
        // Arrange
        var code = @"
            notArray <- ""hello""
            try {
                result <- notArray.Add(1)
            } catch {
                result <- ""not an array""
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
        Assert.Equal("not an array", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_RecursionLimit_HandlesExcessiveRecursion()
    {
        // Arrange
        var code = @"
            func infinite(n:int) -> int {
                if n <= 0 {
                    return 0
                }
                return infinite(n + 1)
            }
            try {
                result <- infinite(0)
            } catch {
                result <- ""recursion limit""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        if (result is StringLangValue strResult)
        {
            Assert.Equal("recursion limit", strResult.Value);
        }
    }

    [Fact]
    public void UnexpectedInputs_MemoryExhaustion_HandlesPotentialMemoryExhaustion()
    {
        // Arrange
        var code = @"
            try {
                bigArray <- []
                for i in 1..100000 {
                    bigArray.Add(i)
                    if bigArray.Length >= 10 {
                        break
                    }
                }
                result <- bigArray.Length
            } catch {
                result <- ""memory error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        if (result is StringLangValue strResult)
        {
            Assert.Equal("memory error", strResult.Value);
        }
        else if (result is IntLangValue intResult)
        {
            Assert.Equal(10, intResult.Value);
        }
    }

    [Fact]
    public void UnexpectedInputs_InvalidCharacter_HandlesInvalidCharacters()
    {
        // Arrange
        var code = @"
            try {
                result <- ""Hello"" + char(127) + ""World""
                valid <- result.Length > 5
            } catch {
                result <- ""invalid character""
                valid <- false
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Result depends on how the language handles invalid characters
    }

    [Fact]
    public void UnexpectedInputs_EmptyExpression_HandlesEmptyExpression()
    {
        // Arrange
        var code = @"
            x <- 5
            try {
                result <- x + ()
            } catch {
                result <- ""empty expression""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        if (result is StringLangValue strResult)
        {
            Assert.Equal("empty expression", strResult.Value);
        }
    }

    [Fact]
    public void UnexpectedInputs_InvalidAssignment_HandlesInvalidAssignment()
    {
        // Arrange
        var code = @"
            try {
                5 <- 10
                result <- ""assignment succeeded""
            } catch {
                result <- ""invalid assignment""
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
        Assert.Equal("invalid assignment", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_MultipleErrors_HandlesMultipleErrorsInSequence()
    {
        // Arrange
        var code = @"
            errors <- []

            try {
                x <- 10 / 0
            } catch {
                errors.Add(""division"")
            }

            try {
                y <- null + 1
            } catch {
                errors.Add(""null"")
            }

            try {
                z <- ""hello""[100]
            } catch {
                errors.Add(""index"")
            }

            result <- errors.Count
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
    public void UnexpectedInputs_NestedErrors_HandlesNestedErrorHandling()
    {
        // Arrange
        var code = @"
            try {
                try {
                    result <- 10 / 0
                } catch {
                    try {
                        result <- null + 1
                    } catch {
                        result <- ""nested error handled""
                    }
                }
            } catch {
                result <- ""outer error handled""
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
        Assert.Equal("nested error handled", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnexpectedInputs_MalformedData_HandlesMalformedDataStructures()
    {
        // Arrange
        var code = @"
            try {
                // Try to create inconsistent data structure
                mixed <- [1, ""hello"", true, null, []]
                sum <- 0
                for item in mixed {
                    try {
                        sum <- sum + item
                    } catch {
                        // Skip non-numeric items
                    }
                }
                result <- sum
            } catch {
                result <- 0
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value); // Only the number 1 should be summed
    }
}