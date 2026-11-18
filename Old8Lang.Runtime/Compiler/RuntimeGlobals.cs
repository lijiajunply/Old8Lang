using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.Error;

namespace Old8Lang.Compiler;

/// <summary>
/// IL 模式运行时全局变量环境。
/// </summary>
/// <remarks>
/// <para>顶层变量默认是 <c>OldLangRun</c> 的局部变量，但具名函数（以及 lambda、类方法）
/// 会被编译成独立的 <see cref="DynamicMethod"/>，无法直接读取这些 <c>LocalBuilder</c>。
/// 当分析发现某个顶层变量会被函数体访问时，它会被提升为「全局槽位」：顶层代码和函数体
/// 都通过本类读写同一份运行期存储，而不是把 <c>LocalBuilder</c> 跨动态方法复用。</para>
/// <para>与 <see cref="RuntimeGlobalWriter"/> 一样使用 <see cref="ThreadStaticAttribute"/>，
/// 使并行执行的编译产物（例如并行测试）互不干扰。槽位在编译产物每次执行前重置，
/// 未赋值的槽位读取时按「变量未声明」处理。</para>
/// </remarks>
public static class RuntimeGlobals
{
    private sealed class Slot
    {
        public object? Value;
        public bool HasValue;
    }

    [ThreadStatic]
    private static Dictionary<string, Slot>? _slots;

    /// <summary>
    /// 捕获当前执行所使用的全局槽位，使 Spawn 创建的 CLR 线程能够访问同一份语言全局变量。
    /// </summary>
    public static object? CaptureContext() => _slots;

    /// <summary>
    /// 在当前 CLR 线程安装先前捕获的全局槽位，并返回该线程原有的上下文。
    /// </summary>
    public static object? InstallContext(object? context)
    {
        var previous = _slots;
        _slots = context as Dictionary<string, Slot>;
        return previous;
    }

    private static readonly System.Reflection.MethodInfo GetMethod =
        typeof(RuntimeGlobals).GetMethod(nameof(Get))!;

    private static readonly System.Reflection.MethodInfo SetMethod =
        typeof(RuntimeGlobals).GetMethod(nameof(Set))!;

    /// <summary>
    /// 为一次执行准备全局槽位。每次执行编译产物前调用，清空上一次的值。
    /// </summary>
    /// <param name="slotTypes">需要创建的槽位（名称 → 静态类型）</param>
    public static void Initialize(IReadOnlyDictionary<string, Type> slotTypes)
    {
        var slots = new Dictionary<string, Slot>(StringComparer.Ordinal);
        foreach (var name in slotTypes.Keys)
        {
            slots[name] = new Slot();
        }

        _slots = slots;
    }

    /// <summary>
    /// 读取一个全局变量的值；尚未赋值时抛出名称错误。
    /// </summary>
    public static object? Get(string name)
    {
        if (_slots is not null && _slots.TryGetValue(name, out var slot) && slot.HasValue)
        {
            return slot.Value;
        }

        throw new Old8Exception(
            "NAME_ERROR",
            $"变量 '{name}' 未声明",
            new SourcePosition(0, 0));
    }

    /// <summary>
    /// 写入一个全局变量的值。
    /// </summary>
    public static void Set(string name, object? value)
    {
        _slots ??= new Dictionary<string, Slot>(StringComparer.Ordinal);
        if (!_slots.TryGetValue(name, out var slot))
        {
            slot = new Slot();
            _slots[name] = slot;
        }

        slot.Value = value;
        slot.HasValue = true;
    }

    /// <summary>
    /// 当前线程已赋值的全部全局槽位快照（名称 → 原生值）。
    /// 供「IL 装饰器转发到解释器」等需要在两种表示之间同步的场景使用。
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Snapshot()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (_slots is null)
        {
            return result;
        }

        foreach (var (name, slot) in _slots)
        {
            if (slot.HasValue)
            {
                result[name] = slot.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// 尝试读取一个全局变量，未初始化的槽位返回 false。
    /// </summary>
    public static bool TryGet(string name, out object? value)
    {
        if (_slots is not null && _slots.TryGetValue(name, out var slot) && slot.HasValue)
        {
            value = slot.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// 把槽位中已赋值的全局变量写回解释器管理器（用于编译产物执行结束后的回写）。
    /// </summary>
    public static void WriteBack(string name)
    {
        if (TryGet(name, out var value))
        {
            RuntimeGlobalWriter.Write(name, value);
        }
    }

    /// <summary>
    /// 把当前执行上下文中的全部全局槽位写回宿主变量管理器。
    /// Spawn 的子线程完成时调用，避免顶层方法先结束而留下过期的集合快照。
    /// </summary>
    public static void WriteBackAll()
    {
        if (_slots is null)
        {
            return;
        }

        foreach (var name in _slots.Keys)
        {
            WriteBack(name);
        }
    }

    /// <summary>
    /// 生成「读取全局变量」的 IL：返回装箱的 object，再按槽位静态类型拆箱/转换。
    /// </summary>
    public static void EmitLoad(ILGenerator ilGenerator, string name, Type slotType)
    {
        ilGenerator.Emit(OpCodes.Ldstr, name);
        ilGenerator.Emit(OpCodes.Call, GetMethod);

        if (slotType == typeof(object) || slotType == typeof(void))
        {
            return;
        }

        ilGenerator.Emit(slotType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, slotType);
    }

    /// <summary>
    /// 生成「写入全局变量」的 IL。栈顶是已计算好的值（静态类型为 <paramref name="valueType"/>）。
    /// </summary>
    public static void EmitStore(ILGenerator ilGenerator, string name, Type slotType, Type valueType)
    {
        if (slotType == typeof(void))
        {
            slotType = typeof(object);
        }

        // 把值转换成槽位的静态类型，再装箱交给运行期存储。
        if (valueType != slotType)
        {
            if (valueType == typeof(object) && slotType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, slotType);
                valueType = slotType;
            }
            else if (slotType == typeof(double) && valueType == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Conv_R8);
                valueType = slotType;
            }
        }

        if (slotType.IsValueType)
        {
            if (valueType != slotType && !valueType.IsValueType)
            {
                // 理论上前面已处理 object → 值类型；这里保证栈上类型与 Box 指令一致。
                ilGenerator.Emit(OpCodes.Unbox_Any, slotType);
            }
            else if (valueType != slotType)
            {
                // 两个不同的值类型没有隐式转换：按实际类型装箱后拆成槽位类型，
                // 无法转换时由运行期抛出类型错误，而不是生成无效 IL。
                ilGenerator.Emit(OpCodes.Box, valueType);
                ilGenerator.Emit(OpCodes.Unbox_Any, slotType);
            }

            ilGenerator.Emit(OpCodes.Box, slotType);
        }
        else if (valueType.IsValueType)
        {
            // 槽位是引用类型（含 object），把值类型装箱。
            ilGenerator.Emit(OpCodes.Box, valueType);
        }

        // Set(string, object) 需要把名称压在值下面，因此先把值存进临时槽。
        var valueLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, valueLocal);
        ilGenerator.Emit(OpCodes.Ldstr, name);
        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
        ilGenerator.Emit(OpCodes.Call, SetMethod);
    }
}
