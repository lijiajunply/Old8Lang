using Old8Lang.AST.Statement;
using Old8Lang.TypeSystem;

namespace Old8Lang.Compiler;

/// <summary>
/// IL 编译器在共享运行时中的开关。
/// </summary>
/// <remarks>
/// 运行时值（异步函数体、状态机生成器）与文件头指令需要在编译前读写这些开关，
/// 但 Runtime 不能在编译期引用 <c>Old8Lang.Compiler</c> 程序集，因此把开关放在共享层。
/// `Old8Lang.Compiler.Compiler` 的同名属性委托到这里，保持既有公开 API 不变。
/// </remarks>
public static class IlCompilerOptions
{
    private static readonly AsyncLocal<bool?> DebugOutputState = new();
    private static readonly AsyncLocal<bool?> IlVerificationState = new();
    private static readonly AsyncLocal<bool?> AsyncStateMachineAwaitState = new();

    /// <summary>调试输出开关，默认为关闭（按异步执行上下文隔离，避免并发测试串味）。</summary>
    public static bool DebugOutputEnabled
    {
        get => DebugOutputState.Value ?? false;
        set => DebugOutputState.Value = value;
    }

    /// <summary>IL 代码验证开关，默认为开启（按异步执行上下文隔离）。</summary>
    public static bool IlVerificationEnabled
    {
        get => IlVerificationState.Value ?? true;
        set => IlVerificationState.Value = value;
    }

    /// <summary>启用异步状态机 await 编译，默认为开启（按异步执行上下文隔离）。</summary>
    public static bool EnableAsyncStateMachineAwait
    {
        get => AsyncStateMachineAwaitState.Value ?? true;
        set => AsyncStateMachineAwaitState.Value = value;
    }

    /// <summary>
    /// 把文件头指令应用到编译配置。
    /// </summary>
    public static void ApplyHeaderDirectives(FileHeaderDirectives directives)
    {
        ArgumentNullException.ThrowIfNull(directives);

        if (directives.HasDirective("debug"))
        {
            DebugOutputEnabled = directives.GetBoolDirective("debug");
        }

        if (directives.HasDirective("verify-il"))
        {
            IlVerificationEnabled = directives.GetBoolDirective("verify-il", true);
        }

        if (directives.HasDirective("type-inference"))
        {
            TypeInferenceConfig.Instance.EnableTypeInference = directives.GetBoolDirective("type-inference", true);
        }

        if (directives.HasDirective("type-inference-debug"))
        {
            TypeInferenceConfig.Instance.DebugOutput = directives.GetBoolDirective("type-inference-debug");
        }
    }
}
