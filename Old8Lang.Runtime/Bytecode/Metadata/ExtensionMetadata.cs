using Old8Lang.AST.Expression;

namespace Old8Lang.Bytecode.Metadata;

/// <summary>
/// 扩展方法元数据
/// </summary>
public class ExtensionMetadata
{
    /// <summary>目标类型名称</summary>
    public string TargetTypeName { get; set; } = string.Empty;

    /// <summary>目标类型泛型参数与约束列表</summary>
    public List<GenericParameter> TargetGenericParameters { get; set; } = [];

    /// <summary>扩展方法列表</summary>
    public List<FunctionMetadata> Methods { get; set; } = [];

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(TargetTypeName);
        writer.Write(TargetGenericParameters.Count);
        foreach (var param in TargetGenericParameters)
        {
            writer.Write(param.Name);
            writer.Write(param.IsNullable);
            var constraints = param.StructuredConstraints ?? [];
            writer.Write(constraints.Count);
            foreach (var c in constraints)
            {
                writer.Write((byte)c.Kind);
                writer.Write(c.TypeName ?? string.Empty);
            }
        }
        writer.Write(Methods.Count);
        foreach (var method in Methods)
        {
            method.WriteTo(writer);
        }
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static ExtensionMetadata ReadFrom(BinaryReader reader)
    {
        var metadata = new ExtensionMetadata
        {
            TargetTypeName = reader.ReadString()
        };

        int genericParamCount = reader.ReadInt32();
        for (int i = 0; i < genericParamCount; i++)
        {
            var name = reader.ReadString();
            var isNullable = reader.ReadBoolean();
            int constraintCount = reader.ReadInt32();
            var constraints = new List<GenericConstraint>();
            for (int j = 0; j < constraintCount; j++)
            {
                var kind = (GenericConstraintKind)reader.ReadByte();
                var typeName = reader.ReadString();
                constraints.Add(kind switch
                {
                    GenericConstraintKind.New => GenericConstraint.CreateNew(),
                    GenericConstraintKind.Class => GenericConstraint.CreateClass(),
                    GenericConstraintKind.Struct => GenericConstraint.CreateStruct(),
                    GenericConstraintKind.TypeParameter => GenericConstraint.CreateTypeParameter(typeName),
                    _ => GenericConstraint.CreateTypeName(typeName)
                });
            }
            metadata.TargetGenericParameters.Add(new GenericParameter(name, constraints, default, isNullable));
        }

        int methodCount = reader.ReadInt32();
        for (int i = 0; i < methodCount; i++)
        {
            metadata.Methods.Add(FunctionMetadata.ReadFrom(reader));
        }

        return metadata;
    }
}
