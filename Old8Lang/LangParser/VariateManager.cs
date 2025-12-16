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
    internal List<Dictionary<string, LangValueType>> Scopes { get; } = [new()];

    /// <summary>
    /// COW（Copy-On-Write）标记数组，记录每个作用域是否共享
    /// </summary>
    /// <remarks>
    /// - true: 作用域被共享（多个VariateManager引用），写入时需要拷贝
    /// - false: 作用域未共享，可以直接修改
    /// 用于优化闭包捕获，避免不必要的深拷贝
    /// </remarks>
    private readonly List<bool> ScopeSharedFlags = [false];

    /// <summary>
    /// 获取当前作用域的所有变量
    /// </summary>
    /// <returns>当前作用域的变量字典</returns>
    public Dictionary<string, LangValueType> GetCurrentScope()
    {
        return Scopes[^1];
    }

    /// <summary>
    /// 获取父作用域的所有变量
    /// </summary>
    /// <returns>父作用域的变量字典，如果没有父作用域则返回null</returns>
    public Dictionary<string, LangValueType>? GetParentScope()
    {
        return Scopes.Count > 1 ? Scopes[^2] : null;
    }

    /// <summary>
    /// 将变量添加到父作用域
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="value">变量值</param>
    public void AddToParentScope(string name, LangValueType value)
    {
        int targetScopeIndex;
        if (Scopes.Count < 2)
        {
            // 没有父作用域，直接添加到当前作用域
            targetScopeIndex = Scopes.Count - 1;
            EnsureScopeNotShared(targetScopeIndex); // COW: 确保作用域未共享
            Scopes[^1][name] = value;
        }
        else
        {
            // 添加到父作用域
            targetScopeIndex = Scopes.Count - 2;
            EnsureScopeNotShared(targetScopeIndex); // COW: 确保父作用域未共享
            Scopes[^2][name] = value;
        }

        // 更新缓存
        _lookupCache ??= new Dictionary<string, (int scopeIndex, LangValueType value)>();
        _lookupCache[name] = (targetScopeIndex, value);
    }

    /// <summary>
    /// 作用域缓存池，使用ThreadLocal的Stack避免并发同步开销
    /// </summary>
    private static readonly ThreadLocal<Stack<Dictionary<string, LangValueType>>> ScopeCache =
        new(() => new Stack<Dictionary<string, LangValueType>>());

    /// <summary>
    /// 临时VariateManager缓存池，用于点操作等临时场景
    /// </summary>
    private static readonly ThreadLocal<Stack<VariateManager>> TempManagerPool =
        new(() => new Stack<VariateManager>());

    /// <summary>
    /// 缓存池最大大小
    /// </summary>
    private const int MaxScopeCacheSize = 100;

    /// <summary>
    /// 临时管理器池最大大小
    /// </summary>
    private const int MaxTempManagerPoolSize = 10;

    /// <summary>
    /// 变量查找缓存，使用ThreadStatic避免线程同步开销
    /// </summary>
    [ThreadStatic] private static Dictionary<string, (int scopeIndex, LangValueType value)>? _lookupCache;

    /// <summary>
    /// 导入信息列表，包含导入的函数、类和原生类型
    /// 使用内部 List 和锁来保证线程安全
    /// </summary>
    private readonly List<ImportInfo> ImportInfosList = [];

    /// <summary>
    /// 保护 ImportInfos 的锁对象
    /// </summary>
    private readonly object ImportInfosLock = new();

    /// <summary>
    /// 公开的 ImportInfos 访问器（线程安全）
    /// </summary>
    public IEnumerable<ImportInfo> ImportInfos
    {
        get
        {
            lock (ImportInfosLock)
            {
                return ImportInfosList.ToList(); // 返回副本以避免外部修改
            }
        }
    }

    #endregion

    #region Return

    /// <summary>
    /// 标记是否是return状态
    /// </summary>
    public bool IsReturn { get; set; }

    /// <summary>
    /// 标记是否是yield状态
    /// </summary>
    public bool IsYield { get; set; }

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
    /// 导入栈，用于检测循环依赖
    /// </summary>
    public Stack<string> ImportStack { get; set; } = new();

    /// <summary>
    /// 控制流管理器，用于管理break和continue等控制流语句的状态
    /// </summary>
    public ControlFlowManager ControlFlowManager { get; } = new();

    /// <summary>
    /// 标记当前是否在生成器上下文中执行
    /// 用于区分生成器的yield暂停机制和普通循环的执行
    /// </summary>
    public bool IsInGenerator { get; set; } = false;

    /// <summary>
    /// 生成器执行上下文（仅在生成器执行时存在）
    /// 替代全局的IsYield标志，每个生成器实例都有独立的上下文
    /// </summary>
    /// <remarks>
    /// 当此属性不为null时，表示当前正在执行生成器函数
    /// 生成器的状态信息（如yield点、当前值等）都保存在此上下文中
    /// </remarks>
    public GeneratorExecutionContext? GeneratorContext { get; set; }

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

    #region Copy-On-Write Support

    /// <summary>
    /// 标记作用域为共享状态（用于闭包捕获）
    /// </summary>
    private void MarkScopesAsShared()
    {
        // 确保标记数组大小与作用域数组一致
        while (ScopeSharedFlags.Count < Scopes.Count)
        {
            ScopeSharedFlags.Add(false);
        }

        // 标记所有作用域为共享
        for (int i = 0; i < Scopes.Count; i++)
        {
            ScopeSharedFlags[i] = true;
        }
    }

    /// <summary>
    /// 在写入前确保作用域未共享（COW机制）
    /// </summary>
    /// <param name="scopeIndex">作用域索引</param>
    private void EnsureScopeNotShared(int scopeIndex)
    {
        // 确保标记数组大小足够
        while (ScopeSharedFlags.Count <= scopeIndex)
        {
            ScopeSharedFlags.Add(false);
        }

        // 如果作用域被标记为共享，则复制一份
        if (ScopeSharedFlags[scopeIndex])
        {
            var originalScope = Scopes[scopeIndex];
            var copiedScope = new Dictionary<string, LangValueType>(originalScope);
            Scopes[scopeIndex] = copiedScope;
            ScopeSharedFlags[scopeIndex] = false;

            // 清除查找缓存，因为作用域引用已改变
            _lookupCache?.Clear();
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
    ///
    /// COW优化：写入前确保作用域未共享
    /// </remarks>
    public void Set(LangId id, LangValueType langValueType)
    {
        // 检查是否是函数调用中的参数设置
        // 如果是，直接添加到当前作用域，创建新的局部变量
        if (IsFunc)
        {
            var currentIndex = Scopes.Count - 1;
            EnsureScopeNotShared(currentIndex); // COW: 确保作用域未共享
            Scopes[^1][id.IdName] = langValueType;

            // 更新缓存
            _lookupCache ??= new Dictionary<string, (int scopeIndex, LangValueType value)>();
            _lookupCache[id.IdName] = (currentIndex, langValueType);
            return;
        }

        // 1. 先查找变量，从当前作用域向父作用域查找
        for (var i = Scopes.Count - 1; i >= 0; i--)
        {
            if (!Scopes[i].ContainsKey(id.IdName)) continue;
            EnsureScopeNotShared(i); // COW: 确保作用域未共享
            Scopes[i][id.IdName] = langValueType;

            // 更新缓存
            _lookupCache ??= new Dictionary<string, (int scopeIndex, LangValueType value)>();
            _lookupCache[id.IdName] = (i, langValueType);
            return;
        }

        // 2. 没有找到变量，在当前作用域创建新变量
        var currentScopeIndex = Scopes.Count - 1;
        EnsureScopeNotShared(currentScopeIndex); // COW: 确保作用域未共享
        Scopes[^1][id.IdName] = langValueType;

        // 更新缓存
        _lookupCache ??= new Dictionary<string, (int scopeIndex, LangValueType value)>();
        _lookupCache[id.IdName] = (currentScopeIndex, langValueType);
    }

    /// <summary>
    /// 添加新的子作用域（进入块语句）
    /// </summary>
    public void AddChildren()
    {
        var cache = ScopeCache.Value!;

        // 优化：从缓存池获取作用域字典，减少内存分配
        if (cache.Count > 0)
        {
            var cachedScope = cache.Pop();
            // 确保缓存的作用域是空的
            cachedScope.Clear();
            Scopes.Add(cachedScope);
        }
        else
        {
            // 缓存池为空时创建新的作用域
            Scopes.Add(new Dictionary<string, LangValueType>());
        }

        // COW: 添加对应的共享标记（新作用域默认未共享）
        ScopeSharedFlags.Add(false);

        // 作用域变化，清理查找缓存
        _lookupCache?.Clear();
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

            // COW: 同步移除共享标记
            if (ScopeSharedFlags.Count > 0)
            {
                ScopeSharedFlags.RemoveAt(ScopeSharedFlags.Count - 1);
            }

            // 优化：清空作用域并将其归还到缓存池，以便复用
            scopeToRemove.Clear();
            var cache = ScopeCache.Value!;
            if (cache.Count < MaxScopeCacheSize)
            {
                cache.Push(scopeToRemove);
            }

            // 作用域变化，清理查找缓存
            _lookupCache?.Clear();
        }
    }

    /// <summary>
    /// 获取变量值
    /// </summary>
    /// <param name="id">变量标识符</param>
    /// <returns>变量值，如果未找到则返回null</returns>
    /// <remarks>
    /// 变量查找规则：
    /// 1. 优先检查缓存（快速路径）
    /// 2. 从当前作用域（栈顶）向全局作用域（栈底）查找
    /// 3. 如果未找到，尝试从导入信息中查找
    /// 4. 找到后更新缓存
    /// </remarks>
    public LangValueType? GetValue(LangId id)
    {
        // 快速路径：检查缓存
        if (_lookupCache?.TryGetValue(id.IdName, out var cached) == true
            && cached.scopeIndex < Scopes.Count
            && Scopes[cached.scopeIndex].TryGetValue(id.IdName, out var cachedValue))
        {
            return cachedValue;
        }

        // 慢速路径：完整查找
        for (var i = Scopes.Count - 1; i >= 0; i--)
        {
            if (Scopes[i].TryGetValue(id.IdName, out var value))
            {
                // 更新缓存
                _lookupCache ??= new Dictionary<string, (int scopeIndex, LangValueType value)>();
                _lookupCache[id.IdName] = (i, value);
                return value;
            }
        }

        // 如果还是没有找到，尝试查找导入的函数或类
        return GetAny(id);
    }

    /// <summary>
    /// 根据函数名和参数数量查找函数（支持同步和异步函数）
    /// </summary>
    /// <param name="id">函数标识符</param>
    /// <param name="paramCount">参数数量</param>
    /// <returns>找到的函数（FuncLangValue或AsyncFuncLangValue）或null</returns>
    public ImportInfo? GetFunc(LangId id, int paramCount)
    {
        return ImportInfos.FirstOrDefault(x =>
            (x is FuncLangValue func && func.Id!.IdName == id.IdName && func.Ids?.Count == paramCount) ||
            (x is AsyncFuncLangValue asyncFunc && asyncFunc.Id!.IdName == id.IdName &&
             asyncFunc.Ids?.Count == paramCount));
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
    /// 添加类或函数到导入信息列表（线程安全）
    /// </summary>
    /// <param name="langValue">要添加的导入信息</param>
    public void AddClassAndFunc(ImportInfo langValue)
    {
        lock (ImportInfosLock)
        {
            ImportInfosList.Add(langValue);
        }
    }

    /// <summary>
    /// 批量添加导入信息到列表（线程安全）
    /// </summary>
    /// <param name="items">要添加的导入信息集合</param>
    public void AddImportInfoRange(IEnumerable<ImportInfo> items)
    {
        lock (ImportInfosLock)
        {
            ImportInfosList.AddRange(items);
        }
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

        var currentScopeIndex = Scopes.Count - 1;
        Scopes[^1][name] = langValueType;

        // 更新缓存
        _lookupCache ??= new Dictionary<string, (int scopeIndex, LangValueType value)>();
        _lookupCache[name] = (currentScopeIndex, langValueType);
    }

    /// <summary>
    /// 清除返回状态和结果
    /// </summary>
    public void ClearReturn()
    {
        IsReturn = false;
        IsYield = false;
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
        return CloneInternal(copyIsYield: true);
    }

    /// <summary>
    /// 创建新的变量管理器实例（与Clone方法类似）
    /// </summary>
    /// <returns>新的变量管理器实例</returns>
    public VariateManager NewManger()
    {
        return CloneInternal(copyIsYield: false);
    }

    /// <summary>
    /// 克隆变量管理器的内部实现
    /// </summary>
    /// <param name="copyIsYield">是否复制 IsYield 字段</param>
    /// <returns>克隆后的变量管理器实例</returns>
    private VariateManager CloneInternal(bool copyIsYield)
    {
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

        // 仅在需要时复制 IsYield（Clone 需要，NewManger 不需要）
        if (copyIsYield)
        {
            newManager.IsYield = IsYield;
        }

        // 深拷贝作用域栈
        newManager.Scopes.Clear(); // 清除构造函数创建的初始作用域
        foreach (var scope in Scopes)
        {
            var newScope = new Dictionary<string, LangValueType>(scope);
            newManager.Scopes.Add(newScope);
        }

        // 复制导入信息（线程安全）
        lock (ImportInfosLock)
        {
            lock (newManager.ImportInfosLock)
            {
                newManager.ImportInfosList.AddRange(ImportInfosList);
            }
        }

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

    /// <summary>
    /// 从池中获取临时VariateManager，用于点操作等临时场景
    /// </summary>
    public static VariateManager GetTemp()
    {
        var pool = TempManagerPool.Value!;
        if (pool.Count > 0)
        {
            var manager = pool.Pop();
            return manager;
        }

        return new VariateManager();
    }

    /// <summary>
    /// 将临时VariateManager归还到池中
    /// </summary>
    public static void ReturnTemp(VariateManager manager)
    {
        // 清空管理器状态
        manager.Clear();

        var pool = TempManagerPool.Value!;
        if (pool.Count < MaxTempManagerPoolSize)
        {
            pool.Push(manager);
        }
    }

    /// <summary>
    /// 清空VariateManager状态，准备复用
    /// </summary>
    private void Clear()
    {
        // 只保留全局作用域，移除所有局部作用域
        while (Scopes.Count > 1)
        {
            RemoveChildren();
        }

        // 清空全局作用域中的变量
        EnsureScopeNotShared(0); // COW: 确保全局作用域未共享
        Scopes[0].Clear();

        // 清空导入信息（线程安全）
        lock (ImportInfosLock)
        {
            ImportInfosList.Clear();
        }

        // 重置标志位
        IsReturn = false;
        IsFunc = false;
        IsClass = false;
        Result = new VoidLangValue();
        RecursionDepth = 0;

        // 清理查找缓存
        _lookupCache?.Clear();
    }

    /// <summary>
    /// 为闭包创建作用域快照（浅拷贝优化 + COW）- 已废弃
    /// </summary>
    /// <returns>包含当前作用域快照的新VariateManager</returns>
    /// <remarks>
    /// ⚠️ 已废弃：此方法不适用于需要修改外部变量的闭包
    ///
    /// 使用 COW（Copy-On-Write）策略：
    /// - 初始时直接引用作用域（零拷贝）
    /// - 标记作用域为共享状态
    /// - 首次写入时才进行拷贝
    ///
    /// 局限性：
    /// - COW 在写入时会创建独立副本，导致闭包与原作用域隔离
    /// - 闭包无法修改外部作用域的变量（与 Old8Lang 语义不符）
    /// - 仅适用于完全只读的场景（极少见）
    ///
    /// 替代方案：
    /// - 需要修改外部变量的闭包：使用 Clone() 深拷贝
    /// - 只读闭包：使用 CaptureForReadOnlyClosure()（基于 ScopeLayer）
    /// - 生成器：使用 CloneForGenerator()
    /// </remarks>
    [Obsolete("此方法不适用于需要修改外部变量的闭包，请使用 Clone() 或 CaptureForReadOnlyClosure()")]
    public VariateManager CaptureForClosure()
    {
        var captured = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = Interpreter
        };

        // COW优化：直接引用所有作用域（零拷贝），并标记为共享
        captured.Scopes.Clear(); // 清除构造函数创建的初始作用域
        captured.Scopes.AddRange(Scopes); // 直接添加对原始作用域的引用

        // 标记原管理器的作用域为共享状态
        MarkScopesAsShared();

        // 同时标记新管理器的作用域为共享状态
        captured.ScopeSharedFlags.Clear();
        captured.ScopeSharedFlags.AddRange(ScopeSharedFlags);

        // 复制导入信息（线程安全）
        lock (ImportInfosLock)
        {
            lock (captured.ImportInfosLock)
            {
                captured.ImportInfosList.AddRange(ImportInfosList);
            }
        }

        return captured;
    }

    /// <summary>
    /// 为生成器创建独立的变量管理器（深拷贝）
    /// </summary>
    /// <returns>包含独立作用域副本和生成器上下文的新VariateManager</returns>
    /// <remarks>
    /// 与闭包的浅拷贝不同，生成器需要独立的变量副本以避免多个生成器实例互相干扰
    /// 每个生成器实例都有自己的：
    /// - 独立的作用域栈副本（深拷贝）
    /// - 独立的GeneratorExecutionContext
    /// - 共享的导入信息（函数、类定义等）
    /// </remarks>
    public VariateManager CloneForGenerator()
    {
        var generatorManager = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = Interpreter,
            // 创建生成器专用的执行上下文
            GeneratorContext = new GeneratorExecutionContext()
        };

        // 深拷贝作用域栈（生成器需要独立副本）
        generatorManager.Scopes.Clear(); // 清除构造函数创建的初始作用域
        foreach (var newScope in Scopes.Select(scope => new Dictionary<string, LangValueType>(scope)))
        {
            generatorManager.Scopes.Add(newScope);
        }

        // 复制导入信息（线程安全）
        lock (ImportInfosLock)
        {
            lock (generatorManager.ImportInfosLock)
            {
                generatorManager.ImportInfosList.AddRange(ImportInfosList);
            }
        }

        return generatorManager;
    }
}