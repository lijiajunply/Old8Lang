namespace Old8Lang.Bytecode.Metadata;

/// <summary>
/// 源码位置信息
/// </summary>
public class SourceLocation
{
    /// <summary>源文件路径</summary>
    public string FilePath { get; set; } = "";

    /// <summary>行号（从1开始）</summary>
    public int Line { get; set; }

    /// <summary>列号（从1开始）</summary>
    public int Column { get; set; }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(FilePath);
        writer.Write(Line);
        writer.Write(Column);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static SourceLocation ReadFrom(BinaryReader reader)
    {
        return new SourceLocation
        {
            FilePath = reader.ReadString(),
            Line = reader.ReadInt32(),
            Column = reader.ReadInt32()
        };
    }

    public override string ToString()
    {
        return $"{FilePath}:{Line}:{Column}";
    }
}

/// <summary>
/// 局部变量调试信息
/// </summary>
public class LocalVariableInfo
{
    /// <summary>变量索引</summary>
    public int Index { get; set; }

    /// <summary>变量名称</summary>
    public string Name { get; set; } = "";

    /// <summary>变量作用域开始的指令偏移</summary>
    public int StartOffset { get; set; }

    /// <summary>变量作用域结束的指令偏移</summary>
    public int EndOffset { get; set; }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Name);
        writer.Write(StartOffset);
        writer.Write(EndOffset);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static LocalVariableInfo ReadFrom(BinaryReader reader)
    {
        return new LocalVariableInfo
        {
            Index = reader.ReadInt32(),
            Name = reader.ReadString(),
            StartOffset = reader.ReadInt32(),
            EndOffset = reader.ReadInt32()
        };
    }

    public override string ToString()
    {
        return $"[{Index}] {Name} (offset {StartOffset}-{EndOffset})";
    }
}

/// <summary>
/// 函数调试信息
/// </summary>
public class FunctionDebugInfo
{
    /// <summary>函数名称</summary>
    public string FunctionName { get; set; } = "";

    /// <summary>函数字节码开始的指令偏移</summary>
    public int StartOffset { get; set; }

    /// <summary>函数字节码结束的指令偏移</summary>
    public int EndOffset { get; set; }

    /// <summary>局部变量列表</summary>
    public List<LocalVariableInfo> LocalVariables { get; set; } = [];

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(FunctionName);
        writer.Write(StartOffset);
        writer.Write(EndOffset);

        writer.Write(LocalVariables.Count);
        foreach (var localVar in LocalVariables)
            localVar.WriteTo(writer);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static FunctionDebugInfo ReadFrom(BinaryReader reader)
    {
        var funcInfo = new FunctionDebugInfo
        {
            FunctionName = reader.ReadString(),
            StartOffset = reader.ReadInt32(),
            EndOffset = reader.ReadInt32()
        };

        int localVarCount = reader.ReadInt32();
        for (int i = 0; i < localVarCount; i++)
            funcInfo.LocalVariables.Add(LocalVariableInfo.ReadFrom(reader));

        return funcInfo;
    }

    public override string ToString()
    {
        return $"Function {FunctionName} (offset {StartOffset}-{EndOffset}, {LocalVariables.Count} locals)";
    }
}

/// <summary>
/// 调试信息主类
/// </summary>
public class DebugInfo
{
    /// <summary>指令偏移 → 源码位置映射</summary>
    public Dictionary<int, SourceLocation> InstructionLocations { get; set; } = new();

    /// <summary>函数调试信息列表</summary>
    public List<FunctionDebugInfo> Functions { get; set; } = [];

    /// <summary>
    /// 添加指令位置映射
    /// </summary>
    public void AddInstructionLocation(int offset, string filePath, int line, int column)
    {
        InstructionLocations[offset] = new SourceLocation
        {
            FilePath = filePath,
            Line = line,
            Column = column
        };
    }

    /// <summary>
    /// 获取指定偏移处的源码位置
    /// </summary>
    public SourceLocation? GetSourceLocation(int offset)
    {
        return InstructionLocations.TryGetValue(offset, out var location) ? location : null;
    }

    /// <summary>
    /// 获取指定偏移处的函数信息
    /// </summary>
    public FunctionDebugInfo? GetFunctionAt(int offset)
    {
        return Functions.FirstOrDefault(f => offset >= f.StartOffset && offset <= f.EndOffset);
    }

    /// <summary>
    /// 获取指定偏移处的局部变量名称
    /// </summary>
    public string? GetLocalVariableName(int offset, int localIndex)
    {
        var function = GetFunctionAt(offset);
        if (function == null) return null;

        var localVar = function.LocalVariables.FirstOrDefault(v =>
            v.Index == localIndex &&
            offset >= v.StartOffset &&
            offset <= v.EndOffset);

        return localVar?.Name;
    }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        // 写入指令位置映射
        writer.Write(InstructionLocations.Count);
        foreach (var kvp in InstructionLocations)
        {
            writer.Write(kvp.Key); // offset
            kvp.Value.WriteTo(writer); // location
        }

        // 写入函数调试信息
        writer.Write(Functions.Count);
        foreach (var func in Functions)
            func.WriteTo(writer);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static DebugInfo ReadFrom(BinaryReader reader)
    {
        var debugInfo = new DebugInfo();

        // 读取指令位置映射
        int locationCount = reader.ReadInt32();
        for (int i = 0; i < locationCount; i++)
        {
            int offset = reader.ReadInt32();
            var location = SourceLocation.ReadFrom(reader);
            debugInfo.InstructionLocations[offset] = location;
        }

        // 读取函数调试信息
        int funcCount = reader.ReadInt32();
        for (int i = 0; i < funcCount; i++)
            debugInfo.Functions.Add(FunctionDebugInfo.ReadFrom(reader));

        return debugInfo;
    }

    public override string ToString()
    {
        return $"DebugInfo [{InstructionLocations.Count} instruction locations, {Functions.Count} functions]";
    }
}
