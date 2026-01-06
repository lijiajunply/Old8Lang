using System;
using System.Collections.Generic;
using Old8Lang.AST.Statement;

namespace Old8Lang.ExternProviders;

/// <summary>
/// Extern 提供者工厂
/// 根据 ExternType 创建对应的语言提供者
/// </summary>
public static class ExternProviderFactory
{
    /// <summary>
    /// 提供者注册表（支持运行时注册新的语言提供者）
    /// </summary>
    private static readonly Dictionary<ExternType, Func<IExternProvider>> ProviderRegistry = new()
    {
        { ExternType.NativeDll, () => new NativeDllProvider() },
        { ExternType.PythonScript, () => new PythonProvider(ExternType.PythonScript) },
        { ExternType.PythonModule, () => new PythonProvider(ExternType.PythonModule) },
        { ExternType.JavaScript, () => new JavaScriptProvider() }
    };

    /// <summary>
    /// 创建对应的 Extern 提供者
    /// </summary>
    /// <param name="externType">Extern 类型</param>
    /// <returns>对应的语言提供者实例</returns>
    /// <exception cref="NotSupportedException">不支持的 Extern 类型</exception>
    public static IExternProvider CreateProvider(ExternType externType)
    {
        if (ProviderRegistry.TryGetValue(externType, out var factory))
        {
            return factory();
        }

        throw new NotSupportedException($"不支持的 Extern 类型: {externType}");
    }

    /// <summary>
    /// 注册新的 Extern 提供者
    /// </summary>
    /// <param name="externType">Extern 类型</param>
    /// <param name="factory">提供者工厂函数</param>
    /// <remarks>
    /// 允许用户在运行时注册自定义的语言提供者
    /// 例如：JavaScript、Ruby、Lua 等
    /// </remarks>
    public static void RegisterProvider(ExternType externType, Func<IExternProvider> factory)
    {
        ProviderRegistry[externType] = factory;
    }

    /// <summary>
    /// 检查是否支持指定的 Extern 类型
    /// </summary>
    /// <param name="externType">Extern 类型</param>
    /// <returns>是否支持</returns>
    public static bool IsSupported(ExternType externType)
    {
        return ProviderRegistry.ContainsKey(externType);
    }

    /// <summary>
    /// 获取所有已注册的 Extern 类型
    /// </summary>
    /// <returns>已注册的类型列表</returns>
    public static IEnumerable<ExternType> GetSupportedTypes()
    {
        return ProviderRegistry.Keys;
    }
}