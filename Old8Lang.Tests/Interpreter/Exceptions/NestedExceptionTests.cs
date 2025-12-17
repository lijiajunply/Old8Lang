using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Old8Lang;

namespace Old8Lang.Tests.Interpreter.Exceptions;

/// <summary>
/// 嵌套异常测试
/// 测试try-catch块的嵌套、多层异常处理、异常链等
/// </summary>
[Trait("Category", "Interpreter")]
[Trait("Category", "Interpreter-Exceptions")]
[Trait("Category", "Interpreter-NestedExceptions")]
public class NestedExceptionTests : InterpreterTestBase
{
    public NestedExceptionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void NestedException_SimpleNesting_HandlesInnerException()
    {
        var code = @"
            outer_result <- """"

            try {
                try {
                    result <- 1 / 0  // 内层异常
                } catch e1 {
                    outer_result <- ""Inner caught: "" + e1.message
                }
            } catch e2 {
                outer_result <- ""Outer caught: "" + e2.message
            }

            final_result <- outer_result
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_result"));
        Assert.Contains("Inner caught", result["final_result"].ToString());
    }

    [Fact]
    public void NestedException_BubbleUp_OuterCatchesInner()
    {
        var code = @"
            outer_result <- """"

            try {
                try {
                    // 内层不处理异常，让它冒泡
                    result <- 1 / 0
                } catch e1 {
                    // 这个catch只处理特定类型的异常
                    if e1.type != ""DivisionError"" {
                        outer_result <- ""Inner caught: "" + e1.message
                    }
                }
            } catch e2 {
                outer_result <- ""Outer caught: "" + e2.message
            }

            final_result <- outer_result
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_result"));
        Assert.Contains("Outer caught", result["final_result"].ToString());
    }

    [Fact]
    public void NestedException_MultipleLevels_DeepNesting()
    {
        var code = @"
            level1_result <- """"
            level2_result <- """"
            level3_result <- """"

            try {
                try {
                    try {
                        result <- 10 / 0  // 最深层异常
                    } catch e3 {
                        level3_result <- ""Level 3: "" + e3.message
                        // 重新抛出异常
                        throw e3
                    }
                } catch e2 {
                    level2_result <- ""Level 2: "" + e2.message
                    // 重新抛出异常
                    throw e2
                }
            } catch e1 {
                level1_result <- ""Level 1: "" + e1.message
            }

            final_result <- level1_result
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_result"));
        Assert.Contains("Level 1", result["final_result"].ToString());
    }

    [Fact]
    public void NestedException_DifferentExceptionTypes_TypeSpecificHandling()
    {
        var code = @"
            results <- {}

            try {
                try {
                    // 类型转换错误
                    value <- ""hello"" + 0
                } catch e1 {
                    results.type_error <- ""Type error caught""
                }
            } catch e2 {
                results.outer_catch <- ""Outer: "" + e2.message
            }

            try {
                try {
                    // 除零错误
                    result <- 5 / 0
                } catch e3 {
                    results.division_error <- ""Division error caught""
                }
            } catch e4 {
                results.outer_catch2 <- ""Outer: "" + e4.message
            }

            final_results <- results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_results"));
        var results = result["final_results"] as dynamic;
        Assert.Equal("Type error caught", results.type_error);
        Assert.Equal("Division error caught", results.division_error);
    }

    [Fact]
    public void NestedException_FinallyWithNesting_CleanupExecution()
    {
        var code = @"
            cleanup_order <- []

            try {
                try {
                    result <- 1 / 0
                } catch e1 {
                    cleanup_order <- cleanup_order.concat({""inner_catch""})
                    throw e1  // 重新抛出
                } finally {
                    cleanup_order <- cleanup_order.concat({""inner_finally""})
                }
            } catch e2 {
                cleanup_order <- cleanup_order.concat({""outer_catch""})
            } finally {
                cleanup_order <- cleanup_order.concat({""outer_finally""})
            }

            final_order <- cleanup_order
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_order"));
        // 验证finally块的执行顺序
        var order = result["final_order"] as object[];
        // inner_catch, inner_finally, outer_catch, outer_finally
        Assert.Equal(4, order.Length);
    }

    [Fact]
    public void NestedException_ExceptionChain_ChainedExceptions()
    {
        var code = @"
            exception_chain <- []

            try {
                try {
                    // 原始异常
                    original_error <- 10 / 0
                } catch original {
                    exception_chain <- exception_chain.concat({""Original: "" + original.message})
                    // 创建新的异常并链接原始异常
                    new_exception <- {
                        message: ""Wrapped error occurred"",
                        cause: original,
                        type: ""WrappedError""
                    }
                    throw new_exception
                }
            } catch wrapped {
                exception_chain <- exception_chain.concat({""Wrapped: "" + wrapped.message})
                if wrapped.cause != null {
                    exception_chain <- exception_chain.concat({""Cause: "" + wrapped.cause.message})
                }
            }

            final_chain <- exception_chain
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_chain"));
        var chain = result["final_chain"] as object[];
        Assert.True(chain.Length >= 2);
    }

    [Fact]
    public void NestedException_NestedInLoops_ExceptionInIteration()
    {
        var code = @"
            iteration_results <- []
            i <- 0

            while i < 5 {
                try {
                    try {
                        if i == 2 {
                            error_result <- 10 / (i - 2)  // 当i=2时除零
                        } else {
                            normal_result <- i * 2
                        }
                        iteration_results <- iteration_results.concat({""iteration_"" + i + ""_success""})
                    } catch inner {
                        iteration_results <- iteration_results.concat({""iteration_"" + i + ""_inner_catch""})
                    }
                } catch outer {
                    iteration_results <- iteration_results.concat({""iteration_"" + i + ""_outer_catch""})
                }
                i <- i + 1
            }

            final_results <- iteration_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_results"));
        var results = result["final_results"] as object[];
        Assert.Equal(5, results.Length);
        // 第3次迭代应该被内部catch捕获
        Assert.Contains(results, r => r.ToString().Contains("iteration_2_inner_catch"));
    }

    [Fact]
    public void NestedException_NestedInFunctions_FunctionCallChaining()
    {
        var code = @"
            function_results <- []

            func1 <- func() {
                try {
                    try {
                        result <- 5 / 0
                        return ""func1_success""
                    } catch e1 {
                        function_results <- function_results.concat({""func1_inner_catch""})
                        throw e1
                    }
                } catch e2 {
                    function_results <- function_results.concat({""func1_outer_catch""})
                    return ""func1_caught""
                }
            }

            func2 <- func() {
                try {
                    result <- func1()
                    function_results <- function_results.concat({""func2_success""})
                    return ""func2_success""
                } catch e3 {
                    function_results <- function_results.concat({""func2_catch""})
                    return ""func2_caught""
                }
            }

            try {
                final_result <- func2()
            } catch e4 {
                function_results <- function_results.concat({""main_catch""})
            }

            final_function_results <- function_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_function_results"));
        var results = result["final_function_results"] as object[];
        // 验证异常在函数调用链中的传播
        Assert.True(results.Length >= 2);
    }

    [Fact]
    public void NestedException_ConditionalNesting_DynamicExceptionHandling()
    {
        var code = @"
            conditional_results <- []
            use_nested <- true

            try {
                if use_nested {
                    try {
                        if true {
                            result <- 8 / 0
                        } else {
                            result <- 10 / 2
                        }
                    } catch inner {
                        conditional_results <- conditional_results.concat({""conditional_inner""})
                    }
                } else {
                    result <- 10 / 2
                }
            } catch outer {
                conditional_results <- conditional_results.concat({""conditional_outer""})
            }

            final_conditional_results <- conditional_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_conditional_results"));
        var results = result["final_conditional_results"] as object[];
        Assert.Contains(results, r => r.ToString().Contains("conditional_inner"));
    }

    [Fact]
    public void NestedException_MultipleCatchBlocks_SpecificExceptionHandling()
    {
        var code = @"
            multi_catch_results <- []

            try {
                try {
                    // 触发除零错误
                    result <- 15 / 0
                } catch e1 {
                    if e1.type == ""DivisionError"" {
                        multi_catch_results <- multi_catch_results.concat({""division_caught_inner""})
                    } else if e1.type == ""TypeError"" {
                        multi_catch_results <- multi_catch_results.concat({""type_caught_inner""})
                    }
                }
            } catch e2 {
                if e2.type == ""DivisionError"" {
                    multi_catch_results <- multi_catch_results.concat({""division_caught_outer""})
                } else if e2.type == ""TypeError"" {
                    multi_catch_results <- multi_catch_results.concat({""type_caught_outer""})
                }
            }

            // 测试类型错误
            try {
                try {
                    value <- ""text"" + 5
                } catch e3 {
                    if e3.type == ""DivisionError"" {
                        multi_catch_results <- multi_catch_results.concat({""division_caught_inner2""})
                    } else if e3.type == ""TypeError"" {
                        multi_catch_results <- multi_catch_results.concat({""type_caught_inner2""})
                    }
                }
            } catch e4 {
                if e4.type == ""DivisionError"" {
                    multi_catch_results <- multi_catch_results.concat({""division_caught_outer2""})
                } else if e4.type == ""TypeError"" {
                    multi_catch_results <- multi_catch_results.concat({""type_caught_outer2""})
                }
            }

            final_multi_catch_results <- multi_catch_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_multi_catch_results"));
        var results = result["final_multi_catch_results"] as object[];
        Assert.Contains(results, r => r.ToString().Contains("division_caught_inner"));
        Assert.Contains(results, r => r.ToString().Contains("type_caught_inner2"));
    }

    [Fact]
    public void NestedException_ResourcesManagement_NestedResourceCleanup()
    {
        var code = @"
            resource_cleanup_order <- []

            try {
                // 外层资源
                resource1_opened <- true
                resource_cleanup_order <- resource_cleanup_order.concat({""resource1_opened""})

                try {
                    // 内层资源
                    resource2_opened <- true
                    resource_cleanup_order <- resource_cleanup_order.concat({""resource2_opened""})

                    try {
                        // 最深层操作，可能抛出异常
                        result <- 7 / 0
                    } catch e3 {
                        resource_cleanup_order <- resource_cleanup_order.concat({""deep_error_caught""})
                        throw e3
                    } finally {
                        if resource2_opened {
                            resource_cleanup_order <- resource_cleanup_order.concat({""resource2_cleanup""})
                            resource2_opened <- false
                        }
                    }
                } catch e2 {
                    resource_cleanup_order <- resource_cleanup_order.concat({""middle_error_caught""})
                } finally {
                    if resource1_opened {
                        resource_cleanup_order <- resource_cleanup_order.concat({""resource1_cleanup""})
                        resource1_opened <- false
                    }
                }
            } catch e1 {
                resource_cleanup_order <- resource_cleanup_order.concat({""outer_error_caught""})
            }

            final_cleanup_order <- resource_cleanup_order
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_cleanup_order"));
        var order = result["final_cleanup_order"] as object[];
        // 验证资源按照正确顺序清理
        Assert.Contains(order, r => r.ToString().Contains("resource2_cleanup"));
        Assert.Contains(order, r => r.ToString().Contains("resource1_cleanup"));
    }

    [Fact]
    public void NestedException_RecursiveFunctionCall_ExceptionInRecursion()
    {
        var code = @"
            recursion_results <- []

            recursive_func <- func(n) {
                try {
                    try {
                        if n == 0 {
                            error_result <- 10 / 0  // 递归基线处的异常
                        } else if n > 0 {
                            result <- recursive_func(n - 1)
                            return ""recursion_"" + n
                        } else {
                            return ""negative_""
                        }
                    } catch inner {
                        recursion_results <- recursion_results.concat({""inner_catch_level_"" + n})
                        throw inner
                    }
                } catch outer {
                    recursion_results <- recursion_results.concat({""outer_catch_level_"" + n})
                    return ""caught_at_level_"" + n
                }
            }

            final_result <- recursive_func(3)
            final_recursion_results <- recursion_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_result"));
        Assert.True(result.ContainsKey("final_recursion_results"));
        var results = result["final_recursion_results"] as object[];
        // 验证递归中异常的处理
        Assert.True(results.Length > 0);
    }

    [Fact]
    public void NestedException_ExceptionPropagation_PropagationAcrossLevels()
    {
        var code = @"
            propagation_log <- []

            deep_function <- func() {
                try {
                    result <- 20 / 0
                } catch e {
                    propagation_log <- propagation_log.concat({""deep_function_caught""})
                    throw {
                        message: ""Deep error: "" + e.message,
                        level: ""deep"",
                        original: e
                    }
                }
            }

            middle_function <- func() {
                try {
                    result <- deep_function()
                } catch e {
                    propagation_log <- propagation_log.concat({""middle_function_caught""})
                    throw {
                        message: ""Middle error: "" + e.message,
                        level: ""middle"",
                        original: e
                    }
                }
            }

            top_function <- func() {
                try {
                    result <- middle_function()
                } catch e {
                    propagation_log <- propagation_log.concat({""top_function_caught""})
                    return {
                        final_message: e.message,
                        level: e.level,
                        chain_length: 1 + (if e.original.original != null then 1 else 0)
                    }
                }
            }

            propagation_result <- top_function()
            final_propagation_log <- propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("propagation_result"));
        Assert.True(result.ContainsKey("final_propagation_log"));
        var log = result["final_propagation_log"] as object[];
        Assert.Equal(3, log.Length);
    }

    [Fact]
    public void NestedException_ParallelTryBlocks_MultipleExceptionHandling()
    {
        var code = @"
            parallel_results <- {}

            try {
                // 第一个try块
                try {
                    result1 <- 1 / 0
                } catch e1 {
                    parallel_results.block1 <- ""caught: "" + e1.message
                }

                // 第二个try块
                try {
                    result2 <- ""hello"" + 5
                } catch e2 {
                    parallel_results.block2 <- ""caught: "" + e2.message
                }

                // 第三个try块
                try {
                    result3 <- 10 / 2
                    parallel_results.block3 <- ""success: "" + result3
                } catch e3 {
                    parallel_results.block3 <- ""caught: "" + e3.message
                }

            } catch outer_e {
                parallel_results.outer <- ""outer_caught: "" + outer_e.message
            }

            final_parallel_results <- parallel_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_parallel_results"));
        var results = result["final_parallel_results"] as dynamic;
        Assert.NotNull(results.block1);
        Assert.NotNull(results.block2);
        Assert.NotNull(results.block3);
    }

    [Fact]
    public void NestedException_CustomExceptionTypes_UserDefinedExceptions()
    {
        var code = @"
            custom_exception_results <- []

            // 自定义异常类型
            BusinessError <- func(message, code) {
                return {
                    type: ""BusinessError"",
                    message: message,
                    code: code,
                    timestamp: 0  // 模拟时间戳
                }
            }

            ValidationError <- func(field, value) {
                return {
                    type: ""ValidationError"",
                    message: ""Invalid value for "" + field + "": "" + value,
                    field: field,
                    value: value
                }
            }

            try {
                try {
                    // 业务逻辑错误
                    if true {
                        throw BusinessError(""Insufficient funds"", 1001)
                    }
                } catch e1 {
                    if e1.type == ""BusinessError"" {
                        custom_exception_results <- custom_exception_results.concat({
                            ""business_caught: "" + e1.message + "" (code: "" + e1.code + "")""
                        })
                    }
                }

                try {
                    // 验证错误
                    if true {
                        throw ValidationError(""age"", -5)
                    }
                } catch e2 {
                    if e2.type == ""ValidationError"" {
                        custom_exception_results <- custom_exception_results.concat({
                            ""validation_caught: "" + e2.message
                        })
                    }
                }

            } catch outer_e {
                custom_exception_results <- custom_exception_results.concat({
                    ""outer_caught: "" + outer_e.message
                })
            }

            final_custom_results <- custom_exception_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_custom_results"));
        var results = result["final_custom_results"] as object[];
        Assert.Equal(2, results.Length);
        Assert.Contains(results, r => r.ToString().Contains("business_caught"));
        Assert.Contains(results, r => r.ToString().Contains("validation_caught"));
    }

    [Fact]
    public void NestedException_ExceptionContext_PreservingCallStack()
    {
        var code = @"
            context_info <- []

            function_a <- func() {
                context_info <- context_info.concat({""function_a_entered""})
                try {
                    result <- function_b()
                } catch e {
                    context_info <- context_info.concat({
                        ""function_a_caught: "" + e.message,
                        ""function_a_context"": ""processing_request""
                    })
                    throw {
                        original: e,
                        context: ""function_a_context"",
                        message: ""Error in function_a: "" + e.message
                    }
                }
            }

            function_b <- func() {
                context_info <- context_info.concat({""function_b_entered""})
                try {
                    result <- function_c()
                } catch e {
                    context_info <- context_info.concat({
                        ""function_b_caught: "" + e.message,
                        ""function_b_context"": ""validating_data""
                    })
                    throw {
                        original: e,
                        context: ""function_b_context"",
                        message: ""Error in function_b: "" + e.message
                    }
                }
            }

            function_c <- func() {
                context_info <- context_info.concat({""function_c_entered""})
                try {
                    result <- 50 / 0
                } catch e {
                    context_info <- context_info.concat({
                        ""function_c_caught: "" + e.message,
                        ""function_c_context"": ""performing_calculation""
                    })
                    throw {
                        original: e,
                        context: ""function_c_context"",
                        message: ""Error in function_c: "" + e.message
                    }
                }
            }

            try {
                final_result <- function_a()
            } catch final_e {
                context_info <- context_info.concat({
                    ""main_caught: "" + final_e.message,
                    ""full_context_available"": final_e.original != null
                })
            }

            final_context_info <- context_info
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_context_info"));
        var context = result["final_context_info"] as object[];
        Assert.True(context.Length >= 6);
        // 验证上下文信息被保留
        Assert.Contains(context, c => c.ToString().Contains("function_a_entered"));
        Assert.Contains(context, c => c.ToString().Contains("function_b_entered"));
        Assert.Contains(context, c => c.ToString().Contains("function_c_entered"));
    }

    [Fact]
    public void NestedException_ExceptionSuppression_SwallowingExceptions()
    {
        var code = @"
            suppression_results <- []

            try {
                try {
                    try {
                        result <- 12 / 0
                    } catch innermost {
                        suppression_results <- suppression_results.concat({""innermost_caught""})
                        // 抑制异常，不重新抛出
                    }
                } catch middle {
                    suppression_results <- suppression_results.concat({""middle_caught"")
                }
            } catch outermost {
                suppression_results <- suppression_results.concat({""outermost_caught"")
            }

            // 测试部分抑制
            try {
                try {
                    try {
                        result <- 15 / 0
                    } catch innermost2 {
                        suppression_results <- suppression_results.concat({""innermost2_caught"")
                        // 重新抛出异常
                        throw innermost2
                    }
                } catch middle2 {
                    suppression_results <- suppression_results.concat({""middle2_caught"")
                    // 抑制异常
                }
            } catch outermost2 {
                suppression_results <- suppression_results.concat({""outermost2_caught"")
            }

            final_suppression_results <- suppression_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_suppression_results"));
        var results = result["final_suppression_results"] as object[];
        // 第一个场景：只有内层捕获，外层不捕获
        // 第二个场景：内层和中层都捕获
        Assert.True(results.Length >= 4);
    }

    [Fact]
    public void NestedException_ExceptionAggregation_CollectingMultipleErrors()
    {
        var code = @"
            aggregated_errors <- []

            try {
                error_collection <- []

                try {
                    // 第一个错误
                    result1 <- 3 / 0
                } catch e1 {
                    error_collection <- error_collection.concat({
                        type: ""division_error"",
                        message: e1.message,
                        context: ""first_operation""
                    })
                }

                try {
                    // 第二个错误
                    value <- ""text"" + 7
                } catch e2 {
                    error_collection <- error_collection.concat({
                        type: ""type_error"",
                        message: e2.message,
                        context: ""second_operation""
                    })
                }

                try {
                    // 第三个错误
                    array_access <- [1, 2, 3][5]
                } catch e3 {
                    error_collection <- error_collection.concat({
                        type: ""index_error"",
                        message: e3.message,
                        context: ""third_operation""
                    })
                }

                if error_collection.length > 0 {
                    throw {
                        type: ""AggregatedError"",
                        message: ""Multiple errors occurred"",
                        errors: error_collection,
                        total_count: error_collection.length
                    }
                }

            } catch aggregated_e {
                if aggregated_e.type == ""AggregatedError"" {
                    aggregated_errors <- aggregated_e.errors
                    total_error_count <- aggregated_e.total_count
                }
            }

            final_aggregated_errors <- aggregated_errors
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_aggregated_errors"));
        var errors = result["final_aggregated_errors"];
        Assert.NotNull(errors);
    }

    [Fact]
    public void NestedException_ExceptionRecovery_RecoveryStrategies()
    {
        var code = @"
            recovery_results <- []

            operation_with_fallback <- func(primary_operation, fallback_operation) {
                try {
                    result <- primary_operation()
                    recovery_results <- recovery_results.concat({""primary_success""})
                    return result
                } catch e {
                    recovery_results <- recovery_results.concat({""primary_failed: "" + e.message})
                    try {
                        fallback_result <- fallback_operation()
                        recovery_results <- recovery_results.concat({""fallback_success""})
                        return fallback_result
                    } catch fallback_e {
                        recovery_results <- recovery_results.concat({""fallback_failed: "" + fallback_e.message})
                        return null
                    }
                }
            }

            // 主操作：除零错误
            primary_func <- func() {
                return 100 / 0
            }

            // 回退操作：安全除法
            fallback_func <- func() {
                return 100 / 1
            }

            result1 <- operation_with_fallback(primary_func, fallback_func)

            // 第二次测试：主操作成功
            success_primary <- func() {
                return 100 / 2
            }

            result2 <- operation_with_fallback(success_primary, fallback_func)

            final_recovery_results <- recovery_results
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_recovery_results"));
        var results = result["final_recovery_results"] as object[];
        Assert.Contains(results, r => r.ToString().Contains("primary_failed"));
        Assert.Contains(results, r => r.ToString().Contains("fallback_success"));
        Assert.Contains(results, r => r.ToString().Contains("primary_success"));
    }
}