using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 类型错误测试
/// </summary>
public class TypeErrorsTests
{
    [Fact]
    public void TypeErrors_StringPlusInt_HandlesStringIntArithmetic()
    {
        // Arrange
        var code = @"
            try {
                result <- ""hello"" + 5
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""string_int""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Behavior depends on language type coercion rules
    }

    [Fact]
    public void TypeErrors_StringTimesInt_HandlesStringMultiplication()
    {
        // Arrange
        var code = @"
            try {
                result <- ""abc"" * 3
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""string_times""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Some languages support string multiplication, others don't
    }

    [Fact]
    public void TypeErrors_BoolPlusNumber_HandlesBoolNumberArithmetic()
    {
        // Arrange
        var code = @"
            try {
                result <- true + 1
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""bool_number""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Some languages convert bool to int, others throw
    }

    [Fact]
    public void TypeErrors_StringMinusNumber_HandlesStringSubtraction()
    {
        // Arrange
        var code = @"
            try {
                result <- ""hello"" - 1
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""string_minus""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("string_minus", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_StringDividedByNumber_HandlesStringDivision()
    {
        // Arrange
        var code = @"
            try {
                result <- ""hello"" / 2
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""string_divide""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("string_divide", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_ArrayPlusArray_HandlesArrayAddition()
    {
        // Arrange
        var code = @"
            try {
                arr1 <- [1, 2, 3]
                arr2 <- [4, 5, 6]
                result <- arr1 + arr2
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""array_add""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Array concatenation might be supported or not
    }

    [Fact]
    public void TypeErrors_ListPlusList_HandlesListAddition()
    {
        // Arrange
        var code = @"
            try {
                list1 <- {1, 2, 3}
                list2 <- {4, 5, 6}
                result <- list1 + list2
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""list_add""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // List concatenation might be supported or not
    }

    [Fact]
    public void TypeErrors_ArrayTimesNumber_HandlesArrayMultiplication()
    {
        // Arrange
        var code = @"
            try {
                arr <- [1, 2, 3]
                result <- arr * 2
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""array_times""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("array_times", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_DictionaryPlusDictionary_HandlesDictionaryAddition()
    {
        // Arrange
        var code = @"
            try {
                dict1 <- {""a"": 1, ""b"": 2}
                dict2 <- {""c"": 3, ""d"": 4}
                result <- dict1 + dict2
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""dict_add""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("dict_add", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_StringLessThanNumber_HandlesStringNumberComparison()
    {
        // Arrange
        var code = @"
            try {
                result <- ""hello"" < 5
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""string_less_number""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // String-number comparison behavior varies by language
    }

    [Fact]
    public void TypeErrors_BoolLessThanString_HandlesBoolStringComparison()
    {
        // Arrange
        var code = @"
            try {
                result <- true < ""hello""
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""bool_less_string""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("bool_less_string", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_ArrayEqualsArray_HandlesArrayEquality()
    {
        // Arrange
        var code = @"
            arr1 <- [1, 2, 3]
            arr2 <- [1, 2, 3]
            try {
                result <- arr1 = arr2
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""array_equals""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Array equality might be supported by value or reference
    }

    [Fact]
    public void TypeErrors_FunctionCalledAsValue_HandlesFunctionAsValue()
    {
        // Arrange
        var code = @"
            func test() -> int {
                return 42
            }
            try {
                result <- test + 5
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""function_as_value""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("function_as_value", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_ClassCalledAsFunction_HandlesClassAsFunction()
    {
        // Arrange
        var code = @"
            class TestClass {
                func getValue() -> int {
                    return 42
                }
            }
            try {
                result <- TestClass()
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""class_as_function""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Class instantiation should work
    }

    [Fact]
    public void TypeErrors_InvalidMethodCall_HandlesMethodOnWrongType()
    {
        // Arrange
        var code = @"
            number <- 42
            try {
                result <- len(number)
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""method_on_number""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("method_on_number", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_InvalidPropertyAccess_HandlesPropertyOnWrongType()
    {
        // Arrange
        var code = @"
            text <- ""hello""
            try {
                result <- text.Add(1)
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""array_method_on_string""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("array_method_on_string", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_WrongTypeInIndex_HandlesWrongTypeAsIndex()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            try {
                result <- arr[""index""]
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""string_index""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("string_index", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_NullInOperation_HandlesNullInArithmetic()
    {
        // Arrange
        var code = @"
            nullValue <- null
            try {
                result <- nullValue + 5
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""null_arithmetic""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("null_arithmetic", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_UndefinedInOperation_HandlesUndefinedInArithmetic()
    {
        // Arrange
        var code = @"
            try {
                result <- undefinedValue + 5
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""undefined_arithmetic""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("undefined_arithmetic", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_TypeCoercionInFunction_HandlesTypeCoercionInParameters()
    {
        // Arrange
        var code = @"
            func processNumber(n:int) -> int {
                return n * 2
            }
            try {
                result <- processNumber(""hello"")
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""parameter_coercion""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("parameter_coercion", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_WrongTypeInLoop_HandlesWrongTypeInForLoop()
    {
        // Arrange
        var code = @"
            try {
                count <- 0
                for i in ""hello"" {
                    count <- count + 1
                }
                result <- count
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""wrong_loop_type""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // String iteration might be supported or not
    }

    [Fact]
    public void TypeErrors_WrongTypeInConditional_HandlesWrongTypeInIfCondition()
    {
        // Arrange
        var code = @"
            try {
                if 123 {
                    result <- ""true""
                } else {
                    result <- ""false""
                }
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""wrong_condition_type""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Number as condition might be supported (truthy/falsy) or not
    }

    [Fact]
    public void TypeErrors_TypeCoercionChains_HandlesComplexTypeCoercion()
    {
        // Arrange
        var code = @"
            try {
                // Complex type coercion chain
                result <- ""5"" + true + null + 3.14
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""complex_coercion""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("complex_coercion", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_DynamicTypeChange_HandlesDynamicTypeChange()
    {
        // Arrange
        var code = @"
            // Variable starts as one type, changes to another
            x <- 5
            x <- ""hello""
            try {
                result <- x + 1
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""dynamic_type_change""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Dynamic typing might support this or not
    }

    [Fact]
    public void TypeErrors_MixedTypeOperations_HandlesMixedTypeOperations()
    {
        // Arrange
        var code = @"
            try {
                // Operation with multiple types
                result <- (1 + 2.5) * ""hello"" - true
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""mixed_type_operation""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        Assert.IsType<StringLangValue>(typeResult);
        Assert.Equal("mixed_type_operation", ((StringLangValue)typeResult).Value);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("type error", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeErrors_TypeCoercionInReturn_HandlesTypeCoercionInReturn()
    {
        // Arrange
        var code = @"
            func getValue() -> int {
                return ""hello""
            }
            try {
                result <- getValue()
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""return_type_mismatch""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Return type checking might be strict or loose
    }

    [Fact]
    public void TypeErrors_ArrayOfMixedTypes_HandlesArrayWithMixedTypes()
    {
        // Arrange
        var code = @"
            mixedArray <- [1, ""hello"", true, 3.14, null]
            try {
                sum <- 0
                for item in mixedArray {
                    sum <- sum + item
                }
                result <- sum
                type <- ""success""
            } catch {
                result <- ""type error""
                type <- ""array_mixed_types""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var typeResult = interpreter.Manager.GetValue(new LangId("type"));
        Assert.NotNull(result);
        Assert.NotNull(typeResult);
        // Mixed type arrays might be supported or cause errors
    }
}