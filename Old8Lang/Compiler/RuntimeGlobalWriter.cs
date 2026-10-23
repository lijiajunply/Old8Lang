using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;

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

    /// <summary>
    /// 设置当前线程用于接收顶层变量的管理器。
    /// </summary>
    public static void SetManager(VariateManager manager) => _manager = manager;

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
        var langValue = value as LangValueType ?? LangValueType.ObjToValue(value);
        _manager.Set(new LangId(name), langValue);
    }
}
