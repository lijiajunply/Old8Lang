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

    /// <summary>
    /// 获取所有已注册的类名
    /// </summary>
    public static List<string> GetAllClassNames(VirtualMachine vm)
    {
        var classNames = new List<string>();

        // 遍历全局变量，查找所有 ClassMetadata
        var globals = vm.GetAllGlobalVariables();
        foreach (var (name, value) in globals)
        {
            if (value is ClassMetadata)
            {
                classNames.Add(name);
            }
        }

        return classNames;
    }

    /// <summary>
    /// 获取类型的详细信息
    /// </summary>
    public static Dictionary<string, object?> GetTypeInfo(VirtualMachine vm, string className)
    {
        var classMetadata = GetClassMetadata(vm, className);
        if (classMetadata == null)
        {
            throw new InvalidOperationException($"找不到类型: {className}");
        }

        var info = new Dictionary<string, object?>
        {
            ["name"] = classMetadata.Name,
            ["isInterface"] = classMetadata.IsInterface,
            ["isAbstract"] = classMetadata.IsAbstract,
            ["isMixin"] = classMetadata.IsMixin,
            ["baseClass"] = classMetadata.BaseClassName,
            ["interfaces"] = classMetadata.InterfaceNames,
            ["mixins"] = classMetadata.Mixins,
            ["methods"] = GetAllMethodNames(classMetadata),
            ["fields"] = GetAllFieldNames(classMetadata),
            ["isGeneric"] = false // VM 模式下暂不支持泛型标记
        };

        return info;
    }

    /// <summary>
    /// 检查类型兼容性（是否可以从 sourceType 赋值给 targetType）
    /// </summary>
    public static bool IsAssignableFrom(VirtualMachine vm, string targetTypeName, string sourceTypeName)
    {
        // 同一类型
        if (targetTypeName == sourceTypeName)
            return true;

        var sourceMetadata = GetClassMetadata(vm, sourceTypeName);
        if (sourceMetadata == null)
            return false;

        // 检查父类链
        var currentParent = sourceMetadata.BaseClassName;
        while (currentParent != null)
        {
            if (currentParent == targetTypeName)
                return true;

            var parentMetadata = GetClassMetadata(vm, currentParent);
            if (parentMetadata == null)
                break;

            currentParent = parentMetadata.BaseClassName;
        }

        return false;
    }

    /// <summary>
    /// 获取父类型名
    /// </summary>
    public static string? GetBaseType(VirtualMachine vm, string className)
    {
        var classMetadata = GetClassMetadata(vm, className);
        return classMetadata?.BaseClassName;
    }

    /// <summary>
    /// 获取实现的接口列表（VM 模式下暂不支持）
    /// </summary>
    public static List<string> GetInterfaces(VirtualMachine vm, string className)
    {
        // VM 模式下暂不支持接口，返回空列表
        return new List<string>();
    }
}
