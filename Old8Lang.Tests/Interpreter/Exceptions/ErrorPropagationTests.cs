using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Old8Lang.Error;

namespace Old8Lang.Tests.Interpreter.Exceptions;

/// <summary>
/// 错误传播测试
/// 测试错误在不同作用域、函数调用链中的传播机制
/// </summary>
public class ErrorPropagationTests
{
    [Fact]
    public void ErrorPropagation_FunctionCallChain_PropagatesThroughCallStack()
    {
        // Arrange
        var code = @"
            func divide_by_zero() -> int {
                return 10 / 0
            }

            func middle_function() -> int {
                return divide_by_zero() + 5
            }

            func top_function() -> int {
                return middle_function() * 2
            }

            try {
                result <- top_function()
            } catch (e) {
                caught <- true
                error_message <- e
            }
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ex = Assert.Throws<ZeroDivisionError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });

        // 验证错误传播了整个调用栈
        Assert.NotNull(ex);
    }

    [Fact]
    public void ErrorPropagation_NestedTryCatch_PropagatesCorrectly()
    {
        // Arrange
        var code = @"
            func inner_function() -> int {
                try {
                    return 5 / 0
                } catch (e) {
                    // 在内部捕获并重新抛出
                    throw e
                }
            }

            func outer_function() -> int {
                try {
                    return inner_function() + 10
                } catch (e) {
                    // 在外部捕获
                    return -1
                }
            }

            result <- outer_function()
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
    public void ErrorPropagation_ScopeBoundary_ErrorCrossesScope()
    {
        // Arrange
        var code = @"
            global_var <- 100

            try {
                // 外层作用域
                outer_var <- 200
                try {
                    // 内层作用域
                    inner_var <- 300
                    result <- global_var + outer_var + inner_var + (10 / 0)
                } catch inner_e {
                    scope_caught <- true
                    throw inner_e  // 重新抛出
                }
            } catch outer_e {
                outer_scope_caught <- true
                final_result <- global_var  // 可以访问全局变量
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var outerScopeCaught = interpreter.Manager.GetValue(new LangId("outer_scope_caught"));
        var finalResult = interpreter.Manager.GetValue(new LangId("final_result"));

        Assert.NotNull(outerScopeCaught);
        Assert.IsType<BoolLangValue>(outerScopeCaught);
        Assert.True(((BoolLangValue)outerScopeCaught).Value);

        Assert.NotNull(finalResult);
        Assert.IsType<IntLangValue>(finalResult);
        Assert.Equal(100, ((IntLangValue)finalResult).Value);
    }

    [Fact]
    public void ErrorPropagation_ConditionalError_DependentOnInput()
    {
        // Arrange
        var code = @"
            func process_input(input_type: string) -> string {
                try {
                    if input_type == ""divide"" {
                        return ""Result: "" + (100 / 0).ToStr()
                    } else if input_type == ""type"" {
                        return ""Result: "" + (""hello"" + 5).ToStr()
                    } else {
                        return ""No error for: "" + input_type
                    }
                } catch (e) {
                    return ""Error in "" + input_type + "": "" + e
                }
            }

            result1 <- process_input(""divide"")
            result2 <- process_input(""type"")
            result3 <- process_input(""normal"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.All([result1, result2, result3], r =>
        {
            Assert.NotNull(r);
            Assert.IsType<StringLangValue>(r);
        });

        Assert.Contains("Error in", ((StringLangValue)result1).Value);
        Assert.Contains("Error in", ((StringLangValue)result2).Value);
        Assert.Equal("No error for: normal", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void ErrorPropagation_ArrayAccess_ErrorPropagatesFromIndex()
    {
        // Arrange
        var code = @"
            func get_array_element(arr: list, index: int) -> int {
                try {
                    return arr[index]
                } catch (e) {
                    return -1  // 简单的错误处理
                }
            }

            func process_data(data: list) -> int {
                try {
                    element <- get_array_element(data, 10)  // 故意使用超出范围的索引
                    return element * 2
                } catch (e) {
                    return -1  // 错误处理
                }
            }

            small_array <- {1, 2, 3}
            result <- process_data(small_array)
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
    public void ErrorPropagation_ClassMethod_PropagatesThroughMethodChain()
    {
        // Arrange
        var code = @"
            class Calculator {
                func Init(value: int) {
                    self.value <- value
                }

                func Divide(divisor: int) -> Calculator {
                    if divisor == 0 {
                        throw ""Division by zero in Calculator""
                    }
                    self.value <- self.value / divisor
                    return self
                }

                func Multiply(factor: int) -> Calculator {
                    self.value <- self.value * factor
                    return self
                }

                func GetValue() -> int {
                    return self.value
                }
            }

            try {
                calc <- Calculator()
                calc.Init(100)
                result <- calc.Divide(0).Multiply(2).GetValue()
            } catch (e) {
                error_handled <- true
                error_caught <- e
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var errorHandled = interpreter.Manager.GetValue(new LangId("error_handled"));
        var errorCaught = interpreter.Manager.GetValue(new LangId("error_caught"));

        Assert.NotNull(errorHandled);
        Assert.IsType<BoolLangValue>(errorHandled);
        Assert.True(((BoolLangValue)errorHandled).Value);

        Assert.NotNull(errorCaught);
        // 错误可能是StringLangValue或ErrorLangValue类型
        if (errorCaught is StringLangValue stringError)
        {
            Assert.Contains("Division by zero", stringError.Value);
        }
        else
        {
            Assert.IsType<ErrorLangValue>(errorCaught);
        }
    }

    [Fact]
    public void ErrorPropagation_ForLoopError_PropagatesFromLoopBody()
    {
        // Arrange
        var code = @"
            func process_collection(items: list) -> int {
                total <- 0
                i <- 0
                while i < items.Length() {
                    item <- items[i]
                    if item == ""error"" {
                        throw ""Error processing item at index "" + i.ToStr()
                    }
                    total <- total + item
                    i <- i + 1
                }
                return total
            }

            try {
                numbers <- {10, 20, ""error"", 40}
                result <- process_collection(numbers)
            } catch (e) {
                loop_error_handled <- true
                loop_error_message <- e
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var loopErrorHandled = interpreter.Manager.GetValue(new LangId("loop_error_handled"));
        var loopErrorMessage = interpreter.Manager.GetValue(new LangId("loop_error_message"));

        Assert.NotNull(loopErrorHandled);
        Assert.IsType<BoolLangValue>(loopErrorHandled);
        Assert.True(((BoolLangValue)loopErrorHandled).Value);

        Assert.NotNull(loopErrorMessage);
        // 错误可能是StringLangValue或ErrorLangValue类型
        if (loopErrorMessage is StringLangValue stringError)
        {
            Assert.Contains("Error processing item", stringError.Value);
        }
        else
        {
            Assert.IsType<ErrorLangValue>(loopErrorMessage);
        }
    }

    [Fact]
    public void ErrorPropagation_AssignmentError_PropagatesFromAssignment()
    {
        // Arrange
        var code = @"
            func assign_value(value) -> string {
                try {
                    // 尝试类型不匹配的赋值
                    if value == ""invalid"" {
                        int_var <- ""string_value""  // 这应该导致类型错误
                    } else {
                        int_var <- 42
                    }
                    return ""Assignment successful: "" + int_var.ToStr()
                } catch (e) {
                    return ""Assignment failed: "" + e
                }
            }

            result1 <- assign_value(42)
            result2 <- assign_value(""invalid"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result1);
        Assert.IsType<StringLangValue>(result2);

        Assert.Contains("successful", ((StringLangValue)result1).Value);
        Assert.Contains("failed", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void ErrorPropagation_MemberAccessError_PropagatesFromMemberAccess()
    {
        // Arrange
        var code = @"
            func get_member_info(obj) -> string {
                try {
                    // 尝试访问不存在的成员
                    if obj == ""test_null"" {
                        null_obj <- null
                        return null_obj.nonexistent_member.ToStr()
                    } else {
                        return ""Object is not null""
                    }
                } catch (e) {
                    return ""Member access failed: "" + e
                }
            }

            result1 <- get_member_info(""valid_object"")
            result2 <- get_member_info(""test_null"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result1);
        Assert.IsType<StringLangValue>(result2);

        Assert.Equal("Object is not null", ((StringLangValue)result1).Value);
        Assert.Contains("Member access failed", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void ErrorPropagation_CompoundOperation_ErrorPropagatesThroughComplexExpression()
    {
        // Arrange
        var code = @"
            func complex_calculation(a: int, b: int, c: int) -> int {
                try {
                    // 复杂的嵌套操作
                    step1 <- a + b
                    step2 <- step1 * c
                    step3 <- 100 / (c - b)  // 当c=b时会除零
                    step4 <- step3 + step2 - a
                    return step4
                } catch (e) {
                    return -999  // 错误指示值
                }
            }

            result1 <- complex_calculation(10, 20, 5)   // 正常情况
            result2 <- complex_calculation(10, 20, 20)  // 除零情况
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result1);
        Assert.IsType<IntLangValue>(result2);

        Assert.NotEqual(-999, ((IntLangValue)result1).Value); // 正常计算
        Assert.Equal(-999, ((IntLangValue)result2).Value); // 错误情况
    }
}