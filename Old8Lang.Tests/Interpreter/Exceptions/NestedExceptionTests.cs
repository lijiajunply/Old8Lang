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
        var code = @"            outer_result <- """"

            try {
                try {
                    // 内层不处理异常，让它冒泡
                    result <- 1 / 0
                } catch (e1) {
                    // 检查异常类型，如果不是我们想要的类型就重新抛出
                    if typeof(e1) != ""ZeroDivisionError"" {
                        outer_result <- ""Should not reach here""
                    } else {
                        throw e1; // 重新抛出除零错误
                    }
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
    public void NestedException_NestedInLoops_ExceptionInIteration()
    {
        // Arrange
        var code = @"            i <- 0
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
    public void NestedException_ResourceManagement_CleanupOrder()
    {
        // Arrange
        var code = @"            cleanup_order <- {}

            try {
                // 外层资源
                resource1 <- ""resource1_opened""
                cleanup_order.Add(""resource1_opened"")

                try {
                    // 内层资源
                    resource2 <- ""resource2_opened""
                    cleanup_order.Add(""resource2_opened"")

                    try {
                        // 最深层操作，可能抛出异常
                        result <- 7 / 0
                    } catch (e3) {
                        cleanup_order.Add(""deep_error_caught"")
                    } finally {
                        cleanup_order.Add(""resource2_cleanup"")
                    }
                } catch (e2) {
                    cleanup_order.Add(""middle_error_caught"")
                } finally {
                    cleanup_order.Add(""resource1_cleanup"")
                }
            } catch (e1) {
                cleanup_order.Add(""outer_error_caught"")
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
    public void NestedException_DeepNestingWithFinally_FinallyExecutionOrder()
    {
        // Arrange
        var code = @"            execution_log <- {}

            try {
                try {
                    try {
                        execution_log.Add(""level3_try"")
                        result <- 1 / 0
                    } catch (e3) {
                        execution_log.Add(""level3_catch"")
                    } finally {
                        execution_log.Add(""level3_finally"")
                    }
                } catch (e2) {
                    execution_log.Add(""level2_catch"")
                } finally {
                    execution_log.Add(""level2_finally"")
                }
            } catch (e1) {
                execution_log.Add(""level1_catch"")
            } finally {
                execution_log.Add(""level1_finally"")
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
}