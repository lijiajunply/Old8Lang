using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Old8Lang;

using Old8Lang.Interpreter;
using System.Collections.Generic;
namespace Old8Lang.Tests.Interpreter.Exceptions;

/// <summary>
/// 错误传播测试
/// 测试错误在不同作用域、函数调用链、模块间的传播机制
/// </summary>
[Trait("Category", "Interpreter")]
[Trait("Category", "Interpreter-Exceptions")]
[Trait("Category", "Interpreter-ErrorPropagation")]
public class ErrorPropagationTests
{
    private readonly ITestOutputHelper _output;

    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public ErrorPropagationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_FunctionCallChain_PropagatesThroughCallStack()
    {
        var code = @"
            propagation_log <- []

            function_c <- func() {
                propagation_log <- propagation_log.concat({""function_c_entered""})
                result <- 10 / 0
                return ""c_success""
            }

            function_b <- func() {
                propagation_log <- propagation_log.concat({""function_b_entered""})
                result <- function_c()
                return ""b_success: "" + result
            }

            function_a <- func() {
                propagation_log <- propagation_log.concat({""function_a_entered""})
                result <- function_b()
                return ""a_success: "" + result
            }

            try {
                final_result <- function_a()
            } catch e {
                propagation_log <- propagation_log.concat({
                    ""caught_at_top: "" + e.message,
                    ""propagation_completed"": true
                })
            }

            final_propagation_log <- propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_propagation_log"));
        var log = result["final_propagation_log"] as object[];
        Assert.True(log.Length >= 4);
        Assert.Contains(log, entry => entry.ToString().Contains("function_a_entered"));
        Assert.Contains(log, entry => entry.ToString().Contains("function_b_entered"));
        Assert.Contains(log, entry => entry.ToString().Contains("function_c_entered"));
        Assert.Contains(log, entry => entry.ToString().Contains("caught_at_top"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_ScopeBoundary_CrossesScopeLevels()
    {
        var code = @"
            scope_propagation_log <- []

            // 全局作用域
            global_var <- ""global""

            try {
                // 外层作用域
                outer_var <- ""outer""
                scope_propagation_log <- scope_propagation_log.concat({""outer_scope_entered""})

                try {
                    // 中层作用域
                    middle_var <- ""middle""
                    scope_propagation_log <- scope_propagation_log.concat({""middle_scope_entered""})

                    try {
                        // 内层作用域
                        inner_var <- ""inner""
                        scope_propagation_log <- scope_propagation_log.concat({""inner_scope_entered""})

                        // 触发错误
                        error_result <- 5 / 0
                    } catch inner_catch {
                        scope_propagation_log <- scope_propagation_log.concat({""inner_scope_caught""})
                        // 重新抛出以测试跨作用域传播
                        throw inner_catch
                    }

                } catch middle_catch {
                    scope_propagation_log <- scope_propagation_log.concat({""middle_scope_caught""})
                    throw middle_catch
                }

            } catch outer_catch {
                scope_propagation_log <- scope_propagation_log.concat({
                    ""outer_scope_caught: "" + outer_catch.message,
                    ""global_accessible"": global_var
                })
            }

            final_scope_log <- scope_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_scope_log"));
        var log = result["final_scope_log"] as object[];
        Assert.True(log.Length >= 4);
        Assert.Contains(log, entry => entry.ToString().Contains("outer_scope_caught"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_ConditionalPropagation_DependentOnErrorType()
    {
        var code = @"
            conditional_propagation_log <- []

            error_processor <- func(error_type) {
                try {
                    if error_type == ""division"" {
                        result <- 20 / 0
                    } else if error_type == ""type"" {
                        result <- ""hello"" + 5
                    } else if error_type == ""index"" {
                        result <- [1, 2, 3][10]
                    } else {
                        result <- ""no_error""
                    }
                    return ""success: "" + result
                } catch e {
                    conditional_propagation_log <- conditional_propagation_log.concat({
                        ""error_caught: "" + e.type,
                        ""message: "" + e.message
                    })

                    // 根据错误类型决定是否传播
                    if e.type == ""DivisionError"" {
                        throw e  // 传播除零错误
                    } else if e.type == ""TypeError"" {
                        return ""type_error_handled_locally""  // 本地处理类型错误
                    } else {
                        throw e  // 传播其他错误
                    }
                }
            }

            // 测试除零错误传播
            try {
                result1 <- error_processor(""division"")
            } catch e1 {
                conditional_propagation_log <- conditional_propagation_log.concat({
                    ""division_error_propagated: "" + e1.message
                })
            }

            // 测试类型错误本地处理
            result2 <- error_processor(""type"")

            // 测试索引错误传播
            try {
                result3 <- error_processor(""index"")
            } catch e3 {
                conditional_propagation_log <- conditional_propagation_log.concat({
                    ""index_error_propagated: "" + e3.message
                })
            }

            final_conditional_log <- conditional_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_conditional_log"));
        var log = result["final_conditional_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("division_error_propagated"));
        Assert.Contains(log, entry => entry.ToString().Contains("index_error_propagated"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_AsyncFunction_PropagatesThroughAsyncCalls()
    {
        var code = @"
            async_propagation_log <- []

            async_deep_function <- func() {
                async_propagation_log <- async_propagation_log.concat({""async_deep_entered""})
                // 模拟异步操作中的错误
                await async {
                    result <- 30 / 0
                }
                return ""async_deep_success""
            }

            async_middle_function <- func() {
                async_propagation_log <- async_propagation_log.concat({""async_middle_entered""})
                result <- await async_deep_function()
                return ""async_middle_success: "" + result
            }

            async_top_function <- func() {
                async_propagation_log <- async_propagation_log.concat({""async_top_entered""})
                result <- await async_middle_function()
                return ""async_top_success: "" + result
            }

            try {
                final_result <- await async_top_function()
            } catch async_e {
                async_propagation_log <- async_propagation_log.concat({
                    ""async_error_propagated: "" + async_e.message
                })
            }

            final_async_log <- async_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_async_log"));
        var log = result["final_async_log"] as object[];
        Assert.True(log.Length >= 3);
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_CallbackPattern_PropagatesThroughCallbacks()
    {
        var code = @"
            callback_propagation_log <- []

            data_processor <- func(data, success_callback, error_callback) {
                callback_propagation_log <- callback_propagation_log.concat({""processor_started""})

                try {
                    if data < 0 {
                        throw {type: ""InvalidDataError"", message: ""Negative data: "" + data}
                    } else if data == 0 {
                        result <- 10 / data
                    } else {
                        result <- data * 2
                    }

                    callback_propagation_log <- callback_propagation_log.concat({""processing_success""})
                    return success_callback(result)

                } catch e {
                    callback_propagation_log <- callback_propagation_log.concat({""processing_error""})
                    return error_callback(e)
                }
            }

            success_handler <- func(result) {
                callback_propagation_log <- callback_propagation_log.concat({""success_callback: "" + result})
                return ""handled_success""
            }

            error_handler <- func(error) {
                callback_propagation_log <- callback_propagation_log.concat({""error_callback: "" + error.message})
                // 重新抛出以测试回调中的错误传播
                throw error
            }

            // 测试成功情况
            result1 <- data_processor(5, success_handler, error_handler)

            // 测试错误传播
            try {
                result2 <- data_processor(0, success_handler, error_handler)
            } catch e2 {
                callback_propagation_log <- callback_propagation_log.concat({
                    ""callback_error_propagated: "" + e2.message
                })
            }

            final_callback_log <- callback_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_callback_log"));
        var log = result["final_callback_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("callback_error_propagated"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_EventDriven_PropagatesThroughEventHandlers()
    {
        var code = @"
            event_propagation_log <- []

            // 简单的事件系统
            event_emitter <- func() {
                return {
                    handlers: {},

                    on: func(self, event_name, handler) {
                        if self.handlers[event_name] == null {
                            self.handlers[event_name] <- {}
                        }
                        self.handlers[event_name] <- self.handlers[event_name].concat({handler})
                    },

                    emit: func(self, event_name, data) {
                        event_propagation_log <- event_propagation_log.concat({""emit_started: "" + event_name})

                        if self.handlers[event_name] != null {
                            i <- 0
                            while i < self.handlers[event_name].length {
                                handler <- self.handlers[event_name][i]
                                try {
                                    result <- handler(data)
                                    event_propagation_log <- event_propagation_log.concat({
                                        ""handler_success: "" + event_name
                                    })
                                } catch e {
                                    event_propagation_log <- event_propagation_log.concat({
                                        ""handler_error: "" + e.message
                                    })
                                    throw e  // 传播错误，停止后续处理
                                }
                                i <- i + 1
                            }
                        }
                        return ""emit_completed""
                    }
                }
            }

            emitter <- event_emitter()

            // 注册成功处理器
            emitter.on(emitter, ""data"", func(data) {
                event_propagation_log <- event_propagation_log.concat({""processing_data: "" + data})
                return data * 2
            })

            // 注册错误处理器
            emitter.on(emitter, ""data"", func(data) {
                event_propagation_log <- event_propagation_log.concat({""about_to_error: "" + data})
                result <- 50 / 0  // 触发错误
                return ""should_not_reach""
            })

            // 注册另一个处理器（不应该被执行）
            emitter.on(emitter, ""data"", func(data) {
                event_propagation_log <- event_propagation_log.concat({""should_not_execute: "" + data})
                return ""not_executed""
            })

            try {
                emit_result <- emitter.emit(emitter, ""data"", 10)
            } catch emit_error {
                event_propagation_log <- event_propagation_log.concat({
                    ""emit_error_propagated: "" + emit_error.message
                })
            }

            final_event_log <- event_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_event_log"));
        var log = result["final_event_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("handler_error"));
        Assert.Contains(log, entry => entry.ToString().Contains("emit_error_propagated"));
        // 确保后续处理器没有执行
        Assert.DoesNotContain(log, entry => entry.ToString().Contains("should_not_execute"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_ModuleBoundary_CrossesModuleBoundaries()
    {
        var code = @"
            module_propagation_log <- []

            // 模拟模块A
            module_a <- func() {
                return {
                    name: ""module_a"",
                    process_data: func(self, data) {
                        module_propagation_log <- module_propagation_log.concat({""module_a_processing""})
                        try {
                            if data == ""error"" {
                                throw {
                                    type: ""ModuleAError"",
                                    message: ""Module A processing failed"",
                                    module: ""module_a""
                                }
                            }
                            return ""module_a_processed: "" + data
                        } catch e {
                            module_propagation_log <- module_propagation_log.concat({""module_a_caught: "" + e.message})
                            throw e  // 传播到模块外部
                        }
                    }
                }
            }

            // 模拟模块B（使用模块A）
            module_b <- func() {
                return {
                    name: ""module_b"",
                    module_a_instance: module_a(),

                    handle_request: func(self, request) {
                        module_propagation_log <- module_propagation_log.concat({""module_b_handling""})
                        try {
                            result <- self.module_a_instance.process_data(self.module_a_instance, request.data)
                            return {
                                status: ""success"",
                                result: result,
                                processed_by: [self.name, self.module_a_instance.name]
                            }
                        } catch e {
                            module_propagation_log <- module_propagation_log.concat({""module_b_caught: "" + e.message})
                            // 添加模块B的上下文
                            throw {
                                original_error: e,
                                module: self.name,
                                context: ""request_handling"",
                                message: ""Module B failed to handle request: "" + e.message
                            }
                        }
                    }
                }
            }

            // 主应用程序
            try {
                app_module <- module_b()
                success_result <- app_module.handle_request(app_module, {data: ""test""})

                error_result <- app_module.handle_request(app_module, {data: ""error""})
            } catch app_error {
                module_propagation_log <- module_propagation_log.concat({
                    ""app_caught: "" + app_error.message,
                    ""original_module"": app_error.original_error.module,
                    ""propagated_from"": app_error.module
                })
            }

            final_module_log <- module_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_module_log"));
        var log = result["final_module_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("app_caught"));
        Assert.Contains(log, entry => entry.ToString().Contains("module_a_caught"));
        Assert.Contains(log, entry => entry.ToString().Contains("module_b_caught"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_PromiseChain_PropagatesThroughPromiseChain()
    {
        var code = @"
            promise_propagation_log <- []

            // 简化的Promise实现
            promise <- func(executor) {
                return {
                    then_callbacks: {},
                    catch_callbacks: {},
                    state: ""pending"",
                    value: null,

                    then: func(self, on_fulfilled, on_rejected) {
                        self.then_callbacks <- self.then_callbacks.concat({on_fulfilled})
                        if on_rejected != null {
                            self.catch_callbacks <- self.catch_callbacks.concat({on_rejected})
                        }
                        return self
                    },

                    catch: func(self, on_rejected) {
                        self.catch_callbacks <- self.catch_callbacks.concat({on_rejected})
                        return self
                    },

                    resolve: func(self, value) {
                        self.state <- ""fulfilled""
                        self.value <- value
                        i <- 0
                        while i < self.then_callbacks.length {
                            callback <- self.then_callbacks[i]
                            try {
                                result <- callback(value)
                                promise_propagation_log <- promise_propagation_log.concat({
                                    ""then_callback_success""
                                })
                            } catch e {
                                promise_propagation_log <- promise_propagation_log.concat({
                                    ""then_callback_error: "" + e.message
                                })
                                self.state <- ""rejected""
                                self.value <- e
                            }
                            i <- i + 1
                        }
                    },

                    reject: func(self, error) {
                        self.state <- ""rejected""
                        self.value <- error
                        i <- 0
                        while i < self.catch_callbacks.length {
                            callback <- self.catch_callbacks[i]
                            try {
                                result <- callback(error)
                                promise_propagation_log <- promise_propagation_log.concat({
                                    ""catch_callback_success""
                                })
                            } catch e {
                                promise_propagation_log <- promise_propagation_log.concat({
                                    ""catch_callback_error: "" + e.message
                                })
                            }
                            i <- i + 1
                        }
                    }
                }
            }

            // 创建成功的Promise链
            success_promise <- promise(func(resolve, reject) {
                promise_propagation_log <- promise_propagation_log.concat({""promise_started""})
                resolve(42)
            })

            success_promise.then(success_promise, func(value) {
                promise_propagation_log <- promise_propagation_log.concat({""first_then: "" + value})
                return value * 2
            }).then(success_promise, func(value) {
                promise_propagation_log <- promise_propagation_log.concat({""second_then: "" + value})
                return value + 10
            })

            success_promise.resolve(success_promise, 42)

            // 创建失败的Promise链
            failed_promise <- promise(func(resolve, reject) {
                promise_propagation_log <- promise_propagation_log.concat({""failed_promise_started""})
                throw {type: ""PromiseError"", message: ""Promise execution failed""}
            })

            failed_promise.then(failed_promise, func(value) {
                promise_propagation_log <- promise_propagation_log.concat({""should_not_execute_then""})
                return value
            }).catch(failed_promise, func(error) {
                promise_propagation_log <- promise_propagation_log.concat({""promise_caught: "" + error.message})
                throw error  // 重新抛出以测试传播
            })

            failed_promise.reject(failed_promise, {type: ""PromiseError"", message: ""Promise execution failed""})

            final_promise_log <- promise_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_promise_log"));
        var log = result["final_promise_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("first_then"));
        Assert.Contains(log, entry => entry.ToString().Contains("second_then"));
        Assert.Contains(log, entry => entry.ToString().Contains("promise_caught"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_MiddlewarePattern_PropagatesThroughMiddleware()
    {
        var code = @"
            middleware_propagation_log <- []

            // 简化的中间件系统
            middleware_chain <- func() {
                return {
                    middlewares: {},

                    use: func(self, middleware) {
                        self.middlewares <- self.middlewares.concat({middleware})
                    },

                    process: func(self, request) {
                        middleware_propagation_log <- middleware_propagation_log.concat({""chain_processing_started""})

                        i <- 0
                        while i < self.middlewares.length {
                            middleware <- self.middlewares[i]
                            try {
                                middleware_propagation_log <- middleware_propagation_log.concat({
                                    ""middleware_"" + i + ""_started""
                                })
                                result <- middleware(request)
                                middleware_propagation_log <- middleware_propagation_log.concat({
                                    ""middleware_"" + i + ""_success""
                                })
                                request <- result  // 传递给下一个中间件
                            } catch e {
                                middleware_propagation_log <- middleware_propagation_log.concat({
                                    ""middleware_"" + i + ""_error: "" + e.message
                                })
                                throw {
                                    original_error: e,
                                    middleware_index: i,
                                    message: ""Middleware chain failed at index "" + i
                                }
                            }
                            i <- i + 1
                        }
                        return request
                    }
                }
            }

            chain <- middleware_chain()

            // 添加中间件
            chain.use(chain, func(req) {
                middleware_propagation_log <- middleware_propagation_log.concat({""auth_middleware""})
                if req.authenticated == false {
                    throw {type: ""AuthenticationError"", message: ""Unauthorized access""}
                }
                req.authenticated <- true
                return req
            })

            chain.use(chain, func(req) {
                middleware_propagation_log <- middleware_propagation_log.concat({""validation_middleware""})
                if req.data == null {
                    throw {type: ""ValidationError"", message: ""Missing required data""}
                }
                return req
            })

            chain.use(chain, func(req) {
                middleware_propagation_log <- middleware_propagation_log.concat({""processing_middleware""})
                if req.processing_error == true {
                    result <- 10 / 0  // 触发处理错误
                }
                req.processed <- true
                return req
            })

            // 测试成功请求
            success_request <- {
                authenticated: true,
                data: ""test_data"",
                processing_error: false
            }

            try {
                success_result <- chain.process(chain, success_request)
            } catch e {
                middleware_propagation_log <- middleware_propagation_log.concat({
                    ""unexpected_success_error: "" + e.message
                })
            }

            // 测试认证错误
            auth_request <- {
                authenticated: false,
                data: ""test_data"",
                processing_error: false
            }

            try {
                auth_result <- chain.process(chain, auth_request)
            } catch auth_e {
                middleware_propagation_log <- middleware_propagation_log.concat({
                    ""auth_error_propagated: "" + auth_e.original_error.message
                })
            }

            // 测试处理错误
            processing_request <- {
                authenticated: true,
                data: ""test_data"",
                processing_error: true
            }

            try {
                processing_result <- chain.process(chain, processing_request)
            } catch processing_e {
                middleware_propagation_log <- middleware_propagation_log.concat({
                    ""processing_error_propagated: "" + processing_e.original_error.message
                })
            }

            final_middleware_log <- middleware_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_middleware_log"));
        var log = result["final_middleware_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("auth_error_propagated"));
        Assert.Contains(log, entry => entry.ToString().Contains("processing_error_propagated"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_PipelinePattern_PropagatesThroughPipeline()
    {
        var code = @"
            pipeline_propagation_log <- []

            // 管道处理系统
            pipeline <- func(steps) {
                return {
                    steps: steps,

                    execute: func(self, input) {
                        pipeline_propagation_log <- pipeline_propagation_log.concat({""pipeline_started""})

                        current_input <- input
                        i <- 0
                        while i < self.steps.length {
                            step <- self.steps[i]
                            try {
                                pipeline_propagation_log <- pipeline_propagation_log.concat({
                                    ""step_"" + i + ""_processing""
                                })
                                current_input <- step(current_input)
                                pipeline_propagation_log <- pipeline_propagation_log.concat({
                                    ""step_"" + i + ""_completed""
                                })
                            } catch e {
                                pipeline_propagation_log <- pipeline_propagation_log.concat({
                                    ""step_"" + i + ""_failed: "" + e.message
                                })
                                throw {
                                    pipeline_error: true,
                                    step_index: i,
                                    step_name: ""step_"" + i,
                                    original_error: e,
                                    input_at_failure: current_input
                                }
                            }
                            i <- i + 1
                        }
                        return current_input
                    }
                }
            }

            // 定义管道步骤
            step1 <- func(data) {
                pipeline_propagation_log <- pipeline_propagation_log.concat({""step1_execution""})
                if data.type == ""error"" {
                    throw {type: ""Step1Error"", message: ""Error in step 1""}
                }
                return {value: data.value + 10, step: 1}
            }

            step2 <- func(data) {
                pipeline_propagation_log <- pipeline_propagation_log.concat({""step2_execution""})
                if data.step == 1 && data.value > 15 {
                    throw {type: ""Step2Error"", message: ""Value too large in step 2""}
                }
                return {value: data.value * 2, step: 2}
            }

            step3 <- func(data) {
                pipeline_propagation_log <- pipeline_propagation_log.concat({""step3_execution""})
                if data.step == 2 && data.value == 40 {
                    throw {type: ""Step3Error"", message: ""Invalid value in step 3""}
                }
                return {value: data.value - 5, step: 3, final: true}
            }

            // 创建管道
            data_pipeline <- pipeline({step1, step2, step3})

            // 测试成功执行
            try {
                success_result <- data_pipeline.execute(data_pipeline, {type: ""normal"", value: 5})
            } catch success_e {
                pipeline_propagation_log <- pipeline_propagation_log.concat({
                    ""unexpected_success_failure: "" + success_e.message
                })
            }

            // 测试step1错误
            try {
                step1_result <- data_pipeline.execute(data_pipeline, {type: ""error"", value: 0})
            } catch step1_e {
                pipeline_propagation_log <- pipeline_propagation_log.concat({
                    ""step1_error_propagated: step_"" + step1_e.step_index
                })
            }

            // 测试step2错误
            try {
                step2_result <- data_pipeline.execute(data_pipeline, {type: ""normal"", value: 10})
            } catch step2_e {
                pipeline_propagation_log <- pipeline_propagation_log.concat({
                    ""step2_error_propagated: step_"" + step2_e.step_index
                })
            }

            // 测试step3错误
            try {
                step3_result <- data_pipeline.execute(data_pipeline, {type: ""normal"", value: 20})
            } catch step3_e {
                pipeline_propagation_log <- pipeline_propagation_log.concat({
                    ""step3_error_propagated: step_"" + step3_e.step_index
                })
            }

            final_pipeline_log <- pipeline_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_pipeline_log"));
        var log = result["final_pipeline_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("step1_error_propagated"));
        Assert.Contains(log, entry => entry.ToString().Contains("step2_error_propagated"));
        Assert.Contains(log, entry => entry.ToString().Contains("step3_error_propagated"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_CascadeEffect_ErrorCascading()
    {
        var code = @"
            cascade_propagation_log <- []

            // 级联系统
            cascade_system <- func() {
                return {
                    components: {},

                    add_component: func(self, name, component) {
                        self.components[name] <- component
                    },

                    trigger_cascade: func(self, event_name, data) {
                        cascade_propagation_log <- cascade_propagation_log.concat({
                            ""cascade_started: "" + event_name
                        })

                        // 按依赖顺序触发组件
                        component_names <- [""database"", ""cache"", ""service"", ""api""]
                        i <- 0
                        while i < component_names.length {
                            comp_name <- component_names[i]
                            if self.components[comp_name] != null {
                                try {
                                    cascade_propagation_log <- cascade_propagation_log.concat({
                                        comp_name + ""_processing""
                                    })
                                    result <- self.components[comp_name](event_name, data)
                                    cascade_propagation_log <- cascade_propagation_log.concat({
                                        comp_name + ""_success""
                                    })
                                } catch e {
                                    cascade_propagation_log <- cascade_propagation_log.concat({
                                        comp_name + ""_failed: "" + e.message
                                    })
                                    throw {
                                        cascade_error: true,
                                        failed_component: comp_name,
                                        original_error: e,
                                        cascade_depth: i
                                    }
                                }
                            }
                            i <- i + 1
                        }
                        return ""cascade_completed""
                    }
                }
            }

            system <- cascade_system()

            // 添加组件
            system.add_component(system, ""database"", func(event, data) {
                if event == ""save"" && data.id == null {
                    throw {type: ""DatabaseError"", message: ""Cannot save without ID""}
                }
                return {saved: true, id: data.id}
            })

            system.add_component(system, ""cache"", func(event, data) {
                if event == ""save"" && data.cacheable == false {
                    throw {type: ""CacheError"", message: ""Data not cacheable""}
                }
                return {cached: true}
            })

            system.add_component(system, ""service"", func(event, data) {
                if event == ""save"" && data.service_error == true {
                    throw {type: ""ServiceError"", message: ""External service unavailable""}
                }
                return {processed: true}
            })

            system.add_component(system, ""api"", func(event, data) {
                return {response: ""OK"", status: 200}
            })

            // 测试正常级联
            try {
                normal_result <- system.trigger_cascade(system, ""save"", {
                    id: 123,
                    cacheable: true,
                    service_error: false
                })
            } catch normal_e {
                cascade_propagation_log <- cascade_propagation_log.concat({
                    ""unexpected_normal_failure: "" + normal_e.message
                })
            }

            // 测试数据库错误级联
            try {
                db_error_result <- system.trigger_cascade(system, ""save"", {
                    cacheable: true,
                    service_error: false
                })
            } catch db_e {
                cascade_propagation_log <- cascade_propagation_log.concat({
                    ""database_error_cascade: "" + db_e.failed_component
                })
            }

            // 测试缓存错误级联
            try {
                cache_error_result <- system.trigger_cascade(system, ""save"", {
                    id: 456,
                    cacheable: false,
                    service_error: false
                })
            } catch cache_e {
                cascade_propagation_log <- cascade_propagation_log.concat({
                    ""cache_error_cascade: "" + cache_e.failed_component
                })
            }

            final_cascade_log <- cascade_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_cascade_log"));
        var log = result["final_cascade_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("database_error_cascade"));
        Assert.Contains(log, entry => entry.ToString().Contains("cache_error_cascade"));
    }

    [Fact]
    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    public void ErrorPropagation_ErrorAggregation_CollectsMultipleErrors()
    {
        var code = @"
            aggregation_propagation_log <- []

            error_aggregator <- func() {
                return {
                    errors: {},

                    add_error: func(self, error_context, error) {
                        self.errors[error_context] <- error
                        aggregation_propagation_log <- aggregation_propagation_log.concat({
                            ""error_added: "" + error_context
                        })
                    },

                    has_errors: func(self) {
                        count <- 0
                        for key, value in self.errors {
                            count <- count + 1
                        }
                        return count > 0
                    },

                    get_error_count: func(self) {
                        count <- 0
                        for key, value in self.errors {
                            count <- count + 1
                        }
                        return count
                    },

                    get_aggregated_error: func(self) {
                        if self.has_errors(self) {
                            return {
                                type: ""AggregatedError"",
                                message: ""Multiple errors occurred"",
                                error_count: self.get_error_count(self),
                                errors: self.errors
                            }
                        } else {
                            return null
                        }
                    }
                }
            }

            multi_operation_processor <- func() {
                aggregator <- error_aggregator()

                try {
                    // 操作1
                    try {
                        result1 <- 25 / 0
                        aggregation_propagation_log <- aggregation_propagation_log.concat({""operation1_success""})
                    } catch e1 {
                        aggregator.add_error(aggregator, ""operation1"", e1)
                    }

                    // 操作2
                    try {
                        result2 <- ""hello"" + 15
                        aggregation_propagation_log <- aggregation_propagation_log.concat({""operation2_success""})
                    } catch e2 {
                        aggregator.add_error(aggregator, ""operation2"", e2)
                    }

                    // 操作3
                    try {
                        result3 <- [1, 2, 3][10]
                        aggregation_propagation_log <- aggregation_propagation_log.concat({""operation3_success""})
                    } catch e3 {
                        aggregator.add_error(aggregator, ""operation3"", e3)
                    }

                    // 操作4（成功）
                    try {
                        result4 <- 100 / 5
                        aggregation_propagation_log <- aggregation_propagation_log.concat({""operation4_success""})
                    } catch e4 {
                        aggregator.add_error(aggregator, ""operation4"", e4)
                    }

                    // 检查是否有错误需要传播
                    if aggregator.has_errors(aggregator) {
                        aggregated_error <- aggregator.get_aggregated_error(aggregator)
                        aggregation_propagation_log <- aggregation_propagation_log.concat({
                            ""propagating_aggregated_error: "" + aggregated_error.error_count + "" errors""
                        })
                        throw aggregated_error
                    } else {
                        aggregation_propagation_log <- aggregation_propagation_log.concat({""all_operations_successful""})
                    }

                    return {success: true, results: {result1, result2, result3, result4}}

                } catch outer_e {
                    aggregation_propagation_log <- aggregation_propagation_log.concat({
                        ""aggregated_error_propagated: "" + outer_e.error_count
                    })
                    throw outer_e
                }
            }

            try {
                final_result <- multi_operation_processor()
            } catch final_e {
                aggregation_propagation_log <- aggregation_propagation_log.concat({
                    ""final_aggregated_handling: "" + final_e.error_count
                })
            }

            final_aggregation_log <- aggregation_propagation_log
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("final_aggregation_log"));
        var log = result["final_aggregation_log"] as object[];
        Assert.Contains(log, entry => entry.ToString().Contains("error_added"));
        Assert.Contains(log, entry => entry.ToString().Contains("propagating_aggregated_error"));
        Assert.Contains(log, entry => entry.ToString().Contains("aggregated_error_propagated"));
    }
}