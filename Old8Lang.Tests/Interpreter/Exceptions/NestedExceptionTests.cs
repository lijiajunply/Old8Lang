using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Exceptions;

/// <summary>
/// 嵌套异常测试
/// 测试try-catch块的嵌套、多层异常处理、异常链等
/// </summary>
public class NestedExceptionTests
{
    [Fact]
    public void NestedException_SimpleNesting_HandlesInnerException()
    {
        // Arrange
        var code = @"
            outer_result <- """"

            try {
                try {
                    result <- 1 / 0  // 内层异常
                } catch (e1) {
                    outer_result <- ""Inner caught""
                }
            } catch (e2) {
                outer_result <- ""Outer caught""
            }

            final_result <- outer_result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("final_result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Inner caught", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NestedException_BubbleUp_OuterCatchesInner()
    {
        // Arrange
        var code = @"
            outer_result <- """"

            try {
                try {
                    // 内层不处理异常，让它冒泡
                    result <- 1 / 0
                } catch (e1) {
                    // 检查异常类型，如果不是我们想要的类型就重新抛出
                    outer_result <- ""Should not reach here""
                }
            } catch (e2) {
                outer_result <- ""Outer caught""
            }

            final_result <- outer_result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("final_result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Outer caught", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NestedException_MultipleLevels_DeepNesting()
    {
        // Arrange
        var code = @"
            level1_result <- """"
            level2_result <- """"

            try {
                try {
                    try {
                        result <- 10 / 0  // 最深层异常
                    } catch (e3) {
                        level3_result <- ""Level 3 caught""
                        // 简单处理，不重新抛出
                    }
                } catch (e2) {
                    level2_result <- ""Level 2 caught""
                }
            } catch (e1) {
                level1_result <- ""Level 1 caught""
            }

            final_result <- level3_result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("final_result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Level 3 caught", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NestedException_DifferentExceptionTypes_TypeSpecificHandling()
    {
        // Arrange
        var code = @"
            results <- {}

            try {
                try {
                    // 类型转换错误
                    value <- ""hello"" + 0
                } catch (e1) {
                    results.type_error <- ""Type error caught""
                }
            } catch (e2) {
                results.outer_catch <- ""Outer caught type error""
            }

            try {
                try {
                    // 除零错误
                    result <- 5 / 0
                } catch (e3) {
                    results.division_error <- ""Division error caught""
                }
            } catch (e4) {
                results.outer_catch2 <- ""Outer caught division error""
            }

            final_results <- results
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var results = interpreter.Manager.GetValue(new LangId("final_results"));
        Assert.NotNull(results);

        // 检查类型错误是否被内层捕获
        var typeError = interpreter.Manager.GetValue(new LangId("results.type_error"));
        Assert.NotNull(typeError);
        Assert.IsType<StringLangValue>(typeError);
        Assert.Equal("Type error caught", ((StringLangValue)typeError).Value);

        // 检查除零错误是否被内层捕获
        var divisionError = interpreter.Manager.GetValue(new LangId("results.division_error"));
        Assert.NotNull(divisionError);
        Assert.IsType<StringLangValue>(divisionError);
        Assert.Equal("Division error caught", ((StringLangValue)divisionError).Value);
    }

    [Fact]
    public void NestedException_NestedInLoops_ExceptionInIteration()
    {
        // Arrange
        var code = @"
            iteration_results <- {}
            i <- 0
            success_count <- 0
            error_count <- 0

            while i < 5 {
                try {
                    try {
                        if i == 2 {
                            error_result <- 10 / (i - 2)  // 当i=2时除零
                        } else {
                            normal_result <- i * 2
                            success_count <- success_count + 1
                        }
                    } catch (inner) {
                        error_count <- error_count + 1
                    }
                } catch (outer) {
                    // 外层通常不会捕获到，因为内层已经处理
                }
                i <- i + 1
            }

            final_success_count <- success_count
            final_error_count <- error_count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var successCount = interpreter.Manager.GetValue(new LangId("final_success_count"));
        var errorCount = interpreter.Manager.GetValue(new LangId("final_error_count"));

        Assert.NotNull(successCount);
        Assert.NotNull(errorCount);
        Assert.IsType<IntLangValue>(successCount);
        Assert.IsType<IntLangValue>(errorCount);

        // 5次迭代中，4次成功，1次错误（i=2时）
        Assert.Equal(4, ((IntLangValue)successCount).Value);
        Assert.Equal(1, ((IntLangValue)errorCount).Value);
    }

    [Fact]
    public void NestedException_NestedInFunctions_FunctionCallChaining()
    {
        // Arrange
        var code = @"
            func inner_function() -> string {
                try {
                    try {
                        result <- 5 / 0
                        return ""inner_success""
                    } catch (e1) {
                        return ""inner_caught""
                    }
                } catch (e2) {
                    return ""inner_outer_caught""
                }
            }

            func outer_function() -> string {
                try {
                    result <- inner_function()
                    return ""outer_success""
                } catch (e3) {
                    return ""outer_caught""
                }
            }

            try {
                final_result <- outer_function()
            } catch (e4) {
                final_result <- ""main_caught""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("final_result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("inner_caught", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NestedException_ConditionalNesting_DynamicExceptionHandling()
    {
        // Arrange
        var code = @"
            conditional_results <- {}
            use_nested <- true

            try {
                if use_nested {
                    try {
                        if true {
                            result <- 8 / 0
                        } else {
                            result <- 10 / 2
                        }
                    } catch (inner) {
                        conditional_results.nested_handled <- true
                    }
                } else {
                    result <- 10 / 2
                }
            } catch (outer) {
                conditional_results.outer_handled <- true
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("conditional_results.nested_handled"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);

        // 外层不应该捕获到异常
        var outerResult = interpreter.Manager.GetValue(new LangId("conditional_results.outer_handled"));
        Assert.Null(outerResult);
    }

    [Fact]
    public void NestedException_ResourceManagement_CleanupOrder()
    {
        // Arrange
        var code = @"
            cleanup_order <- {}

            try {
                // 外层资源
                resource1 <- ""resource1_opened""
                cleanup_order <- cleanup_order.Push(""resource1_opened"")

                try {
                    // 内层资源
                    resource2 <- ""resource2_opened""
                    cleanup_order <- cleanup_order.Push(""resource2_opened"")

                    try {
                        // 最深层操作，可能抛出异常
                        result <- 7 / 0
                    } catch (e3) {
                        cleanup_order <- cleanup_order.Push(""deep_error_caught"")
                    } finally {
                        cleanup_order <- cleanup_order.Push(""resource2_cleanup"")
                    }
                } catch (e2) {
                    cleanup_order <- cleanup_order.Push(""middle_error_caught"")
                } finally {
                    cleanup_order <- cleanup_order.Push(""resource1_cleanup"")
                }
            } catch (e1) {
                cleanup_order <- cleanup_order.Push(""outer_error_caught"")
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("cleanup_order"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var order = ((ListLangValue)result).GetItems().ToList();
        Assert.True(order.Count >= 4);

        // 验证资源清理顺序
        var orderStrings = order.Cast<StringLangValue>().Select(v => v.Value).ToList();
        Assert.Contains("resource2_opened", orderStrings);
        Assert.Contains("resource2_cleanup", orderStrings);
        Assert.Contains("resource1_cleanup", orderStrings);
    }

    [Fact]
    public void NestedException_ParallelTryBlocks_MultipleExceptionHandling()
    {
        // Arrange
        var code = @"
            try {
                // 第一个try块
                try {
                    result1 <- 1 / 0
                } catch (e1) {
                    block1_result <- ""block1_caught""
                }

                // 第二个try块
                try {
                    result2 <- ""hello"" + 5
                } catch (e2) {
                    block2_result <- ""block2_caught""
                }

                // 第三个try块（成功）
                try {
                    result3 <- 10 / 2
                    block3_result <- ""block3_success""
                } catch (e3) {
                    block3_result <- ""block3_caught""
                }

            } catch (outer_e) {
                outer_result <- ""outer_caught""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var block1Result = interpreter.Manager.GetValue(new LangId("block1_result"));
        var block2Result = interpreter.Manager.GetValue(new LangId("block2_result"));
        var block3Result = interpreter.Manager.GetValue(new LangId("block3_result"));

        Assert.NotNull(block1Result);
        Assert.NotNull(block2Result);
        Assert.NotNull(block3Result);

        Assert.IsType<StringLangValue>(block1Result);
        Assert.IsType<StringLangValue>(block2Result);
        Assert.IsType<StringLangValue>(block3Result);

        Assert.Equal("block1_caught", ((StringLangValue)block1Result).Value);
        Assert.Equal("block2_caught", ((StringLangValue)block2Result).Value);
        Assert.Equal("block3_success", ((StringLangValue)block3Result).Value);
    }

    [Fact]
    public void NestedException_ExceptionSuppression_SwallowingExceptions()
    {
        // Arrange
        var code = @"
            suppression_results <- {}

            try {
                try {
                    try {
                        result <- 12 / 0
                    } catch (innermost) {
                        suppression_results.innermost_caught <- true
                        // 抑制异常，不重新抛出
                    }
                } catch (middle) {
                    suppression_results.middle_caught <- true
                }
            } catch (outermost) {
                suppression_results.outermost_caught <- true
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var innermostResult = interpreter.Manager.GetValue(new LangId("suppression_results.innermost_caught"));
        var middleResult = interpreter.Manager.GetValue(new LangId("suppression_results.middle_caught"));
        var outermostResult = interpreter.Manager.GetValue(new LangId("suppression_results.outermost_caught"));

        Assert.NotNull(innermostResult);
        Assert.IsType<BoolLangValue>(innermostResult);
        Assert.True(((BoolLangValue)innermostResult).Value);

        // 中层和外层不应该捕获到，因为内层已经抑制了异常
        Assert.Null(middleResult);
        Assert.Null(outermostResult);
    }

    [Fact]
    public void NestedException_DeepNestingWithFinally_FinallyExecutionOrder()
    {
        // Arrange
        var code = @"
            execution_log <- {}

            try {
                try {
                    try {
                        execution_log <- execution_log.Push(""level3_try"")
                        result <- 1 / 0
                    } catch (e3) {
                        execution_log <- execution_log.Push(""level3_catch"")
                    } finally {
                        execution_log <- execution_log.Push(""level3_finally"")
                    }
                } catch (e2) {
                    execution_log <- execution_log.Push(""level2_catch"")
                } finally {
                    execution_log <- execution_log.Push(""level2_finally"")
                }
            } catch (e1) {
                execution_log <- execution_log.Push(""level1_catch"")
            } finally {
                execution_log <- execution_log.Push(""level1_finally"")
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("execution_log"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var log = ((ListLangValue)result).GetItems().ToList();
        var logStrings = log.Cast<StringLangValue>().Select(v => v.Value).ToList();

        // 验证执行顺序
        Assert.Contains("level3_try", logStrings);
        Assert.Contains("level3_catch", logStrings);
        Assert.Contains("level3_finally", logStrings);
        Assert.Contains("level2_finally", logStrings);
        Assert.Contains("level1_finally", logStrings);

        // level3_finally应该在level2_finally之前
        var level3Index = logStrings.IndexOf("level3_finally");
        var level2Index = logStrings.IndexOf("level2_finally");
        Assert.True(level3Index < level2Index);
    }

    [Fact]
    public void NestedException_CustomExceptionHandling_ErrorClassification()
    {
        // Arrange
        var code = @"
            error_categories <- {}

            try {
                try {
                    // 模拟业务逻辑错误
                    if true {
                        throw ""Business logic error: insufficient funds""
                    }
                } catch (e1) {
                    if e1.Contains(""insufficient"") {
                        error_categories.business_error <- true
                    } else {
                        error_categories.other_error <- true
                    }
                }

                try {
                    // 模拟验证错误
                    if true {
                        throw ""Validation error: invalid age""
                    }
                } catch (e2) {
                    if e2.Contains(""validation"") {
                        error_categories.validation_error <- true
                    } else {
                        error_categories.other_error <- true
                    }
                }

            } catch (outer_e) {
                error_categories.unhandled_error <- true
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var businessError = interpreter.Manager.GetValue(new LangId("error_categories.business_error"));
        var validationError = interpreter.Manager.GetValue(new LangId("error_categories.validation_error"));
        var unhandledError = interpreter.Manager.GetValue(new LangId("error_categories.unhandled_error"));

        Assert.NotNull(businessError);
        Assert.NotNull(validationError);
        Assert.IsType<BoolLangValue>(businessError);
        Assert.IsType<BoolLangValue>(validationError);

        Assert.True(((BoolLangValue)businessError).Value);
        Assert.True(((BoolLangValue)validationError).Value);
        Assert.Null(unhandledError); // 应该没有被外层捕获
    }

    [Fact]
    public void NestedException_RecursiveFunction_ExceptionInRecursion()
    {
        // Arrange
        var code = @"
            recursion_log <- {}

            func recursive_func(n) -> string {
                recursion_log <- recursion_log.Push(""enter_level_"" + n.ToStr())
                try {
                    try {
                        if n == 0 {
                            result <- 10 / 0  // 递归基线处的异常
                        } else if n > 0 {
                            return recursive_func(n - 1)
                        } else {
                            return ""negative""
                        }
                    } catch (inner) {
                        recursion_log <- recursion_log.Push(""inner_catch_level_"" + n.ToStr())
                        return ""inner_caught_at_"" + n.ToStr()
                    }
                } catch (outer) {
                    recursion_log <- recursion_log.Push(""outer_catch_level_"" + n.ToStr())
                    return ""outer_caught_at_"" + n.ToStr()
                }
            }

            final_result <- recursive_func(3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("final_result"));
        var log = interpreter.Manager.GetValue(new LangId("recursion_log"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("inner_caught_at_0", ((StringLangValue)result).Value);

        Assert.NotNull(log);
        Assert.IsType<ListLangValue>(log);

        var logStrings = ((ListLangValue)log).GetItems().Cast<StringLangValue>().Select(v => v.Value).ToList();

        // 验证递归调用日志
        Assert.Contains("enter_level_3", logStrings);
        Assert.Contains("enter_level_2", logStrings);
        Assert.Contains("enter_level_1", logStrings);
        Assert.Contains("enter_level_0", logStrings);
        Assert.Contains("inner_catch_level_0", logStrings);
    }
}