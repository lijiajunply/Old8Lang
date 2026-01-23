using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Bytecode.VM;

namespace Old8Lang.Runtime;

/// <summary>
/// 虚拟机模式下的反射辅助类
/// </summary>
public static class VMReflectionHelper
{
    /// <summary>
    /// 从虚拟机获取类元数据
    /// </summary>
    public static ClassMetadata? GetClassMetadata(VirtualMachine vm, string className)
    {
        var classMetadata = vm.GetGlobalVariable(className);
        return classMetadata as ClassMetadata;
    }

    /// <summary>
    /// 从对象实例获取类元数据
    /// </summary>
    public static ClassMetadata? GetClassMetadataFromInstance(VirtualMachine vm, BytecodeObjectInstance instance)
    {
        return GetClassMetadata(vm, instance.ClassName);
    }

    /// <summary>
    /// 获取类的所有方法名（包括静态和实例方法）
    /// </summary>
    public static List<string> GetAllMethodNames(ClassMetadata classMetadata)
    {
        var methodNames = new List<string>();
        methodNames.AddRange(classMetadata.Methods.Select(m => m.Name));
        methodNames.AddRange(classMetadata.StaticMethods.Select(m => m.Name));
        return methodNames;
    }

    /// <summary>
    /// 获取类的所有字段名（包括静态和实例字段）
    /// </summary>
    public static List<string> GetAllFieldNames(ClassMetadata classMetadata)
    {
        var fieldNames = new List<string>();
        fieldNames.AddRange(classMetadata.Fields.Select(f => f.Name));
        fieldNames.AddRange(classMetadata.StaticFields.Select(f => f.Name));
        return fieldNames;
    }

    /// <summary>
    /// 查找方法元数据
    /// </summary>
    public static MethodMetadata? FindMethod(ClassMetadata classMetadata, string methodName)
    {
        // 先查找实例方法
        var method = classMetadata.Methods.FirstOrDefault(m => m.Name == methodName);
        if (method != null) return method;

        // 再查找静态方法
        return classMetadata.StaticMethods.FirstOrDefault(m => m.Name == methodName);
    }

    /// <summary>
    /// 查找字段元数据
    /// </summary>
    public static FieldMetadata? FindField(ClassMetadata classMetadata, string fieldName)
    {
        // 先查找实例字段
        var field = classMetadata.Fields.FirstOrDefault(f => f.Name == fieldName);
        if (field != null) return field;

        // 再查找静态字段
        return classMetadata.StaticFields.FirstOrDefault(f => f.Name == fieldName);
    }

    /// <summary>
    /// 检查方法是否存在
    /// </summary>
    public static bool HasMethod(ClassMetadata classMetadata, string methodName)
    {
        return classMetadata.Methods.Any(m => m.Name == methodName) ||
               classMetadata.StaticMethods.Any(m => m.Name == methodName);
    }

    /// <summary>
    /// 检查字段是否存在
    /// </summary>
    public static bool HasField(ClassMetadata classMetadata, string fieldName)
    {
        return classMetadata.Fields.Any(f => f.Name == fieldName) ||
               classMetadata.StaticFields.Any(f => f.Name == fieldName);
    }

    /// <summary>
    /// 创建方法信息字典
    /// </summary>
    public static List<(string, object?)> CreateMethodInfoTuples(MethodMetadata method)
    {
        return new List<(string, object?)>
        {
            ("name", method.Name),
            ("isStatic", method.IsStatic),
            ("isPublic", method.AccessModifier == AccessModifier.Public),
            ("isPrivate", method.AccessModifier == AccessModifier.Private),
            ("parameterCount", method.Function.Parameters.Count)
        };
    }

    /// <summary>
    /// 创建字段信息字典
    /// </summary>
    public static List<(string, object?)> CreateFieldInfoTuples(FieldMetadata field)
    {
        return new List<(string, object?)>
        {
            ("name", field.Name),
            ("isStatic", field.IsStatic),
            ("isPublic", field.AccessModifier == AccessModifier.Public),
            ("isPrivate", field.AccessModifier == AccessModifier.Private)
        };
    }
}
