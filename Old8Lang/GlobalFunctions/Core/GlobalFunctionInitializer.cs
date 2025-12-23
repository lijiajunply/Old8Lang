using Old8Lang.GlobalFunctions.Implementations;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 全局函数初始化器 - 负责注册所有内置的全局函数
/// </summary>
public static class GlobalFunctionInitializer
{
    private static bool _initialized = false;
    private static readonly object _initLock = new();

    /// <summary>
    /// 初始化并注册所有内置全局函数
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            var registry = GlobalFunctionRegistry.Instance;

            // 注册 IO 函数
            registry.Register(new PrintLineFunction());
            registry.Register(new PrintFunction());
            registry.Register(new ReadLineFunction());
            registry.Register(new ErrorFunction());
            registry.Register(new ClearFunction());

            // 注册工具函数
            registry.Register(new LenFunction());
            registry.Register(new TypeFunction());
            registry.Register(new AssertFunction());
            registry.Register(new ShowValuesFunction());

            _initialized = true;
        }
    }

    /// <summary>
    /// 确保全局函数已初始化（延迟初始化）
    /// </summary>
    public static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// 重置初始化状态（主要用于测试）
    /// </summary>
    public static void Reset()
    {
        lock (_initLock)
        {
            GlobalFunctionRegistry.Instance.Clear();
            _initialized = false;
        }
    }
}
