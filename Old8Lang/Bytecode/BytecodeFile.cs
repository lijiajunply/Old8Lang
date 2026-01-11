namespace Old8Lang.Bytecode;

/// <summary>
/// 字节码文件(.o8c)
/// </summary>
public class BytecodeFile
{
    // 魔数: "OLD8" (0x4F4C4438)
    private const uint MAGIC_NUMBER = 0x4F4C4438;

    // 文件格式版本
    private const ushort MAJOR_VERSION = 1;
    private const ushort MINOR_VERSION = 0;

    /// <summary>常量池</summary>
    public ConstantPool ConstantPool { get; set; } = new();

    /// <summary>全局变量名称列表</summary>
    public List<string> GlobalVariables { get; set; } = new();

    /// <summary>函数列表</summary>
    public List<FunctionMetadata> Functions { get; set; } = new();

    /// <summary>入口点函数索引</summary>
    public int EntryPointIndex { get; set; } = -1;

    /// <summary>
    /// 保存到文件
    /// </summary>
    public void SaveToFile(string filePath)
    {
        using var fileStream = File.Create(filePath);
        using var writer = new BinaryWriter(fileStream);

        WriteTo(writer);
    }

    /// <summary>
    /// 从文件加载
    /// </summary>
    public static BytecodeFile LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"字节码文件不存在: {filePath}");

        using var fileStream = File.OpenRead(filePath);
        using var reader = new BinaryReader(fileStream);

        return ReadFrom(reader);
    }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        // 魔数
        writer.Write(MAGIC_NUMBER);

        // 版本号
        writer.Write(MAJOR_VERSION);
        writer.Write(MINOR_VERSION);

        // 常量池
        ConstantPool.WriteTo(writer);

        // 全局变量
        writer.Write(GlobalVariables.Count);
        foreach (var globalVar in GlobalVariables)
            writer.Write(globalVar);

        // 函数列表
        writer.Write(Functions.Count);
        foreach (var function in Functions)
            function.WriteTo(writer);

        // 入口点
        writer.Write(EntryPointIndex);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static BytecodeFile ReadFrom(BinaryReader reader)
    {
        // 验证魔数
        uint magic = reader.ReadUInt32();
        if (magic != MAGIC_NUMBER)
            throw new InvalidDataException($"无效的字节码文件格式: 魔数不匹配 (期望: 0x{MAGIC_NUMBER:X}, 实际: 0x{magic:X})");

        // 读取版本号
        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();

        if (majorVersion != MAJOR_VERSION)
            throw new InvalidDataException($"不兼容的字节码版本: {majorVersion}.{minorVersion} (当前支持: {MAJOR_VERSION}.{MINOR_VERSION})");

        var bytecodeFile = new BytecodeFile();

        // 常量池
        bytecodeFile.ConstantPool = ConstantPool.ReadFrom(reader);

        // 全局变量
        int globalCount = reader.ReadInt32();
        for (int i = 0; i < globalCount; i++)
            bytecodeFile.GlobalVariables.Add(reader.ReadString());

        // 函数列表
        int funcCount = reader.ReadInt32();
        for (int i = 0; i < funcCount; i++)
            bytecodeFile.Functions.Add(FunctionMetadata.ReadFrom(reader));

        // 入口点
        bytecodeFile.EntryPointIndex = reader.ReadInt32();

        return bytecodeFile;
    }

    /// <summary>
    /// 获取入口点函数
    /// </summary>
    public FunctionMetadata? GetEntryPoint()
    {
        if (EntryPointIndex < 0 || EntryPointIndex >= Functions.Count)
            return null;

        return Functions[EntryPointIndex];
    }

    /// <summary>
    /// 根据名称查找函数
    /// </summary>
    public FunctionMetadata? FindFunction(string name)
    {
        return Functions.FirstOrDefault(f => f.Name == name);
    }

    public override string ToString()
    {
        return $"BytecodeFile[{Functions.Count} functions, {ConstantPool.Count} constants, {GlobalVariables.Count} globals]";
    }
}
