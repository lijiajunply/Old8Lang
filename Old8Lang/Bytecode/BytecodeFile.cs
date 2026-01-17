namespace Old8Lang.Bytecode;

using ModuleSystem;

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
    public List<string> GlobalVariables { get; set; } = [];

    /// <summary>函数列表</summary>
    public List<FunctionMetadata> Functions { get; set; } = [];

    /// <summary>类列表</summary>
    public List<ClassMetadata> Classes { get; set; } = [];

    /// <summary>接口列表</summary>
    public List<InterfaceMetadata> Interfaces { get; set; } = [];

    /// <summary>Mixin列表</summary>
    public List<MixinMetadata> Mixins { get; set; } = [];

    /// <summary>调试信息（可选）</summary>
    public DebugInfo? DebugInfo { get; set; }

    /// <summary>入口点函数索引</summary>
    public int EntryPointIndex { get; set; } = -1;

    // ===== 模块系统字段 =====

    /// <summary>模块名称（如果是模块）</summary>
    public string? ModuleName { get; set; }

    /// <summary>模块依赖列表</summary>
    public List<ModuleDependency> Dependencies { get; set; } = [];

    /// <summary>导出符号表</summary>
    public Dictionary<string, ExportedSymbol>? Exports { get; set; }

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

        // 类列表
        writer.Write(Classes.Count);
        foreach (var classMetadata in Classes)
            classMetadata.WriteTo(writer);

        // 接口列表
        writer.Write(Interfaces.Count);
        foreach (var interfaceMetadata in Interfaces)
            interfaceMetadata.WriteTo(writer);

        // Mixin列表
        writer.Write(Mixins.Count);
        foreach (var mixinMetadata in Mixins)
            mixinMetadata.WriteTo(writer);

        // 入口点
        writer.Write(EntryPointIndex);

        // 模块名称（可选）
        bool hasModuleName = !string.IsNullOrEmpty(ModuleName);
        writer.Write(hasModuleName);
        if (hasModuleName)
            writer.Write(ModuleName!);

        // 模块依赖
        writer.Write(Dependencies.Count);
        foreach (var dependency in Dependencies)
        {
            writer.Write(dependency.ModuleName);
            writer.Write(dependency.ImportAll);
            writer.Write(dependency.ModuleAlias ?? string.Empty);

            // 导入符号列表
            bool hasImportedSymbols = dependency.ImportedSymbols != null;
            writer.Write(hasImportedSymbols);
            if (hasImportedSymbols)
            {
                writer.Write(dependency.ImportedSymbols!.Count);
                foreach (var symbol in dependency.ImportedSymbols)
                {
                    writer.Write(symbol.OriginalName);
                    writer.Write(symbol.Alias ?? string.Empty);
                }
            }
        }

        // 导出符号表（可选）
        bool hasExports = Exports != null && Exports.Count > 0;
        writer.Write(hasExports);
        if (hasExports)
        {
            writer.Write(Exports!.Count);
            foreach (var export in Exports)
            {
                writer.Write(export.Key);
                writer.Write((int)export.Value.Type);
                writer.Write(export.Value.MetadataIndex);
            }
        }

        // 调试信息（可选）
        bool hasDebugInfo = DebugInfo != null;
        writer.Write(hasDebugInfo);
        if (hasDebugInfo)
            DebugInfo!.WriteTo(writer);
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
            throw new InvalidDataException(
                $"不兼容的字节码版本: {majorVersion}.{minorVersion} (当前支持: {MAJOR_VERSION}.{MINOR_VERSION})");

        var bytecodeFile = new BytecodeFile
        {
            // 常量池
            ConstantPool = ConstantPool.ReadFrom(reader)
        };

        // 全局变量
        int globalCount = reader.ReadInt32();
        for (int i = 0; i < globalCount; i++)
            bytecodeFile.GlobalVariables.Add(reader.ReadString());

        // 函数列表
        int funcCount = reader.ReadInt32();
        for (int i = 0; i < funcCount; i++)
            bytecodeFile.Functions.Add(FunctionMetadata.ReadFrom(reader));

        // 类列表
        int classCount = reader.ReadInt32();
        for (int i = 0; i < classCount; i++)
            bytecodeFile.Classes.Add(ClassMetadata.ReadFrom(reader));

        // 接口列表
        int interfaceCount = reader.ReadInt32();
        for (int i = 0; i < interfaceCount; i++)
            bytecodeFile.Interfaces.Add(InterfaceMetadata.ReadFrom(reader));

        // Mixin列表
        int mixinCount = reader.ReadInt32();
        for (int i = 0; i < mixinCount; i++)
            bytecodeFile.Mixins.Add(MixinMetadata.ReadFrom(reader));

        // 入口点
        bytecodeFile.EntryPointIndex = reader.ReadInt32();

        // 模块名称（可选）
        bool hasModuleName = reader.ReadBoolean();
        if (hasModuleName)
            bytecodeFile.ModuleName = reader.ReadString();

        // 模块依赖
        int dependencyCount = reader.ReadInt32();
        for (int i = 0; i < dependencyCount; i++)
        {
            var dependency = new ModuleDependency(reader.ReadString())
            {
                ImportAll = reader.ReadBoolean(),
                ModuleAlias = reader.ReadString()
            };

            if (string.IsNullOrEmpty(dependency.ModuleAlias))
                dependency.ModuleAlias = null;

            // 导入符号列表
            bool hasImportedSymbols = reader.ReadBoolean();
            if (hasImportedSymbols)
            {
                int symbolCount = reader.ReadInt32();
                dependency.ImportedSymbols = new List<ImportedSymbol>();
                for (int j = 0; j < symbolCount; j++)
                {
                    string originalName = reader.ReadString();
                    string alias = reader.ReadString();
                    dependency.ImportedSymbols.Add(new ImportedSymbol(
                        originalName,
                        string.IsNullOrEmpty(alias) ? null : alias
                    ));
                }
            }

            bytecodeFile.Dependencies.Add(dependency);
        }

        // 导出符号表（可选）
        bool hasExports = reader.ReadBoolean();
        if (hasExports)
        {
            int exportCount = reader.ReadInt32();
            bytecodeFile.Exports = new Dictionary<string, ExportedSymbol>();
            for (int i = 0; i < exportCount; i++)
            {
                string name = reader.ReadString();
                var type = (ExportedSymbolType)reader.ReadInt32();
                int metadataIndex = reader.ReadInt32();
                bytecodeFile.Exports[name] = new ExportedSymbol(name, type, null, metadataIndex);
            }
        }

        // 调试信息（可选）
        bool hasDebugInfo = reader.ReadBoolean();
        if (hasDebugInfo)
            bytecodeFile.DebugInfo = DebugInfo.ReadFrom(reader);

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
        var parts = new List<string>
        {
            $"{Functions.Count} functions",
            $"{Classes.Count} classes",
            $"{Interfaces.Count} interfaces",
            $"{Mixins.Count} mixins",
            $"{ConstantPool.Count} constants",
            $"{GlobalVariables.Count} globals"
        };

        if (DebugInfo != null)
            parts.Add("with debug info");

        return $"BytecodeFile[{string.Join(", ", parts)}]";
    }
}