using Old8Lang.Bytecode.Core;

namespace Old8Lang.Bytecode.Metadata;

/// <summary>
/// 函数元数据
/// </summary>
public class FunctionMetadata
{
    public enum FastParameterTypeKind : byte
    {
        None = 0,
        Int = 1,
        Double = 2,
        String = 3,
        Bool = 4,
        Char = 5,
        Any = 6,
        Object = 7,
        Other = 255
    }

    private Dictionary<string, int>? _parameterIndexMap;
    private int _parameterIndexMapCount = -1;
    private FastParameterTypeKind[]? _fastParameterKinds;
    private int _fastParameterKindsCount = -1;
    private bool _fastParameterKindsContainUnsupportedType;
    private Dictionary<int, ExceptionDispatchCandidate[]>? _exceptionDispatchCandidatesByIp;
    private int _exceptionDispatchCacheTableCount = -1;

    /// <summary>函数名称</summary>
    public string Name { get; set; } = "";

    /// <summary>参数名称列表</summary>
    public List<string> Parameters { get; set; } = [];

    /// <summary>参数类型列表（索引对应Parameters，空字符串表示无类型注解）</summary>
    public List<string> ParameterTypes { get; set; } = [];

    /// <summary>参数默认值列表（索引对应Parameters，null表示无默认值）</summary>
    public List<object?> DefaultValues { get; set; } = [];

    /// <summary>返回类型（空字符串表示无类型注解）</summary>
    public string ReturnType { get; set; } = "";

    /// <summary>params参数的索引（-1表示没有params参数）</summary>
    public int ParamsParameterIndex { get; set; } = -1;

    /// <summary>字节码指令列表</summary>
    public List<Instruction> Instructions { get; set; } = [];

    /// <summary>局部变量数量</summary>
    public int LocalCount { get; set; }

    /// <summary>最大栈深度</summary>
    public int MaxStackSize { get; set; }

    /// <summary>是否是异步函数</summary>
    public bool IsAsync { get; set; }

    /// <summary>是否是生成器函数</summary>
    public bool IsGenerator { get; set; }

    /// <summary>是否是扩展方法</summary>
    public bool IsExtensionMethod { get; set; }

    /// <summary>是否是带装饰器的函数</summary>
    /// <remarks>
    /// 装饰器在运行期把包装后的函数写回同名全局变量，因此这类函数的“真身”
    /// 是全局绑定而不是这里的函数体。调用点若按函数索引直接跳到函数体，
    /// 就会静默绕过装饰器，所以解析调用目标时要避开索引捷径、走全局绑定。
    /// </remarks>
    public bool IsDecorated { get; set; }

    /// <summary>函数在常量池中的索引(用于闭包)</summary>
    public int FunctionIndex { get; set; } = -1;

    /// <summary>捕获的变量列表(用于闭包)</summary>
    public List<string> CapturedVariables { get; set; } = [];

    /// <summary>异常表 - 记录try-catch-finally块的位置信息</summary>
    public List<ExceptionTableEntry> ExceptionTable { get; set; } = [];

    /// <summary>泛型类型参数映射（用于泛型函数实例化）</summary>
    /// <remarks>例如: getValue&lt;int?> 时为 {"T": "int"}</remarks>
    public Dictionary<string, string>? GenericTypeMapping { get; set; }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Name);

        // 参数
        writer.Write(Parameters.Count);
        foreach (var param in Parameters)
            writer.Write(param);

        // 参数类型
        writer.Write(ParameterTypes.Count);
        foreach (var paramType in ParameterTypes)
            writer.Write(paramType);

        // 默认参数值
        writer.Write(DefaultValues.Count);
        foreach (var defaultValue in DefaultValues)
        {
            if (defaultValue == null)
            {
                writer.Write((byte)0); // null标记
            }
            else
            {
                writer.Write((byte)1); // 非null标记
                // 序列化默认值（支持基本类型）
                WriteDefaultValue(writer, defaultValue);
            }
        }

        // 返回类型
        writer.Write(ReturnType);

        // 指令
        writer.Write(Instructions.Count);
        foreach (var instruction in Instructions)
            instruction.WriteTo(writer);

        // 元数据
        writer.Write(LocalCount);
        writer.Write(MaxStackSize);
        writer.Write(IsAsync);
        writer.Write(IsGenerator);
        writer.Write(IsExtensionMethod);
        writer.Write(IsDecorated);
        writer.Write(FunctionIndex);
        writer.Write(ParamsParameterIndex);

        // 异常表
        writer.Write(ExceptionTable.Count);
        foreach (var entry in ExceptionTable)
            entry.WriteTo(writer);

        // 泛型类型映射
        if (GenericTypeMapping == null)
        {
            writer.Write(0);
        }
        else
        {
            writer.Write(GenericTypeMapping.Count);
            foreach (var kvp in GenericTypeMapping)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static FunctionMetadata ReadFrom(BinaryReader reader)
    {
        var func = new FunctionMetadata
        {
            Name = reader.ReadString()
        };

        // 参数
        int paramCount = reader.ReadInt32();
        for (int i = 0; i < paramCount; i++)
            func.Parameters.Add(reader.ReadString());

        // 参数类型
        int paramTypeCount = reader.ReadInt32();
        for (int i = 0; i < paramTypeCount; i++)
            func.ParameterTypes.Add(reader.ReadString());

        // 默认参数值
        int defaultValueCount = reader.ReadInt32();
        for (int i = 0; i < defaultValueCount; i++)
        {
            byte nullMarker = reader.ReadByte();
            func.DefaultValues.Add(nullMarker == 0 ? null : ReadDefaultValue(reader));
        }

        // 返回类型
        func.ReturnType = reader.ReadString();

        // 指令
        int instCount = reader.ReadInt32();
        for (int i = 0; i < instCount; i++)
            func.Instructions.Add(Instruction.ReadFrom(reader));

        // 元数据
        func.LocalCount = reader.ReadInt32();
        func.MaxStackSize = reader.ReadInt32();
        func.IsAsync = reader.ReadBoolean();
        func.IsGenerator = reader.ReadBoolean();
        func.IsExtensionMethod = reader.ReadBoolean();
        func.IsDecorated = reader.ReadBoolean();
        func.FunctionIndex = reader.ReadInt32();
        func.ParamsParameterIndex = reader.ReadInt32();

        // 异常表
        int exceptionTableCount = reader.ReadInt32();
        for (int i = 0; i < exceptionTableCount; i++)
            func.ExceptionTable.Add(ExceptionTableEntry.ReadFrom(reader));

        // 泛型类型映射
        int genericTypeMappingCount = reader.ReadInt32();
        if (genericTypeMappingCount > 0)
        {
            func.GenericTypeMapping = new Dictionary<string, string>();
            for (int i = 0; i < genericTypeMappingCount; i++)
            {
                string key = reader.ReadString();
                string value = reader.ReadString();
                func.GenericTypeMapping[key] = value;
            }
        }

        return func;
    }

    public override string ToString()
    {
        return $"Function {Name}({string.Join(", ", Parameters)}) [{Instructions.Count} instructions]";
    }

    public readonly struct ExceptionDispatchCandidate
    {
        public ExceptionDispatchCandidate(ExceptionTableEntry entry, bool inTryBlock, bool inCatchBlock)
        {
            Entry = entry;
            InTryBlock = inTryBlock;
            InCatchBlock = inCatchBlock;
        }

        public ExceptionTableEntry Entry { get; }
        public bool InTryBlock { get; }
        public bool InCatchBlock { get; }
    }

    /// <summary>
    /// 获取异常指令位置对应的候选处理器（热路径缓存，保持异常表原有顺序）。
    /// </summary>
    public ExceptionDispatchCandidate[] GetExceptionDispatchCandidates(int instructionPointer)
    {
        EnsureExceptionDispatchCache();

        if (_exceptionDispatchCandidatesByIp!.TryGetValue(instructionPointer, out var cached))
        {
            return cached;
        }

        if (ExceptionTable.Count == 0)
        {
            cached = [];
            _exceptionDispatchCandidatesByIp[instructionPointer] = cached;
            return cached;
        }

        var candidates = new List<ExceptionDispatchCandidate>(ExceptionTable.Count);
        foreach (var entry in ExceptionTable)
        {
            var inTry = entry.IsInTryBlock(instructionPointer);
            var inCatch = entry.IsInCatchBlock(instructionPointer);
            if (inTry || inCatch)
            {
                candidates.Add(new ExceptionDispatchCandidate(entry, inTry, inCatch));
            }
        }

        cached = candidates.Count == 0 ? [] : candidates.ToArray();
        _exceptionDispatchCandidatesByIp[instructionPointer] = cached;
        return cached;
    }

    /// <summary>
    /// 尝试获取参数名对应的索引（命名参数绑定热路径缓存）
    /// </summary>
    public bool TryGetParameterIndex(string parameterName, out int index)
    {
        EnsureParameterIndexMap();
        return _parameterIndexMap!.TryGetValue(parameterName, out index);
    }

    private void EnsureParameterIndexMap()
    {
        if (_parameterIndexMap is not null && _parameterIndexMapCount == Parameters.Count)
        {
            return;
        }

        var map = new Dictionary<string, int>(Parameters.Count, StringComparer.Ordinal);
        for (var i = 0; i < Parameters.Count; i++)
        {
            var name = Parameters[i];
            if (!map.ContainsKey(name))
            {
                map[name] = i;
            }
        }

        _parameterIndexMap = map;
        _parameterIndexMapCount = Parameters.Count;
    }

    /// <summary>
    /// 获取参数类型的快速分类（用于 VM 热路径类型校验）
    /// </summary>
    public FastParameterTypeKind GetFastParameterTypeKind(int index)
    {
        EnsureFastParameterKinds();
        if (_fastParameterKinds is null || index < 0 || index >= _fastParameterKinds.Length)
        {
            return FastParameterTypeKind.Other;
        }

        return _fastParameterKinds[index];
    }

    /// <summary>
    /// 尝试获取“纯位置参数调用”热路径所需的参数类型分类缓存。
    /// </summary>
    public bool TryGetPositionalFastCallTypeKinds(int argCount, out FastParameterTypeKind[] typeKinds)
    {
        typeKinds = Array.Empty<FastParameterTypeKind>();

        if (argCount != Parameters.Count || ParamsParameterIndex >= 0 || IsGenerator || GenericTypeMapping is { Count: > 0 })
        {
            return false;
        }

        EnsureFastParameterKinds();
        if (_fastParameterKinds is null || _fastParameterKinds.Length < argCount)
        {
            return false;
        }

        if (_fastParameterKindsContainUnsupportedType)
        {
            return false;
        }

        typeKinds = _fastParameterKinds;
        return true;
    }

    private void EnsureFastParameterKinds()
    {
        if (_fastParameterKinds is not null && _fastParameterKindsCount == ParameterTypes.Count)
        {
            return;
        }

        var kinds = new FastParameterTypeKind[ParameterTypes.Count];
        var hasUnsupportedType = false;
        for (var i = 0; i < ParameterTypes.Count; i++)
        {
            var typeName = ParameterTypes[i];
            if (string.IsNullOrWhiteSpace(typeName))
            {
                kinds[i] = FastParameterTypeKind.None;
                continue;
            }

            kinds[i] = typeName switch
            {
                _ when typeName.Equals("int", StringComparison.OrdinalIgnoreCase) => FastParameterTypeKind.Int,
                _ when typeName.Equals("double", StringComparison.OrdinalIgnoreCase) => FastParameterTypeKind.Double,
                _ when typeName.Equals("string", StringComparison.OrdinalIgnoreCase) => FastParameterTypeKind.String,
                _ when typeName.Equals("bool", StringComparison.OrdinalIgnoreCase) => FastParameterTypeKind.Bool,
                _ when typeName.Equals("char", StringComparison.OrdinalIgnoreCase) => FastParameterTypeKind.Char,
                _ when typeName.Equals("any", StringComparison.OrdinalIgnoreCase) => FastParameterTypeKind.Any,
                _ when typeName.Equals("object", StringComparison.OrdinalIgnoreCase) => FastParameterTypeKind.Object,
                _ => FastParameterTypeKind.Other
            };

            if (kinds[i] == FastParameterTypeKind.Other)
            {
                hasUnsupportedType = true;
            }
        }

        _fastParameterKinds = kinds;
        _fastParameterKindsCount = ParameterTypes.Count;
        _fastParameterKindsContainUnsupportedType = hasUnsupportedType;
    }

    private void EnsureExceptionDispatchCache()
    {
        if (_exceptionDispatchCandidatesByIp is not null && _exceptionDispatchCacheTableCount == ExceptionTable.Count)
        {
            return;
        }

        _exceptionDispatchCandidatesByIp = new Dictionary<int, ExceptionDispatchCandidate[]>();
        _exceptionDispatchCacheTableCount = ExceptionTable.Count;
    }

    /// <summary>
    /// 序列化默认参数值
    /// </summary>
    private static void WriteDefaultValue(BinaryWriter writer, object value)
    {
        switch (value)
        {
            case int intValue:
                writer.Write((byte)1); // int类型标记
                writer.Write(intValue);
                break;
            case double doubleValue:
                writer.Write((byte)2); // double类型标记
                writer.Write(doubleValue);
                break;
            case string stringValue:
                writer.Write((byte)3); // string类型标记
                writer.Write(stringValue);
                break;
            case bool boolValue:
                writer.Write((byte)4); // bool类型标记
                writer.Write(boolValue);
                break;
            case char charValue:
                writer.Write((byte)5); // char类型标记
                writer.Write(charValue);
                break;
            default:
                throw new NotSupportedException($"不支持的默认参数类型: {value.GetType().Name}");
        }
    }

    /// <summary>
    /// 反序列化默认参数值
    /// </summary>
    private static object ReadDefaultValue(BinaryReader reader)
    {
        byte typeMarker = reader.ReadByte();
        return typeMarker switch
        {
            1 => reader.ReadInt32(),
            2 => reader.ReadDouble(),
            3 => reader.ReadString(),
            4 => reader.ReadBoolean(),
            5 => reader.ReadChar(),
            _ => throw new NotSupportedException($"不支持的默认参数类型标记: {typeMarker}")
        };
    }
}
