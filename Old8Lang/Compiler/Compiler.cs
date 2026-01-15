using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.Compiler;

/// <summary>
/// Old8Lang编译器，负责将抽象语法树(AST)转换为IL代码并执行
/// </summary>
/// <remarks>
/// 该类是Old8Lang编译器的核心组件，主要负责：
/// - 将AST编译为动态方法
/// - 生成IL代码
/// - 验证IL代码的正确性
/// - 处理编译错误和日志输出
/// - 支持不同级别的日志记录
/// </remarks>
public static class Compiler
{
    /// <summary>
    /// 调试输出开关，默认为关闭
    /// </summary>
    public static bool DebugOutputEnabled { get; set; }

    /// <summary>
    /// IL代码验证开关，默认为开启
    /// </summary>
    public static bool IlVerificationEnabled { get; set; } = true;

    /// <summary>
    /// 日志级别枚举，用于控制编译过程中的日志输出
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 仅输出错误信息
        /// </summary>
        Error,

        /// <summary>
        /// 输出错误和警告信息
        /// </summary>
        Warning,

        /// <summary>
        /// 输出错误、警告和信息
        /// </summary>
        Info,

        /// <summary>
        /// 输出所有信息，包括调试信息
        /// </summary>
        Debug
    }

    /// <summary>
    /// 当前日志级别，默认为 Info
    /// </summary>
    public static LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// 获取或设置类型推断配置
    /// </summary>
    public static TypeInferenceConfig TypeInferenceConfig => TypeInferenceConfig.Instance;

    /// <summary>
    /// 启用或禁用渐进式类型推断（默认：true）
    /// </summary>
    public static bool EnableTypeInference
    {
        get => TypeInferenceConfig.EnableTypeInference;
        set => TypeInferenceConfig.EnableTypeInference = value;
    }

    /// <summary>
    /// 启用或禁用类型推断调试输出（默认：禁用）
    /// </summary>
    public static bool TypeInferenceDebugOutput
    {
        get => TypeInferenceConfig.DebugOutput;
        set => TypeInferenceConfig.DebugOutput = value;
    }

    /// <summary>
    /// 条件输出调试信息
    /// </summary>
    /// <param name="message">调试信息</param>
    /// <param name="level">日志级别</param>
    private static void Log(string message, LogLevel level = LogLevel.Info)
    {
        if (!DebugOutputEnabled && level > LogLevel.Info)
            return;

        if (level > CurrentLogLevel)
            return;

        string levelPrefix = level switch
        {
            LogLevel.Error => "[编译错误]",
            LogLevel.Warning => "[编译警告]",
            LogLevel.Info => "[编译信息]",
            LogLevel.Debug => "[编译调试]",
            _ => "[编译日志]"
        };

        if (level == LogLevel.Error)
            Console.Error.WriteLine($"{levelPrefix} {message}");
        else
            Console.WriteLine($"{levelPrefix} {message}");
    }

    /// <summary>
    /// 条件输出格式化调试信息
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="level">日志级别</param>
    /// <param name="args">格式化参数</param>
    private static void LogFormat(string format, LogLevel level = LogLevel.Info, params object[] args)
    {
        if (!DebugOutputEnabled && level > LogLevel.Info)
            return;

        if (level > CurrentLogLevel)
            return;

        string levelPrefix = level switch
        {
            LogLevel.Error => "[编译错误]",
            LogLevel.Warning => "[编译警告]",
            LogLevel.Info => "[编译信息]",
            LogLevel.Debug => "[编译调试]",
            _ => "[编译日志]"
        };

        string message = string.Format(format, args);

        if (level == LogLevel.Error)
            Console.Error.WriteLine($"{levelPrefix} {message}");
        else
            Console.WriteLine($"{levelPrefix} {message}");
    }

    /// <summary>
    /// 检测语句块是否包含 await 表达式
    /// </summary>
    /// <param name="statement">要检测的语句</param>
    /// <returns>如果包含 await 表达式则返回 true，否则返回 false</returns>
    private static bool ContainsAwait(OldStatement statement)
    {
        // 使用 AsyncStateMachineGenerator 的识别逻辑
        // 创建一个临时的生成器来识别 await
        if (statement is not BlockStatement block)
        {
            // 如果不是块语句，包装成块语句
            block = new BlockStatement([statement]);
        }

        try
        {
            // 创建临时的 IL 生成器和 LocalManager
            var tempMethod = new DynamicMethod("TempAwaitCheck", null, null, true);
            var tempIl = tempMethod.GetILGenerator();
            var tempLocal = new LocalManager();

            // 创建 AsyncStateMachineGenerator 并检查是否识别到 await
            var generator = new Generators.AsyncStateMachineGenerator(tempIl, tempLocal, block);

            // 通过反射访问 AwaitInfos 字段
            var awaitInfosField = typeof(Generators.AsyncStateMachineGenerator)
                .GetField("AwaitInfos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (awaitInfosField != null)
            {
                var awaitInfos = awaitInfosField.GetValue(generator) as System.Collections.IList;
                return awaitInfos != null && awaitInfos.Count > 0;
            }

            return false;
        }
        catch
        {
            // 如果检测失败，假设不包含 await
            return false;
        }
    }

    /// <summary>
    /// 注册全局静态类到LocalManager中
    /// </summary>
    /// <param name="local">局部变量管理器</param>
    /// <remarks>
    /// 该方法将所有全局静态类实例注册到LocalManager的GlobalStaticClasses字典中，
    /// 使得在编译模式下可以像解释模式一样访问这些静态类
    /// </remarks>
    private static void RegisterGlobalStaticClasses(LocalManager local)
    {
        // 注册异步任务相关静态类
        local.GlobalStaticClasses["Task"] = TaskClassLangValue.GetInstance();
        local.GlobalStaticClasses["Thread"] = ThreadClassLangValue.GetInstance();
        local.GlobalStaticClasses["TaskScheduler"] = TaskSchedulerClassLangValue.GetInstance();
        local.GlobalStaticClasses["CancellationTokenSource"] = CancellationTokenSourceConstructor.GetInstance();
        local.GlobalStaticClasses["TaskCompletionSource"] = TaskCompletionSourceConstructor.GetInstance();

        // 注册测试相关静态类
        local.GlobalStaticClasses["Assert"] = AssertClassLangValue.GetInstance();
        local.GlobalStaticClasses["TestRunner"] = TestRunnerClassLangValue.GetInstance();
        local.GlobalStaticClasses["Mock"] = MockLibClassLangValue.GetInstance();

        Log("全局静态类注册完成", LogLevel.Debug);
    }

    /// <summary>
    /// 将抽象语法树编译为可执行的委托
    /// </summary>
    /// <param name="statement">表示整个程序的块语句</param>
    /// <param name="path">源代码文件路径</param>
    /// <param name="i">关联的解释器实例</param>
    /// <returns>编译后的可执行委托</returns>
    /// <exception cref="CompilerException">当编译或IL验证失败时抛出</exception>
    /// <remarks>
    /// 该方法执行以下步骤：
    /// 1. 创建动态方法和IL生成器
    /// 2. 生成IL代码
    /// 3. 验证IL代码（如果启用）
    /// 4. 创建并返回可执行委托
    /// </remarks>
    public static Action Compile(BlockStatement statement, string path, LangInterpreter i)
    {
        LogFormat("开始编译: {0}", LogLevel.Debug, path);
        LogFormat("语句类型: {0}", LogLevel.Debug, statement.GetType().Name);

        // 检测顶层代码是否包含 await 表达式
        bool containsAwait = ContainsAwait(statement);
        if (containsAwait)
        {
            Log("检测到顶层 await 表达式，使用异步编译模式", LogLevel.Debug);
            return CompileWithTopLevelAwait(statement, path, i);
        }

        // 创建动态方法，用于生成和执行IL代码
        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path, Interpreter = i };

        // 注册全局静态类
        RegisterGlobalStaticClasses(local);

        try
        {
            Log("开始生成IL代码", LogLevel.Debug);
            // 调用块语句的GenerateIl方法生成IL代码
            statement.GenerateIl(ilGenerator, local);
            Log("IL代码生成完成", LogLevel.Debug);

            // 生成返回指令
            ilGenerator.Emit(OpCodes.Ret);

            // 执行IL代码验证（如果启用）
            if (IlVerificationEnabled)
            {
                Log("开始验证IL代码", LogLevel.Debug);
                var verificationResult = IlVerifier.Verify(dynamicMethod, "OldLangRun");

                if (!verificationResult.IsValid)
                {
                    // IL验证失败，输出详细错误信息
                    foreach (var error in verificationResult.Errors)
                    {
                        if (error is { Code: not null, Message: not null })
                            LogFormat("IL验证错误 [{0}]: {1}", LogLevel.Error, error.Code, error.Message);
                        if (!string.IsNullOrEmpty(error.Context))
                        {
                            LogFormat("上下文: {0}", LogLevel.Error, error.Context);
                        }

                        if (!string.IsNullOrEmpty(error.StackTrace))
                        {
                            LogFormat("堆栈跟踪: {0}", LogLevel.Debug, error.StackTrace);
                        }
                    }

                    throw new CompilerException($"IL代码验证失败，共发现 {verificationResult.Errors.Count} 个错误",
                        new SourcePosition(0, 0));
                }

                Log("IL代码验证通过", LogLevel.Debug);
            }
            else
            {
                Log("IL代码验证已禁用", LogLevel.Debug);
            }

            Log("创建委托", LogLevel.Debug);
            Action oldLangRun;
            try
            {
                // 创建可执行委托
                oldLangRun = (Action)dynamicMethod.CreateDelegate(typeof(Action));
                Log("编译成功");
            }
            catch (InvalidProgramException ex)
            {
                LogFormat("创建委托失败: {0}", LogLevel.Error, ex.Message);
                throw new CompilerException($"创建委托失败: {ex.Message}", new SourcePosition(0, 0), ex);
            }

            // 包装委托以捕获并转换运行时异常
            return () =>
            {
                try
                {
                    oldLangRun();
                }
                catch (DivideByZeroException)
                {
                    throw new Old8Exception(
                        "RUNTIME_ERROR",
                        "除数不能为零",
                        new SourcePosition(0, 0));
                }
                catch (Old8Exception)
                {
                    // 已经是Old8Exception，直接重新抛出
                    throw;
                }
                catch (Exception ex)
                {
                    // 将其他.NET异常包装为Old8Exception
                    throw new Old8Exception(
                        "RUNTIME_ERROR",
                        ex.Message,
                        new SourcePosition(0, 0),
                        innerException: ex);
                }
            };
        }
        catch (Exception ex)
        {
            // 处理编译过程中的异常，输出详细错误信息
            LogFormat("\n{0}", LogLevel.Error, path);
            LogFormat("错误类型: {0}", LogLevel.Error, ex.GetType().Name);
            LogFormat("错误信息: {0}", LogLevel.Error, ex.Message);

            // 仅在调试模式下输出完整堆栈跟踪
            if (CurrentLogLevel >= LogLevel.Debug)
            {
                if (ex.StackTrace is not null)
                {
                    LogFormat("堆栈跟踪: {0}", LogLevel.Error, ex.StackTrace);
                }
            }

            // 尝试获取更详细的位置信息
            if (ex is InvalidOperationError invalidOpError)
            {
                LogFormat("位置信息: {0}", LogLevel.Error, invalidOpError.Position);
                if (invalidOpError.SourceContext is { Length: > 0 })
                {
                    Log("上下文", LogLevel.Error);
                    foreach (var line in invalidOpError.SourceContext)
                    {
                        LogFormat("  {0}", LogLevel.Error, line);
                    }
                }

                if (invalidOpError.Suggestion is not null)
                {
                    LogFormat("建议: {0}", LogLevel.Error, invalidOpError.Suggestion);
                }
            }
            else if (ex is CompilerException compilerEx)
            {
                LogFormat("位置信息: {0}", LogLevel.Error, compilerEx.Position);
            }

            throw;
        }
    }

    /// <summary>
    /// 编译包含顶层 await 的代码
    /// </summary>
    /// <param name="statement">表示整个程序的块语句</param>
    /// <param name="path">源代码文件路径</param>
    /// <param name="i">关联的解释器实例</param>
    /// <returns>编译后的可执行委托</returns>
    /// <remarks>
    /// 该方法将顶层代码包装成异步方法，然后同步等待其完成
    /// </remarks>
    private static Action CompileWithTopLevelAwait(BlockStatement statement, string path, LangInterpreter i)
    {
        Log("编译包含顶层 await 的代码", LogLevel.Debug);

        // 创建一个返回 Task 的动态方法
        var asyncMethod = new DynamicMethod("OldLangRunAsync", typeof(Task), null, true);
        var ilGenerator = asyncMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path, Interpreter = i };

        // 注册全局静态类
        RegisterGlobalStaticClasses(local);

        try
        {
            Log("开始生成异步 IL 代码", LogLevel.Debug);

            // 生成语句的 IL 代码
            statement.GenerateIl(ilGenerator, local);

            // 清空栈（如果有值的话）
            // 注意：BlockStatement.GenerateIl 通常不会在栈上留下值
            // 但为了安全起见，我们不做任何假设

            // 返回一个已完成的 Task
            // 注意：这是简化实现，实际的 await 会在语句内部处理
            ilGenerator.Emit(OpCodes.Call, typeof(Task).GetMethod("get_CompletedTask")!);
            ilGenerator.Emit(OpCodes.Ret);

            Log("异步 IL 代码生成完成", LogLevel.Debug);

            // 创建异步委托
            var asyncDelegate = (Func<Task>)asyncMethod.CreateDelegate(typeof(Func<Task>));
            Log("异步编译成功", LogLevel.Info);

            // 返回一个 Action，该 Action 会同步等待异步方法完成
            return () =>
            {
                try
                {
                    // 使用 GetAwaiter().GetResult() 同步等待
                    // 这会保留异常堆栈信息
                    asyncDelegate().GetAwaiter().GetResult();
                }
                catch (Old8Exception)
                {
                    // 已经是Old8Exception，直接重新抛出
                    throw;
                }
                catch (Exception ex)
                {
                    // 将其他.NET异常包装为Old8Exception
                    throw new Old8Exception(
                        "RUNTIME_ERROR",
                        ex.Message,
                        new SourcePosition(0, 0),
                        innerException: ex);
                }
            };
        }
        catch (Exception ex)
        {
            // 处理编译过程中的异常
            LogFormat("\n{0}", LogLevel.Error, path);
            LogFormat("错误类型: {0}", LogLevel.Error, ex.GetType().Name);
            LogFormat("错误信息: {0}", LogLevel.Error, ex.Message);

            if (CurrentLogLevel >= LogLevel.Debug && ex.StackTrace is not null)
            {
                LogFormat("堆栈跟踪: {0}", LogLevel.Error, ex.StackTrace);
            }

            throw;
        }
    }

    /// <summary>
    /// 从文件路径编译Old8Lang代码
    /// </summary>
    /// <param name="path">Old8Lang源代码文件路径</param>
    /// <param name="i">关联的解释器实例</param>
    /// <returns>编译后的可执行委托</returns>
    /// <exception cref="CompilerException">当编译失败时抛出</exception>
    /// <remarks>
    /// 该方法执行以下步骤：
    /// 1. 从文件读取源代码
    /// 2. 解析源代码生成抽象语法树
    /// 3. 调用另一个Compile重载方法编译AST
    /// </remarks>
    public static Action Compile(string path, LangInterpreter i)
    {
        LogFormat("开始编译文件: {0}", LogLevel.Debug, path);

        try
        {
            Log("解析代码", LogLevel.Debug);
            // 从文件读取代码并解析为AST
            var statement = i.Build(Apis.FromFile(path));
            Log("解析完成", LogLevel.Debug);

            // 调用另一个Compile重载方法编译AST
            return Compile(statement, path, i);
        }
        catch (Exception ex)
        {
            // 处理编译过程中的异常，输出详细错误信息
            LogFormat("\n{0}", LogLevel.Error, path);
            LogFormat("错误类型: {0}", LogLevel.Error, ex.GetType().Name);
            LogFormat("错误信息: {0}", LogLevel.Error, ex.Message);

            // 仅在调试模式下输出完整堆栈跟踪
            if (CurrentLogLevel >= LogLevel.Debug)
            {
                if (ex.StackTrace is not null)
                {
                    LogFormat("堆栈跟踪: {0}", LogLevel.Error, ex.StackTrace);
                }
            }

            // 尝试获取更详细的位置信息
            if (ex is SyntaxError syntaxError)
            {
                LogFormat("位置信息: {0}", LogLevel.Error, syntaxError.Position);
                if (syntaxError.SourceContext is { Length: > 0 })
                {
                    Log("上下文", LogLevel.Error);
                    foreach (var line in syntaxError.SourceContext)
                    {
                        LogFormat("  {0}", LogLevel.Error, line);
                    }
                }

                if (syntaxError.Suggestion is not null)
                {
                    LogFormat("建议: {0}", LogLevel.Error, syntaxError.Suggestion);
                }
            }

            throw;
        }
    }

    /// <summary>
    /// 应用文件头指令到编译器配置
    /// </summary>
    /// <param name="directives">文件头指令集合</param>
    public static void ApplyHeaderDirectives(FileHeaderDirectives directives)
    {
        // 处理 debug 指令
        if (directives.HasDirective("debug"))
        {
            DebugOutputEnabled = directives.GetBoolDirective("debug");
        }

        // 处理 verify-il 指令
        if (directives.HasDirective("verify-il"))
        {
            IlVerificationEnabled = directives.GetBoolDirective("verify-il", true);
        }

        // 处理 optimize 指令 (保留用于将来)
        if (directives.HasDirective("optimize"))
        {
            var optimizeLevel = directives.GetIntDirective("optimize");
            // 优化级别处理逻辑将来实现
            LogFormat("优化级别设置为: {0}", LogLevel.Info, optimizeLevel);
        }

        // 处理 type-inference 指令
        if (directives.HasDirective("type-inference"))
        {
            EnableTypeInference = directives.GetBoolDirective("type-inference", true);
        }

        // 处理 type-inference-debug 指令
        if (directives.HasDirective("type-inference-debug"))
        {
            TypeInferenceDebugOutput = directives.GetBoolDirective("type-inference-debug");
        }
    }
}