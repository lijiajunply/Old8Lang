using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
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
    /// 把运行期的「类型值」归一化成类型名。
    /// </summary>
    /// <remarks>
    /// 虚拟机里表示类型值的方式不唯一：<c>GetType</c> / <c>TypeOf</c> / <c>GetAllTypes</c>
    /// 返回装箱的 <see cref="TypeLangValue"/>，而 <c>TypeLangValue</c> 的实例方法在 VM 侧
    /// 统一按「类型名字符串」实现。原先两套写法互不认识，导致 <c>GetType("A").IsClass()</c>
    /// 抛「VM 模式下 TypeLangValue 应该是字符串类型」。这里做一次归一化，两种写法都能用。
    /// </remarks>
    /// <returns>类型名；无法识别时返回 null</returns>
    public static string? ResolveTypeName(object? value)
    {
        return value switch
        {
            null => null,
            string name => name,
            TypeLangValue typeValue => typeValue.Value,
            BytecodeObjectInstance instance => instance.ClassName,
            ClassMetadata metadata => metadata.Name,
            _ => null
        };
    }

    /// <summary>
    /// 按任意形式的类型值取出类元数据
    /// </summary>
    public static ClassMetadata? GetClassMetadataFromTypeValue(VirtualMachine vm, object? value)
    {
        var typeName = ResolveTypeName(value);
        return string.IsNullOrEmpty(typeName) ? null : GetClassMetadata(vm, typeName);
    }

    /// <summary>
    /// 判断类型是否为泛型（虚拟机侧依据特化类名记录的类型参数映射）
    /// </summary>
    public static bool IsGenericType(ClassMetadata? classMetadata)
    {
        return classMetadata?.GenericTypeMapping is { Count: > 0 };
    }

    /// <summary>
    /// 判断类型名是否是基本类型（口径与 <see cref="TypeLangValue.IsPrimitiveTypeName"/> 一致）
    /// </summary>
    public static bool IsPrimitiveTypeName(string? typeName)
    {
        return TypeLangValue.IsPrimitiveTypeName(typeName);
    }

    /// <summary>
    /// 用语言口径描述一个运行期值的类型（用于 <c>TypeOf</c> 与报错信息）。
    /// </summary>
    /// <remarks>
    /// 直接写 <c>value.GetType().Name</c> 会把 <c>Tuple`2</c>、<c>List`1</c> 这类
    /// .NET 类型名泄露给使用者，这里统一换成语义相同的语言类型名
    /// （与 <see cref="LangValueType.TypeToString"/> 一致）。
    /// </remarks>
    public static string DescribeValueType(object? value)
    {
        return value switch
        {
            null => "Null",
            BytecodeObjectInstance instance => instance.ClassName,
            LangValueType langValue => langValue.TypeToString(),
            // object[] 也实现 IList，必须排在集合分支之前
            object[] => "Array",
            System.Collections.IDictionary => "Dictionary",
            System.Collections.IList => "List",
            Tuple<object?, object?> => "Tuple",
            bool => "Bool",
            char => "Char",
            int => "Int",
            double => "Double",
            string => "String",
            _ => value.GetType().Name
        };
    }

    /// <summary>
    /// 用语言口径描述一个 CLR 类型（只有 <see cref="Type"/> 可用时使用）。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="DescribeValueType"/> 同一口径：先按接口/基类归类，
    /// 再退回类型名，避免出现 <c>List`1</c>、<c>Int32</c> 这类名字。
    /// </remarks>
    public static string DescribeTypeName(Type? type)
    {
        if (type is null)
        {
            return "Null";
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(byte) || type == typeof(sbyte) || type == typeof(uint) ||
            type == typeof(ulong) || type == typeof(ushort))
        {
            return "Int";
        }

        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
        {
            return "Double";
        }

        if (type == typeof(string))
        {
            return "String";
        }

        if (type == typeof(bool))
        {
            return "Bool";
        }

        if (type == typeof(char))
        {
            return "Char";
        }

        if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
        {
            return "Dictionary";
        }

        if (typeof(System.Collections.IList).IsAssignableFrom(type) || type.IsArray)
        {
            return type.IsArray ? "Array" : "List";
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            return "Tuple";
        }

        return type.Name;
    }

    /// <summary>
    /// 从对象实例获取类元数据
    /// </summary>
    public static ClassMetadata? GetClassMetadataFromInstance(VirtualMachine vm, BytecodeObjectInstance instance)
    {
        return GetClassMetadata(vm, instance.ClassName);
    }

    /// <summary>
    /// 获取类的所有方法名（包括静态和实例方法，沿继承链向上）
    /// </summary>
    public static List<string> GetAllMethodNames(VirtualMachine vm, ClassMetadata classMetadata)
    {
        var methodNames = new List<string>();
        foreach (var metadata in EnumerateClassChain(vm, classMetadata))
        {
            AddDistinct(methodNames, metadata.Methods.Select(m => m.Name));
            AddDistinct(methodNames, metadata.StaticMethods.Select(m => m.Name));
        }

        return methodNames;
    }

    /// <summary>
    /// 获取类的所有字段名（包括静态和实例字段，沿继承链向上）
    /// </summary>
    public static List<string> GetAllFieldNames(VirtualMachine vm, ClassMetadata classMetadata)
    {
        var fieldNames = new List<string>();
        foreach (var metadata in EnumerateClassChain(vm, classMetadata))
        {
            AddDistinct(fieldNames, metadata.Fields.Select(f => f.Name));
            AddDistinct(fieldNames, metadata.StaticFields.Select(f => f.Name));
        }

        return fieldNames;
    }

    /// <summary>
    /// 查找方法元数据（沿继承链向上）
    /// </summary>
    public static MethodMetadata? FindMethod(VirtualMachine vm, ClassMetadata classMetadata, string methodName)
    {
        return EnumerateClassChain(vm, classMetadata)
            .Select(metadata => metadata.Methods.FirstOrDefault(m => m.Name == methodName)
                                ?? metadata.StaticMethods.FirstOrDefault(m => m.Name == methodName))
            .FirstOrDefault(method => method != null);
    }

    /// <summary>
    /// 查找字段元数据（沿继承链向上）
    /// </summary>
    public static FieldMetadata? FindField(VirtualMachine vm, ClassMetadata classMetadata, string fieldName)
    {
        return EnumerateClassChain(vm, classMetadata)
            .Select(metadata => metadata.Fields.FirstOrDefault(f => f.Name == fieldName)
                                ?? metadata.StaticFields.FirstOrDefault(f => f.Name == fieldName))
            .FirstOrDefault(field => field != null);
    }

    /// <summary>
    /// 检查方法是否存在（沿继承链向上）
    /// </summary>
    public static bool HasMethod(VirtualMachine vm, ClassMetadata classMetadata, string methodName)
    {
        return EnumerateClassChain(vm, classMetadata).Any(metadata =>
            metadata.Methods.Any(m => m.Name == methodName) ||
            metadata.StaticMethods.Any(m => m.Name == methodName));
    }

    /// <summary>
    /// 检查字段是否存在（沿继承链向上）
    /// </summary>
    public static bool HasField(VirtualMachine vm, ClassMetadata classMetadata, string fieldName)
    {
        return EnumerateClassChain(vm, classMetadata).Any(metadata =>
            metadata.Fields.Any(f => f.Name == fieldName) ||
            metadata.StaticFields.Any(f => f.Name == fieldName));
    }

    /// <summary>
    /// 自身 + 全部父类（按继承顺序，遇到环即停）
    /// </summary>
    private static IEnumerable<ClassMetadata> EnumerateClassChain(VirtualMachine vm, ClassMetadata classMetadata)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var current = classMetadata; current != null && visited.Add(current.Name);)
        {
            yield return current;
            current = string.IsNullOrEmpty(current.BaseClassName)
                ? null
                : GetClassMetadata(vm, current.BaseClassName);
        }
    }

    private static void AddDistinct(List<string> target, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!target.Contains(name, StringComparer.Ordinal))
            {
                target.Add(name);
            }
        }
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
            ["methods"] = GetAllMethodNames(vm, classMetadata),
            ["fields"] = GetAllFieldNames(vm, classMetadata),
            ["isGeneric"] = IsGenericType(classMetadata)
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
