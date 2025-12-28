namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型推断策略配置
/// </summary>
public class TypeInferenceConfig
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static TypeInferenceConfig Instance { get; } = new();

    private TypeInferenceConfig() { }

    /// <summary>
    /// 是否启用渐进式类型推断（默认：true）
    /// </summary>
    public bool EnableTypeInference { get; set; } = true;

    /// <summary>
    /// 是否允许从函数调用处推断参数类型（默认：true）
    /// </summary>
    public bool InferParameterTypesFromCalls { get; set; } = true;

    /// <summary>
    /// 是否允许从return语句推断返回类型（默认：true）
    /// </summary>
    public bool InferReturnTypesFromBody { get; set; } = true;

    /// <summary>
    /// 是否在无法推断时回退到动态类型（默认：true）
    /// </summary>
    public bool FallbackToDynamic { get; set; } = true;

    /// <summary>
    /// 最小置信度阈值：低于此值的推断将被拒绝（默认：0.5）
    /// </summary>
    public double MinimumConfidence { get; set; } = 0.5;

    /// <summary>
    /// 是否输出推断过程的调试信息（默认：false）
    /// </summary>
    public bool DebugOutput { get; set; }
}
