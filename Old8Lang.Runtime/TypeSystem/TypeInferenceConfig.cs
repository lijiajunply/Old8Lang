namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型推断策略配置
/// </summary>
public class TypeInferenceConfig
{
    private static readonly AsyncLocal<bool?> EnableTypeInferenceState = new();
    private static readonly AsyncLocal<bool?> InferParameterTypesFromCallsState = new();
    private static readonly AsyncLocal<bool?> InferReturnTypesFromBodyState = new();
    private static readonly AsyncLocal<bool?> FallbackToDynamicState = new();
    private static readonly AsyncLocal<double?> MinimumConfidenceState = new();
    private static readonly AsyncLocal<bool?> DebugOutputState = new();

    /// <summary>
    /// 单例实例
    /// </summary>
    public static TypeInferenceConfig Instance { get; } = new();

    private TypeInferenceConfig() { }

    /// <summary>
    /// 是否启用渐进式类型推断（默认：true；按异步执行上下文隔离，避免并发测试串味）
    /// </summary>
    public bool EnableTypeInference
    {
        get => EnableTypeInferenceState.Value ?? true;
        set => EnableTypeInferenceState.Value = value;
    }

    /// <summary>
    /// 是否允许从函数调用处推断参数类型（默认：true）
    /// </summary>
    public bool InferParameterTypesFromCalls
    {
        get => InferParameterTypesFromCallsState.Value ?? true;
        set => InferParameterTypesFromCallsState.Value = value;
    }

    /// <summary>
    /// 是否允许从return语句推断返回类型（默认：true）
    /// </summary>
    public bool InferReturnTypesFromBody
    {
        get => InferReturnTypesFromBodyState.Value ?? true;
        set => InferReturnTypesFromBodyState.Value = value;
    }

    /// <summary>
    /// 是否在无法推断时回退到动态类型（默认：true）
    /// </summary>
    public bool FallbackToDynamic
    {
        get => FallbackToDynamicState.Value ?? true;
        set => FallbackToDynamicState.Value = value;
    }

    /// <summary>
    /// 最小置信度阈值：低于此值的推断将被拒绝（默认：0.5）
    /// </summary>
    public double MinimumConfidence
    {
        get => MinimumConfidenceState.Value ?? 0.5;
        set => MinimumConfidenceState.Value = value;
    }

    /// <summary>
    /// 是否输出推断过程的调试信息（默认：false）
    /// </summary>
    public bool DebugOutput
    {
        get => DebugOutputState.Value ?? false;
        set => DebugOutputState.Value = value;
    }
}
