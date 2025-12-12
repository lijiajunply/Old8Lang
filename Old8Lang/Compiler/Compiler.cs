using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.Compiler;

public static class Compiler
{
    /// <summary>
    /// 调试输出开关，默认为关闭
    /// </summary>
    public static bool DebugOutputEnabled { get; set; }

    /// <summary>
    /// 日志级别枚举
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
    /// 条件输出调试信息
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

    public static Action Compile(BlockStatement statement, string path, LangInterpreter i)
    {
        LogFormat("开始编译: {0}", LogLevel.Debug, path);
        LogFormat("语句类型: {0}", LogLevel.Debug, statement.GetType().Name);

        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path, Interpreter = i };

        try
        {
            Log("开始生成IL代码", LogLevel.Debug);
            statement.GenerateIl(ilGenerator, local);
            Log("IL代码生成完成", LogLevel.Debug);

            ilGenerator.Emit(OpCodes.Ret);

            Log("创建委托", LogLevel.Debug);
            var oldLangRun = (Action)dynamicMethod.CreateDelegate(typeof(Action));
            Log("编译成功");

            return oldLangRun;
        }
        catch (Exception ex)
        {
            LogFormat("\n{0}", LogLevel.Error, path);
            LogFormat("错误类型: {0}", LogLevel.Error, ex.GetType().Name);
            LogFormat("错误信息: {0}", LogLevel.Error, ex.Message);

            // 仅在调试模式下输出完整堆栈跟踪
            if (CurrentLogLevel >= LogLevel.Debug)
            {
                if (ex.StackTrace != null)
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

                if (invalidOpError.Suggestion != null)
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

    public static Action Compile(string path, LangInterpreter i)
    {
        LogFormat("开始编译文件: {0}", LogLevel.Debug, path);

        try
        {
            Log("解析代码", LogLevel.Debug);
            var statement = i.Build(Apis.FromFile(path));
            Log("解析完成", LogLevel.Debug);

            return Compile(statement, path, i);
        }
        catch (Exception ex)
        {
            LogFormat("\n{0}", LogLevel.Error, path);
            LogFormat("错误类型: {0}", LogLevel.Error, ex.GetType().Name);
            LogFormat("错误信息: {0}", LogLevel.Error, ex.Message);

            // 仅在调试模式下输出完整堆栈跟踪
            if (CurrentLogLevel >= LogLevel.Debug)
            {
                if (ex.StackTrace != null)
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

                if (syntaxError.Suggestion != null)
                {
                    LogFormat("建议: {0}", LogLevel.Error, syntaxError.Suggestion);
                }
            }

            throw;
        }
    }
}