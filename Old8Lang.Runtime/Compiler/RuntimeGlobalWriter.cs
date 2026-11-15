using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using System.Collections;

namespace Old8Lang.Compiler;

/// <summary>
/// IL 模式顶层变量回写。
/// 编译产物执行结束后，把顶层局部变量的最终值写回解释器的 <see cref="VariateManager"/>，
/// 使宿主（REPL、测试、调试器）能像解释模式一样读取程序结果。
/// </summary>
/// <remarks>
/// <para>IL 模式的顶层变量是 <c>OldLangRun</c> 的局部变量（<c>LocalManager</c>），
/// 而函数体是独立的动态方法，二者都不能直接访问解释器作用域。</para>
/// <para>这里用 <see cref="ThreadStaticAttribute"/> 保存当前执行线程上的管理器：
/// 包装后的 <c>Action</c> 在执行前设置它，生成的 IL 在返回前调用 <see cref="Write"/>。
/// 之所以不用静态字段，是为了让并行测试（每个测试线程各跑一个编译产物）互不干扰。</para>
/// </remarks>
public static class RuntimeGlobalWriter
{
    [ThreadStatic]
    private static VariateManager? _manager;
    [ThreadStatic]
    private static Dictionary<object, LangValueType>? _convertedCollections;

    /// <summary>
    /// 当前线程正在执行的解释器管理器；IL 产物里的反射类内置函数需要它。
    /// </summary>
    public static VariateManager? CurrentManager => _manager;

    /// <summary>
    /// 设置当前线程用于接收顶层变量的管理器。
    /// </summary>
    public static void SetManager(VariateManager manager)
    {
        _manager = manager;
        _convertedCollections = new Dictionary<object, LangValueType>(ReferenceEqualityComparer.Instance);
    }

    /// <summary>
    /// 捕获当前回写目标，供 Spawn 创建的 CLR 线程在完成时刷新共享全局变量。
    /// </summary>
    public static object? CaptureContext() => _manager;

    /// <summary>
    /// 在当前 CLR 线程安装回写目标，并返回该线程原有的目标。
    /// </summary>
    public static object? InstallContext(object? context)
    {
        var previous = _manager;
        _manager = context as VariateManager;
        _convertedCollections = _manager is null
            ? null
            : new Dictionary<object, LangValueType>(ReferenceEqualityComparer.Instance);
        return previous;
    }

    /// <summary>
    /// 把一个顶层变量的值写回管理器。
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="value">已装箱的运行时值（可能是原生 int/string/...，也可能已是 LangValueType）</param>
    public static void Write(string name, object? value)
    {
        if (_manager is null)
        {
            return;
        }

        // 已经是语言值的必须原样保留：ObjToValue 对 LangValueType 没有专门分支，
        // 会把它包成 NativeAnyLangValue，导致宿主侧 (IntLangValue) 强转失败。
        var langValue = ConvertValue(value);
        _manager.Set(new LangId(name), langValue);
    }

    private static LangValueType ConvertValue(object? value)
    {
        if (value is LangValueType languageValue)
            return languageValue;
        if (value is null)
            return LangValueType.ObjToValue(null);

        var converted = _convertedCollections!;
        if (converted.TryGetValue(value, out var existing))
            return existing;

        if (value is Array { Rank: 1 } array && array.GetLowerBound(0) == 0)
        {
            var result = new ArrayLangValue(new LangValueType[array.Length]);
            converted.Add(value, result);
            for (var i = 0; i < array.Length; i++)
                result.RunResult[i] = ConvertValue(array.GetValue(i));
            return result;
        }

        if (value is IDictionary dictionary)
        {
            var result = new DictionaryLangValue();
            converted.Add(value, result);
            foreach (DictionaryEntry entry in dictionary)
                result.Set(ConvertValue(entry.Key), ConvertValue(entry.Value));
            return result;
        }

        if (value is IList list)
        {
            var result = new ListLangValue(new List<LangValueType>());
            converted.Add(value, result);
            foreach (var item in list)
                result.Values.Add(ConvertValue(item));
            return result;
        }

        return LangValueType.ObjToValue(value);
    }
}
