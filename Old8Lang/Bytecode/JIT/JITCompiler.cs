using System.Reflection.Emit;

namespace Old8Lang.Bytecode.JIT;

/// <summary>
/// JIT即时编译器
/// </summary>
public class JITCompiler
{
    /// <summary>触发JIT编译的调用次数阈值</summary>
    private const int JIT_THRESHOLD = 100;

    /// <summary>函数调用计数器</summary>
    private readonly Dictionary<string, int> _callCounts = new();

    /// <summary>已编译的函数缓存</summary>
    private readonly Dictionary<string, Delegate> _compiledFunctions = new();

    /// <summary>是否启用JIT编译</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 记录函数调用
    /// </summary>
    public void RecordCall(string functionName)
    {
        if (!Enabled) return;

        _callCounts.TryAdd(functionName, 0);

        _callCounts[functionName]++;
    }

    /// <summary>
    /// 检查是否应该触发JIT编译
    /// </summary>
    public bool ShouldCompile(string functionName)
    {
        if (!Enabled) return false;
        if (_compiledFunctions.ContainsKey(functionName)) return false;

        return _callCounts.TryGetValue(functionName, out var count) && count >= JIT_THRESHOLD;
    }

    /// <summary>
    /// 获取已编译的函数
    /// </summary>
    public Delegate? GetCompiledFunction(string functionName)
    {
        return _compiledFunctions.TryGetValue(functionName, out var func) ? func : null;
    }

    /// <summary>
    /// 编译函数到IL
    /// </summary>
    public bool TryCompileFunction(FunctionMetadata function, ConstantPool constantPool)
    {
        if (!Enabled) return false;
        if (_compiledFunctions.ContainsKey(function.Name)) return true;

        try
        {
            // 创建动态方法
            var dynamicMethod = new DynamicMethod(
                function.Name,
                typeof(object),
                new[] { typeof(object[]) },
                typeof(JITCompiler).Module);

            var il = dynamicMethod.GetILGenerator();

            // TODO: 生成IL代码
            // 目前返回null作为占位
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            // 创建委托并缓存
            var compiled = dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
            _compiledFunctions[function.Name] = compiled;

            return true;
        }
        catch
        {
            return false;
        }
    }
}