using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Interpreter;

/// <summary>
/// 对象池管理器，管理各种类型的值对象池
/// </summary>
public class ObjectPoolManager
{
    /// <summary>
    /// BoolLangValue对象池
    /// </summary>
    public ObjectPool<BoolLangValue> BoolPool { get; } = new(() => new BoolLangValue());

    /// <summary>
    /// IntLangValue对象池
    /// </summary>
    public ObjectPool<IntLangValue> IntPool { get; } = new(() => new IntLangValue());

    /// <summary>
    /// DoubleLangValue对象池
    /// </summary>
    public ObjectPool<DoubleLangValue> DoublePool { get; } = new(() => new DoubleLangValue());

    /// <summary>
    /// StringLangValue对象池
    /// </summary>
    public ObjectPool<StringLangValue> StringPool { get; } = new(() => new StringLangValue());

    /// <summary>
    /// CharLangValue对象池
    /// </summary>
    public ObjectPool<CharLangValue> CharPool { get; } = new(() => new CharLangValue());

    /// <summary>
    /// ControlFlowState对象池，用于循环控制流状态复用
    /// </summary>
    public ObjectPool<ControlFlowManager.ControlFlowState> ControlFlowStatePool { get; } =
        new(() => new ControlFlowManager.ControlFlowState());

    /// <summary>
    /// 单例实例
    /// </summary>
    public static ObjectPoolManager Instance { get; } = new();

    /// <summary>
    /// 私有构造函数，避免外部实例化
    /// </summary>
    private ObjectPoolManager() { }

    /// <summary>
    /// 重置所有对象池
    /// </summary>
    public void ResetAllPools()
    {
        // 对象池使用ConcurrentBag实现，不需要显式重置
    }
}