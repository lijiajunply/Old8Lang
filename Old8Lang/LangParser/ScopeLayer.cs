using Old8Lang.AST.Expression;

namespace Old8Lang.LangParser;

/// <summary>
/// 作用域层：支持分层的 Copy-On-Write 优化（未来基础设施）
/// 通过差异层实现零拷贝读取和延迟写入
/// </summary>
/// <remarks>
/// ⚠️ 当前状态：已实现但未集成到 VariateManager
///
/// 原因：
/// - 需要大幅修改 VariateManager 的作用域管理逻辑
/// - 需要修改所有变量读写方法以支持 ScopeLayer
/// - 工作量大，需要全面测试
///
/// 未来计划：
/// - 阶段1：完成 VariateManager 与 ScopeLayer 的集成
/// - 阶段2：在只读闭包场景中使用 ScopeLayer
/// - 阶段3：性能基准测试和优化
///
/// 架构说明：
/// - BaseScope: 基础作用域（只读，可被多个 ScopeLayer 共享）
/// - DeltaScope: 差异层（写入的变量保存在这里）
/// - 读取时：先查 DeltaScope，再查 BaseScope
/// - 写入时：直接写入 DeltaScope，不影响 BaseScope
///
/// 优势：
/// - 零拷贝：创建闭包时不需要深拷贝作用域
/// - 隔离性：写入不影响基础作用域
/// - 性能：只读闭包完全无开销
/// </remarks>
public class ScopeLayer
{
    /// <summary>
    /// 基础作用域（只读共享）
    /// </summary>
    private readonly Dictionary<string, LangValueType>? BaseScope;

    /// <summary>
    /// 差异层（写入变量）
    /// </summary>
    private Dictionary<string, LangValueType>? DeltaScope;

    /// <summary>
    /// 创建一个独立的作用域层（无基础作用域）
    /// </summary>
    public ScopeLayer()
    {
        BaseScope = null;
        DeltaScope = new Dictionary<string, LangValueType>();
    }

    /// <summary>
    /// 创建一个基于现有作用域的 COW 层
    /// </summary>
    /// <param name="baseScope">基础作用域（只读共享）</param>
    public ScopeLayer(Dictionary<string, LangValueType> baseScope)
    {
        BaseScope = baseScope;
        DeltaScope = null; // 延迟创建，只在首次写入时创建
    }

    /// <summary>
    /// 读取变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="value">变量值</param>
    /// <returns>是否找到变量</returns>
    public bool TryGetValue(string name, out LangValueType? value)
    {
        // 优先从差异层查找
        if (DeltaScope?.TryGetValue(name, out value) == true)
        {
            return true;
        }

        // 再从基础作用域查找
        if (BaseScope?.TryGetValue(name, out value) == true)
        {
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// 写入变量（COW：仅写入差异层）
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="value">变量值</param>
    public void SetValue(string name, LangValueType value)
    {
        // 延迟创建差异层
        DeltaScope ??= new Dictionary<string, LangValueType>();

        // 写入差异层（不影响基础作用域）
        DeltaScope[name] = value;
    }

    /// <summary>
    /// 检查变量是否存在
    /// </summary>
    /// <param name="name">变量名</param>
    /// <returns>是否存在</returns>
    public bool ContainsKey(string name)
    {
        return DeltaScope?.ContainsKey(name) == true || BaseScope?.ContainsKey(name) == true;
    }

    /// <summary>
    /// 获取所有变量（合并基础作用域和差异层）
    /// </summary>
    /// <returns>变量字典</returns>
    public Dictionary<string, LangValueType> GetAllVariables()
    {
        var result = new Dictionary<string, LangValueType>();

        // 先复制基础作用域
        if (BaseScope != null)
        {
            foreach (var (key, value) in BaseScope)
            {
                result[key] = value;
            }
        }

        // 再应用差异层（覆盖同名变量）
        if (DeltaScope != null)
        {
            foreach (var (key, value) in DeltaScope)
            {
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// 清空差异层
    /// </summary>
    public void ClearDelta()
    {
        DeltaScope?.Clear();
    }

    /// <summary>
    /// 获取差异层的大小（用于性能监控）
    /// </summary>
    public int DeltaSize => DeltaScope?.Count ?? 0;

    /// <summary>
    /// 是否有写入操作（差异层是否非空）
    /// </summary>
    public bool HasWrites => DeltaScope != null && DeltaScope.Count > 0;

    /// <summary>
    /// 扁平化：将差异层合并到基础作用域，返回新的独立字典
    /// </summary>
    /// <returns>合并后的独立字典</returns>
    public Dictionary<string, LangValueType> Flatten()
    {
        return GetAllVariables();
    }
}
