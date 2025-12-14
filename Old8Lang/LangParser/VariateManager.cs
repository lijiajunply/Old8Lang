using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.LangParser;

/// <summary>
/// Old8Lang变量管理器，负责管理变量作用域、返回值、导入信息等
/// </summary>
/// <remarks>
/// 该类是Old8Lang解释器的核心组件之一，主要负责：
/// - 管理变量作用域栈（全局作用域和局部作用域）
/// - 处理函数返回值和返回状态
/// - 管理导入的函数、类和原生类型
/// - 跟踪递归深度，防止栈溢出
/// - 提供变量状态查询功能
/// </remarks>
public class VariateManager
{
    #region Lang

    /// <summary>
    /// 语言信息，包含库信息和导入路径等
    /// </summary>
    public LangInfo? LangInfo { get; set; }

    /// <summary>
    /// 当前源代码文件路径
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// 关联的解释器实例
    /// </summary>
    [NotNull]
    public LangInterpreter? Interpreter { get; set; }

    #endregion

    #region Variate

    /// <summary>
    /// 作用域栈，每个作用域是一个字典
    /// </summary>
    /// <remarks>
    /// Scopes[0] 是全局作用域，Scopes[^1] 是当前作用域
    /// 作用域栈用于实现变量的作用域规则和变量查找
    /// </remarks>
    private List<Dictionary<string, LangValueType>> Scopes { get; } = [new()];

    /// <summary>
    /// 作用域缓存池，用于复用作用域字典，减少内存分配和垃圾回收开销
    /// </summary>
    private static readonly ConcurrentBag<Dictionary<string, LangValueType>> ScopeCache = new();

    /// <summary>
    /// 缓存池最大大小
    /// </summary>
    private const int MaxScopeCacheSize = 100;

    /// <summary>
    /// 导入信息列表，包含导入的函数、类和原生类型
    /// </summary>
    public List<ImportInfo> ImportInfos { get; } = [];

    #endregion

    #region Return

    /// <summary>
    /// 是否处于返回状态
    /// </summary>
    public bool IsReturn { get; set; }

    /// <summary>
    /// 返回结果值
    /// </summary>
    public LangValueType Result { get; set; } = new VoidLangValue();

    #endregion

    #region Block

    /// <summary>
    /// 当前是否处于函数内部
    /// </summary>
    public bool IsFunc { get; set; }

    /// <summary>
    /// 当前是否处于类内部
    /// </summary>
    public bool IsClass { get; set; }

    /// <summary>
    /// 控制流管理器，用于管理break和continue等控制流语句的状态
    /// </summary>
    public ControlFlowManager ControlFlowManager { get; } = new();

    /// <summary>
    /// 最大递归深度限制，防止栈溢出
    /// </summary>
    private const int MaxRecursionDepth = 1000;

    /// <summary>
    /// 当前递归深度
    /// </summary>
    /// <exception cref="RecursionError">当递归深度超过最大值时抛出</exception>
    public int RecursionDepth
    {
        get;
        set
        {
            if (value > MaxRecursionDepth)
            {
                throw new RecursionError(
                    new SourcePosition(0, 0),
                    MaxRecursionDepth
                );
            }

            field = value;
        }
    }

    #endregion

    /// <summary>
    /// 设置变量值
    /// </summary>
    /// <param name="id">变量标识符</param>
    /// <param name="langValueType">变量值</param>
    /// <remarks>
    /// 变量查找规则：
    /// 1. 如果在函数内部，直接在当前作用域创建新变量
    /// 2. 否则，从当前作用域向上查找，找到则更新值
    /// 3. 未找到则在当前作用域创建新变量
    /// </remarks>
    public void Set(LangId id, LangValueType langValueType)
    {
        // 检查是否是函数调用中的参数设置
        // 如果是，直接添加到当前作用域，创建新的局部变量
        if (IsFunc)
        {
            Scopes[^1][id.IdName] = langValueType;
            return;
        }

        // 1. 先查找变量，从当前作用域向父作用域查找
        for (var i = Scopes.Count - 1; i >= 0; i--)
        {
            if (!Scopes[i].ContainsKey(id.IdName)) continue;
            Scopes[i][id.IdName] = langValueType;
            return;
        }

        // 3. 没有找到变量，在当前作用域中创建新变量
        Scopes[^1][id.IdName] = langValueType;
    }

    /// <summary>
    /// 添加新的子作用域（进入块语句）
    /// </summary>
    public void AddChildren()
    {
        // 优化：从缓存池获取作用域字典，减少内存分配
        if (ScopeCache.TryTake(out var cachedScope))
        {
            // 确保缓存的作用域是空的
            cachedScope.Clear();
            Scopes.Add(cachedScope);
        }
        else
        {
            // 缓存池为空时创建新的作用域
            Scopes.Add(new Dictionary<string, LangValueType>());
        }
    }

    /// <summary>
    /// 移除当前作用域（退出块语句）
    /// </summary>
    /// <remarks>
    /// 全局作用域（Scopes[0]）不能被移除
    /// </remarks>
    public void RemoveChildren()
    {
        // 移除当前作用域
        if (Scopes.Count > 1)
        {
            var scopeToRemove = Scopes[^1];
            Scopes.RemoveAt(Scopes.Count - 1);

            // 优化：清空作用域并将其归还到缓存池，以便复用
            scopeToRemove.Clear();
            if (ScopeCache.Count < MaxScopeCacheSize)
            {
                ScopeCache.Add(scopeToRemove);
            }
        }
    }

    /// <summary>
    /// 获取变量值
    /// </summary>
    /// <param name="id">变量标识符</param>
    /// <returns>变量值，如果未找到则返回null</returns>
    /// <remarks>
    /// 变量查找规则：
    /// 1. 从当前作用域（栈顶）向全局作用域（栈底）查找
    /// 2. 如果未找到，尝试从导入信息中查找
    /// </remarks>
    public LangValueType? GetValue(LangId id)
    {
        // 从当前作用域（栈顶）向全局作用域（栈底）查找
        for (var i = Scopes.Count - 1; i >= 0; i--)
        {
            if (Scopes[i].TryGetValue(id.IdName, out var value))
            {
                return value;
            }
        }

        // 如果还是没有找到，尝试查找导入的函数或类
        return GetAny(id);
    }

    /// <summary>
    /// 根据函数名和参数数量查找函数
    /// </summary>
    /// <param name="id">函数标识符</param>
    /// <param name="paramCount">参数数量</param>
    /// <returns>找到的函数或null</returns>
    public FuncLangValue? GetFunc(LangId id, int paramCount)
    {
        return ImportInfos.FirstOrDefault(x =>
            x is FuncLangValue func &&
            func.Id!.IdName == id.IdName &&
            func.Ids?.Count == paramCount) as FuncLangValue;
    }

    /// <summary>
    /// 从导入信息中查找任意类型的导入项
    /// </summary>
    /// <param name="id">要查找的标识符</param>
    /// <returns>找到的导入项或null</returns>
    public ImportInfo? GetAny(LangId id)
    {
        return ImportInfos.FirstOrDefault(x =>
        {
            return x switch
            {
                FuncLangValue func => func.Id!.IdName == id.IdName,
                TypeTemplate template => template.ClassName == id.IdName,
                NativeAnyLangValue na => na.RegisterName == id.IdName, // 使用 RegisterName 而不是 ClassName
                NativeStaticAny staticAny => staticAny.ClassName == id.IdName,
                _ => false
            };
        });
    }

    /// <summary>
    /// 添加类或函数到导入信息列表
    /// </summary>
    /// <param name="langValue">要添加的导入信息</param>
    public void AddClassAndFunc(ImportInfo langValue)
    {
        ImportInfos.Add(langValue);
    }

    /// <summary>
    /// 在当前作用域添加变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="langValueType">变量值</param>
    /// <exception cref="DuplicateNameError">当变量名已存在时抛出</exception>
    private void AddVariate(string name, LangValueType langValueType)
    {
        // 检查当前作用域中是否已存在同名变量
        if (Scopes[^1].ContainsKey(name))
        {
            // 创建一个默认的SourcePosition，因为AddVariate方法没有位置信息
            // 在实际使用中，应该从调用处传递位置信息
            throw new DuplicateNameError(langValueType.Position, name, "变量");
        }

        Scopes[^1][name] = langValueType;
    }

    /// <summary>
    /// 清除返回状态和结果
    /// </summary>
    public void ClearReturn()
    {
        IsReturn = false;
        Result = new VoidLangValue();
    }

    /// <summary>
    /// 初始化变量管理器，添加多个变量
    /// </summary>
    /// <param name="result">要添加的变量字典</param>
    public void Init(Dictionary<string, LangValueType> result)
    {
        // 初始化方法实现
        // 将结果添加到管理器中
        foreach (var item in result)
        {
            // 使用 AddVariate 方法添加变量
            AddVariate(item.Key, item.Value);
        }
    }

    /// <summary>
    /// 克隆变量管理器实例
    /// </summary>
    /// <returns>克隆后的变量管理器实例</returns>
    public VariateManager Clone()
    {
        // 克隆方法实现
        var newManager = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = Interpreter,
            IsFunc = IsFunc,
            IsClass = IsClass,
            IsReturn = IsReturn,
            Result = Result
        };

        // 深拷贝作用域栈
        foreach (var scope in Scopes)
        {
            var newScope = new Dictionary<string, LangValueType>(scope);
            newManager.Scopes.Add(newScope);
        }

        // 移除初始化时的空作用域（因为构造函数已经创建了一个）
        if (newManager.Scopes.Count > 0 && Scopes.Count > 0)
        {
            newManager.Scopes.RemoveAt(0);
        }

        // 复制导入信息
        newManager.ImportInfos.AddRange(ImportInfos);

        return newManager;
    }

    /// <summary>
    /// 创建新的变量管理器实例（与Clone方法类似）
    /// </summary>
    /// <returns>新的变量管理器实例</returns>
    public VariateManager NewManger()
    {
        var newManager = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = Interpreter,
            // 复制返回和函数状态
            IsReturn = IsReturn,
            Result = Result,
            IsFunc = IsFunc,
            IsClass = IsClass
        };

        // 深拷贝作用域栈
        foreach (var newScope in Scopes.Select(scope => new Dictionary<string, LangValueType>(scope)))
        {
            newManager.Scopes.Add(newScope);
        }

        // 移除初始化时的空作用域（因为构造函数已经创建了一个）
        if (newManager.Scopes.Count > 0 && Scopes.Count > 0)
        {
            newManager.Scopes.RemoveAt(0);
        }

        // 复制导入信息
        newManager.ImportInfos.AddRange(ImportInfos);

        return newManager;
    }

    /// <summary>
    /// 获取当前作用域和父作用域中的变量信息
    /// </summary>
    /// <param name="limit">限制返回的变量数量</param>
    /// <returns>变量信息字典，键为变量名，值为变量值的字符串表示</returns>
    /// <remarks>
    /// 该方法用于调试和错误报告，从当前作用域向上遍历，收集变量信息
    /// </remarks>
    public Dictionary<string, string> GetVariableStates(int limit = 20)
    {
        var variableStates = new Dictionary<string, string>();

        // 从当前作用域向上遍历，收集变量信息
        for (int i = Scopes.Count - 1; i >= 0 && variableStates.Count < limit; i--)
        {
            var scope = Scopes[i];
            foreach (var (varName, varValue) in scope)
            {
                if (variableStates.Count >= limit) break;
                variableStates[varName] = varValue.ToString();
            }
        }

        return variableStates;
    }
}