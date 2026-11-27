using System.Runtime.CompilerServices;
using Old8Lang.Bytecode.Closures;

namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 虚拟机类型化值的种类标记。
/// </summary>
/// <remarks>
/// 数值排列保证：<see cref="Null"/> 为 0（<c>default(VmValue)</c> 即 null）；
/// 小于 <see cref="Object"/> 的种类均不持有托管引用（<c>ObjectRef</c> 恒为 null），
/// 便于弹栈与清池时跳过无意义的引用写屏障。
/// </remarks>
public enum VmValueKind : byte
{
    Null = 0,
    Int = 1,
    Long = 2,
    Double = 3,
    Bool = 4,
    Char = 5,
    Object = 6,
    /// <summary>
    /// 仅存于 <see cref="CallFrame.Locals"/> 的闭包共享单元引用；不得直接暴露到求值栈。
    /// </summary>
    UpValue = 7
}

/// <summary>
/// 虚拟机类型化值表示（标记联合值类型），用于消除基础类型在求值栈、局部变量、全局槽位与闭包单元上的装箱分配。
/// </summary>
public readonly struct VmValue
{
    private const int CachedIntMin = -128;
    private const int CachedIntMax = 4095;
    private static readonly object[] CachedInts = CreateCachedInts();
    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;

    private static object[] CreateCachedInts()
    {
        var table = new object[CachedIntMax - CachedIntMin + 1];
        for (int i = 0; i < table.Length; i++)
        {
            table[i] = CachedIntMin + i;
        }

        return table;
    }

    /// <summary>值类型标记</summary>
    public readonly VmValueKind Kind;

    /// <summary>基础类型内联载荷（Int/Long/Double 位模式/Bool/Char）</summary>
    internal readonly long RawBits;

    /// <summary>引用类型载荷（仅在 <see cref="Kind"/> 为 <see cref="VmValueKind.Object"/> 或 <see cref="VmValueKind.UpValue"/> 时非 null）</summary>
    internal readonly object? ObjectRef;

    /// <summary>空值单例</summary>
    public static readonly VmValue Null = default;

    /// <summary>布尔真单例</summary>
    public static readonly VmValue True = new(VmValueKind.Bool, 1L, null);

    /// <summary>布尔假单例</summary>
    public static readonly VmValue False = new(VmValueKind.Bool, 0L, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue(VmValueKind kind, long rawBits, object? objectRef)
    {
        Kind = kind;
        RawBits = rawBits;
        ObjectRef = objectRef;
    }

    /// <summary>是否为 null</summary>
    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.Null;
    }

    /// <summary>是否为 32 位整数</summary>
    public bool IsInt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.Int;
    }

    /// <summary>是否为 64 位整数</summary>
    public bool IsLong
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.Long;
    }

    /// <summary>是否为双精度浮点数</summary>
    public bool IsDouble
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.Double;
    }

    /// <summary>是否为布尔值</summary>
    public bool IsBool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.Bool;
    }

    /// <summary>是否为字符</summary>
    public bool IsChar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.Char;
    }

    /// <summary>是否为普通托管对象引用</summary>
    public bool IsObject
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.Object;
    }

    /// <summary>是否为闭包共享单元槽位</summary>
    public bool IsUpValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == VmValueKind.UpValue;
    }

    /// <summary>是否持有非空托管对象引用（弹栈清理 GC 根时用于快速跳过基础类型）</summary>
    public bool HasReference
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind >= VmValueKind.Object;
    }

    /// <summary>按 32 位整数读取内联载荷</summary>
    public int AsInt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => unchecked((int)RawBits);
    }

    /// <summary>按 64 位整数读取内联载荷</summary>
    public long AsLong
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => RawBits;
    }

    /// <summary>按双精度浮点数读取内联载荷</summary>
    public double AsDouble
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.Int64BitsToDouble(RawBits);
    }

    /// <summary>按布尔值读取内联载荷</summary>
    public bool AsBool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => RawBits != 0;
    }

    /// <summary>按字符读取内联载荷</summary>
    public char AsChar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (char)RawBits;
    }

    /// <summary>读取托管引用载荷</summary>
    public object? AsObject
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ObjectRef;
    }

    /// <summary>读取闭包共享单元引用</summary>
    internal UpValueCell AsUpValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.As<UpValueCell>(ObjectRef!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VmValue FromInt(int value) => new(VmValueKind.Int, value, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VmValue FromLong(long value) => new(VmValueKind.Long, value, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VmValue FromDouble(double value) => new(VmValueKind.Double, BitConverter.DoubleToInt64Bits(value), null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VmValue FromBool(bool value) => value ? True : False;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VmValue FromChar(char value) => new(VmValueKind.Char, value, null);

    /// <summary>
    /// 从已知非基础类型的托管对象构造 <see cref="VmValue"/>（避免多余的基础类型检查）。
    /// 若传入 null，则返回 <see cref="Null"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VmValue FromReference(object? value) =>
        value is null ? Null : new VmValue(VmValueKind.Object, 0L, value);

    /// <summary>构造闭包共享单元槽位值（仅限 <see cref="CallFrame.Locals"/> 使用）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static VmValue FromUpValue(UpValueCell cell) => new(VmValueKind.UpValue, 0L, cell);

    /// <summary>
    /// 从任意 CLR <see cref="object"/> 转换为 <see cref="VmValue"/>（外部/原生互操作边界入口）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VmValue FromObject(object? value)
    {
        return value switch
        {
            null => Null,
            int i => FromInt(i),
            string s => new VmValue(VmValueKind.Object, 0L, s),
            double d => FromDouble(d),
            bool b => FromBool(b),
            long l => FromLong(l),
            char c => FromChar(c),
            _ => new VmValue(VmValueKind.Object, 0L, value)
        };
    }

    /// <summary>
    /// 转换为 CLR <see cref="object"/>（外部/原生互操作与容器边界出口）。
    /// 布尔值与常用小整数（<c>-128..4095</c>）复用共享静态装箱实例，避免边界处短命装箱分配。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? ToObject()
    {
        return Kind switch
        {
            VmValueKind.Null => null,
            VmValueKind.Object => ObjectRef,
            VmValueKind.Int => BoxInt(unchecked((int)RawBits)),
            VmValueKind.Double => BitConverter.Int64BitsToDouble(RawBits),
            VmValueKind.Bool => RawBits != 0 ? BoxedTrue : BoxedFalse,
            VmValueKind.Long => RawBits,
            VmValueKind.Char => (char)RawBits,
            VmValueKind.UpValue => Unsafe.As<UpValueCell>(ObjectRef!).Value.ToObject(),
            _ => ObjectRef
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object BoxInt(int value)
    {
        uint offset = unchecked((uint)(value - CachedIntMin));
        if (offset < (uint)CachedInts.Length)
        {
            return CachedInts[offset];
        }

        return value;
    }

    /// <summary>
    /// 快速转为 32 位整数（用于容器下标、资源 ID 等路径，避免先装箱再 <see cref="Convert.ToInt32(object?)"/>）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ToInt32()
    {
        return Kind switch
        {
            VmValueKind.Int => unchecked((int)RawBits),
            VmValueKind.Long => checked((int)RawBits),
            VmValueKind.Double => Convert.ToInt32(BitConverter.Int64BitsToDouble(RawBits)),
            VmValueKind.Bool => RawBits != 0 ? 1 : 0,
            VmValueKind.Char => (int)RawBits,
            VmValueKind.Null => 0,
            _ => Convert.ToInt32(ObjectRef)
        };
    }

    /// <summary>
    /// 快速转为双精度浮点数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToDoubleValue()
    {
        return Kind switch
        {
            VmValueKind.Double => BitConverter.Int64BitsToDouble(RawBits),
            VmValueKind.Int => unchecked((int)RawBits),
            VmValueKind.Long => RawBits,
            _ => Convert.ToDouble(ToObject())
        };
    }

    public override string ToString()
    {
        return Kind switch
        {
            VmValueKind.Null => "null",
            VmValueKind.Int => AsInt.ToString(),
            VmValueKind.Long => AsLong.ToString(),
            VmValueKind.Double => AsDouble.ToString(),
            VmValueKind.Bool => AsBool ? "true" : "false",
            VmValueKind.Char => AsChar.ToString(),
            VmValueKind.Object => ObjectRef?.ToString() ?? "null",
            VmValueKind.UpValue => AsUpValue.Value.ToString(),
            _ => string.Empty
        };
    }
}
