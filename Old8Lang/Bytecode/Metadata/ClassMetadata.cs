namespace Old8Lang.Bytecode;

/// <summary>
/// 访问修饰符枚举
/// </summary>
public enum AccessModifier : byte
{
    /// <summary>公共访问</summary>
    Public = 0,

    /// <summary>私有访问</summary>
    Private = 1,

    /// <summary>受保护访问</summary>
    Protected = 2
}

/// <summary>
/// 字段元数据
/// </summary>
public class FieldMetadata
{
    /// <summary>字段名称</summary>
    public string Name { get; set; } = "";

    /// <summary>访问修饰符</summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;

    /// <summary>是否是静态字段</summary>
    public bool IsStatic { get; set; }

    /// <summary>字段类型名称（可选，用于类型提示）</summary>
    public string? TypeName { get; set; }

    /// <summary>默认值在常量池中的索引（-1表示无默认值）</summary>
    public int DefaultValueIndex { get; set; } = -1;

    /// <summary>默认值是否为 null</summary>
    public bool IsDefaultNull { get; set; }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Name);
        writer.Write((byte)AccessModifier);
        writer.Write(IsStatic);
        writer.Write(TypeName ?? "");
        writer.Write(DefaultValueIndex);
        writer.Write(IsDefaultNull);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static FieldMetadata ReadFrom(BinaryReader reader)
    {
        var field = new FieldMetadata
        {
            Name = reader.ReadString(),
            AccessModifier = (AccessModifier)reader.ReadByte(),
            IsStatic = reader.ReadBoolean()
        };

        var typeName = reader.ReadString();
        field.TypeName = string.IsNullOrEmpty(typeName) ? null : typeName;
        field.DefaultValueIndex = reader.ReadInt32();
        field.IsDefaultNull = reader.ReadBoolean();

        return field;
    }

    public override string ToString()
    {
        var modifiers = new List<string>();
        if (AccessModifier != AccessModifier.Public)
            modifiers.Add(AccessModifier.ToString().ToLower());
        if (IsStatic)
            modifiers.Add("static");

        var modifierStr = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";
        var typeStr = TypeName != null ? $":{TypeName}" : "";
        return $"{modifierStr}{Name}{typeStr}";
    }
}

/// <summary>
/// 方法元数据
/// </summary>
public class MethodMetadata
{
    /// <summary>方法名称</summary>
    public string Name { get; set; } = "";

    /// <summary>访问修饰符</summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;

    /// <summary>是否是静态方法</summary>
    public bool IsStatic { get; set; }

    /// <summary>是否是抽象方法</summary>
    public bool IsAbstract { get; set; }

    /// <summary>是否是虚方法</summary>
    public bool IsVirtual { get; set; }

    /// <summary>函数元数据</summary>
    public FunctionMetadata Function { get; set; } = new();

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Name);
        writer.Write((byte)AccessModifier);
        writer.Write(IsStatic);
        writer.Write(IsAbstract);
        writer.Write(IsVirtual);
        Function.WriteTo(writer);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static MethodMetadata ReadFrom(BinaryReader reader)
    {
        var method = new MethodMetadata
        {
            Name = reader.ReadString(),
            AccessModifier = (AccessModifier)reader.ReadByte(),
            IsStatic = reader.ReadBoolean(),
            IsAbstract = reader.ReadBoolean(),
            IsVirtual = reader.ReadBoolean(),
            Function = FunctionMetadata.ReadFrom(reader)
        };

        return method;
    }

    public override string ToString()
    {
        var modifiers = new List<string>();
        if (AccessModifier != AccessModifier.Public)
            modifiers.Add(AccessModifier.ToString().ToLower());
        if (IsStatic)
            modifiers.Add("static");
        if (IsAbstract)
            modifiers.Add("abstract");
        if (IsVirtual)
            modifiers.Add("virtual");

        var modifierStr = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";
        return $"{modifierStr}func {Name}({string.Join(", ", Function.Parameters)})";
    }
}

/// <summary>
/// 类元数据
/// </summary>
public class ClassMetadata
{
    /// <summary>类名</summary>
    public string Name { get; set; } = "";

    /// <summary>父类名（可选）</summary>
    public string? BaseClassName { get; set; }

    /// <summary>实现的接口列表</summary>
    public List<string> InterfaceNames { get; set; } = [];

    /// <summary>实现的接口列表（新字段，与InterfaceNames保持一致）</summary>
    public List<string> ImplementsInterfaces { get; set; } = [];

    /// <summary>混入的Mixin列表</summary>
    public List<string> Mixins { get; set; } = [];

    /// <summary>实例字段列表</summary>
    public List<FieldMetadata> Fields { get; set; } = [];

    /// <summary>实例方法列表</summary>
    public List<MethodMetadata> Methods { get; set; } = [];

    /// <summary>静态字段列表</summary>
    public List<FieldMetadata> StaticFields { get; set; } = [];

    /// <summary>静态方法列表</summary>
    public List<MethodMetadata> StaticMethods { get; set; } = [];

    /// <summary>是否是接口</summary>
    public bool IsInterface { get; set; }

    /// <summary>是否是抽象类</summary>
    public bool IsAbstract { get; set; }

    /// <summary>是否是Mixin</summary>
    public bool IsMixin { get; set; }

    /// <summary>类在常量池中的索引</summary>
    public int ClassIndex { get; set; } = -1;

    /// <summary>
    /// 静态字段的运行时值存储
    /// key: 字段名
    /// value: 字段值
    /// </summary>
    public Dictionary<string, object?> StaticFieldValues { get; set; } = new();

    /// <summary>泛型类型参数映射（用于泛型类实例化）</summary>
    /// <remarks>例如: Container&lt;int?> 时为 {"T": "int"}</remarks>
    public Dictionary<string, string>? GenericTypeMapping { get; set; }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        // 基本信息
        writer.Write(Name);
        writer.Write(BaseClassName ?? "");

        // 接口列表
        writer.Write(InterfaceNames.Count);
        foreach (var interfaceName in InterfaceNames)
            writer.Write(interfaceName);

        // 实例字段
        writer.Write(Fields.Count);
        foreach (var field in Fields)
            field.WriteTo(writer);

        // 实例方法
        writer.Write(Methods.Count);
        foreach (var method in Methods)
            method.WriteTo(writer);

        // 静态字段
        writer.Write(StaticFields.Count);
        foreach (var field in StaticFields)
            field.WriteTo(writer);

        // 静态方法
        writer.Write(StaticMethods.Count);
        foreach (var method in StaticMethods)
            method.WriteTo(writer);

        // 类型标志
        writer.Write(IsInterface);
        writer.Write(IsAbstract);
        writer.Write(IsMixin);
        writer.Write(ClassIndex);

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
    public static ClassMetadata ReadFrom(BinaryReader reader)
    {
        var classMetadata = new ClassMetadata
        {
            Name = reader.ReadString()
        };

        // 父类名
        var baseClassName = reader.ReadString();
        classMetadata.BaseClassName = string.IsNullOrEmpty(baseClassName) ? null : baseClassName;

        // 接口列表
        int interfaceCount = reader.ReadInt32();
        for (int i = 0; i < interfaceCount; i++)
            classMetadata.InterfaceNames.Add(reader.ReadString());

        // 实例字段
        int fieldCount = reader.ReadInt32();
        for (int i = 0; i < fieldCount; i++)
            classMetadata.Fields.Add(FieldMetadata.ReadFrom(reader));

        // 实例方法
        int methodCount = reader.ReadInt32();
        for (int i = 0; i < methodCount; i++)
            classMetadata.Methods.Add(MethodMetadata.ReadFrom(reader));

        // 静态字段
        int staticFieldCount = reader.ReadInt32();
        for (int i = 0; i < staticFieldCount; i++)
            classMetadata.StaticFields.Add(FieldMetadata.ReadFrom(reader));

        // 静态方法
        int staticMethodCount = reader.ReadInt32();
        for (int i = 0; i < staticMethodCount; i++)
            classMetadata.StaticMethods.Add(MethodMetadata.ReadFrom(reader));

        // 类型标志
        classMetadata.IsInterface = reader.ReadBoolean();
        classMetadata.IsAbstract = reader.ReadBoolean();
        classMetadata.IsMixin = reader.ReadBoolean();
        classMetadata.ClassIndex = reader.ReadInt32();

        // 泛型类型映射
        int genericTypeMappingCount = reader.ReadInt32();
        if (genericTypeMappingCount > 0)
        {
            classMetadata.GenericTypeMapping = new Dictionary<string, string>();
            for (int i = 0; i < genericTypeMappingCount; i++)
            {
                string key = reader.ReadString();
                string value = reader.ReadString();
                classMetadata.GenericTypeMapping[key] = value;
            }
        }

        return classMetadata;
    }

    public override string ToString()
    {
        var typePrefix = IsInterface ? "interface" :
                        IsMixin ? "mixin" :
                        IsAbstract ? "abstract class" : "class";

        var inheritance = new List<string>();
        if (BaseClassName != null)
            inheritance.Add($"extends {BaseClassName}");
        if (InterfaceNames.Count > 0)
            inheritance.Add($"implements {string.Join(", ", InterfaceNames)}");

        var inheritanceStr = inheritance.Count > 0 ? " " + string.Join(" ", inheritance) : "";

        var stats = new List<string>();
        if (Fields.Count > 0)
            stats.Add($"{Fields.Count} fields");
        if (Methods.Count > 0)
            stats.Add($"{Methods.Count} methods");
        if (StaticFields.Count > 0)
            stats.Add($"{StaticFields.Count} static fields");
        if (StaticMethods.Count > 0)
            stats.Add($"{StaticMethods.Count} static methods");

        var statsStr = stats.Count > 0 ? $" [{string.Join(", ", stats)}]" : "";

        return $"{typePrefix} {Name}{inheritanceStr}{statsStr}";
    }
}
