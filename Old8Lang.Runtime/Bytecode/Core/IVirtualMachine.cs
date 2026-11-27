using Old8Lang.Bytecode.Metadata;

namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 虚拟机在共享运行时中暴露的最小契约。
/// </summary>
/// <remarks>
/// 运行时值、实例方法与反射辅助需要在 VM 模式下访问当前虚拟机；具体
/// <c>VirtualMachine</c> 实现位于 <c>Old8Lang.VirtualMachine</c>，Runtime 只依赖本接口。
/// </remarks>
public interface IVirtualMachine
{
    /// <summary>读取全局变量。</summary>
    object? GetGlobalVariable(string name);

    /// <summary>枚举全部全局变量。</summary>
    IEnumerable<KeyValuePair<string, object?>> GetAllGlobalVariables();

    /// <summary>读取常量池项。</summary>
    object? GetConstant(int index);

    /// <summary>调用函数值对象。</summary>
    object? CallFunctionObject(object? funcObj, object?[] arguments);

    /// <summary>在线程工作项中调用函数值对象。</summary>
    object? ExecuteFunctionObjectInWorker(object? funcObj, object?[] arguments);

    /// <summary>
    /// 在启动工作线程或并发任务前开启全局槽位并发保护（防止跨线程读写类型化值撕裂）。
    /// </summary>
    void EnableConcurrentGlobals() { }

    /// <summary>按函数元数据执行函数并返回结果。</summary>
    object? ExecuteFunctionAndGetResult(FunctionMetadata function, object?[] args);
}
