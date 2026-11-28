using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using System.Reflection;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.GlobalFunctions.Core;

namespace Old8Lang.Runtime;

/// <summary>
/// 反射运行时辅助类
/// 提供 IL 模式下的反射支持
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// 获取类的完整信息（合并了 GetClassName, GetClassMethods, GetClassFields）
    /// </summary>
    public static object GetClassInfo(object obj)
    {
        if (obj is AnyLangValue anyValue)
        {
            // 获取所有方法名
            var methods = anyValue.Metadata.MethodTable.GetAllMethods()
                .Select(m => new StringLangValue(m.MethodName))
                .Cast<LangValueType>()
                .ToList();

            // 获取所有字段名
            var fields = anyValue.Metadata.FieldTable.GetAllFields()
                .Select(f => new StringLangValue(f.FieldName))
                .Cast<LangValueType>()
                .ToList();

            // 构建完整的类信息字典
            var tuples = new List<TupleLangValue>
            {
                new TupleLangValue([new StringLangValue("className"), new StringLangValue(anyValue.ClassId.IdName)]),
                new TupleLangValue([new StringLangValue("methods"), new ListLangValue(methods)]),
                new TupleLangValue([new StringLangValue("fields"), new ListLangValue(fields)]),
                new TupleLangValue([new StringLangValue("isInterface"), new BoolLangValue(anyValue.Metadata.IsInterface)]),
                new TupleLangValue([new StringLangValue("isAbstract"), new BoolLangValue(anyValue.Metadata.IsAbstract)]),
                new TupleLangValue([new StringLangValue("isMixin"), new BoolLangValue(anyValue.Metadata.IsMixin)]),
                new TupleLangValue([
                    new StringLangValue("baseClass"),
                    anyValue.Metadata.ParentClassName is not null
                        ? new StringLangValue(anyValue.Metadata.ParentClassName)
                        : new NullLangValue()
                ]),
                new TupleLangValue([
                    new StringLangValue("interfaces"),
                    new ListLangValue(
                        anyValue.Metadata.InterfaceNames
                            .Select(name => new StringLangValue(name))
                            .Cast<LangValueType>()
                            .ToList()
                    )
                ])
            };

            return new DictionaryLangValue(tuples);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 获取成员详细信息（合并了 GetMethodInfo 和 GetFieldInfo）
    /// </summary>
    public static object GetMemberInfo(object obj, string memberName)
    {
        if (obj is AnyLangValue anyValue)
        {
            // 先尝试查找方法
            var methods = anyValue.Metadata.MethodTable.LookupMethod(memberName);
            if (methods is not null && methods.Count > 0)
            {
                var method = methods[0];
                var methodTuples = new List<TupleLangValue>
                {
                    new TupleLangValue([new StringLangValue("name"), new StringLangValue(method.MethodName)]),
                    new TupleLangValue([new StringLangValue("type"), new StringLangValue("method")]),
                    new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(method.IsStatic)]),
                    new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!method.HasModifier(AccessModifierType.Private))]),
                    new TupleLangValue([new StringLangValue("isPrivate"), new BoolLangValue(method.HasModifier(AccessModifierType.Private))]),
                    new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(method.ParameterCount)]),
                    new TupleLangValue([new StringLangValue("overloadCount"), new IntLangValue(methods.Count)])
                };
                return new DictionaryLangValue(methodTuples);
            }

            // 再尝试查找字段
            var field = anyValue.Metadata.FieldTable.LookupField(memberName);
            if (field is not null)
            {
                var fieldTuples = new List<TupleLangValue>
                {
                    new TupleLangValue([new StringLangValue("name"), new StringLangValue(field.FieldName)]),
                    new TupleLangValue([new StringLangValue("type"), new StringLangValue("field")]),
                    new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(field.IsStatic)]),
                    new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!field.HasModifier(AccessModifierType.Private))]),
                    new TupleLangValue([new StringLangValue("isPrivate"), new BoolLangValue(field.HasModifier(AccessModifierType.Private))])
                };
                return new DictionaryLangValue(fieldTuples);
            }

            throw new AttributeError(anyValue, memberName, anyValue.ClassId.IdName);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 检查对象是否有指定成员（合并了 HasMethod 和 HasField）
    /// </summary>
    public static bool HasMember(object obj, string memberName)
    {
        if (obj is AnyLangValue anyValue)
        {
            bool hasMethod = anyValue.Metadata.MethodTable.ContainsMethod(memberName);
            bool hasField = anyValue.Metadata.FieldTable.ContainsField(memberName);
            return hasMethod || hasField;
        }
        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm != null)
            {
                var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
                if (classMetadata != null)
                {
                    bool hasMethod = VMReflectionHelper.HasMethod(vm, classMetadata, memberName);
                    bool hasField = VMReflectionHelper.HasField(vm, classMetadata, memberName);
                    return hasMethod || hasField;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 获取函数信息（支持全局函数、普通函数、原生函数）
    /// </summary>
    public static object GetFunctionInfo(object function)
    {
        // 情况 1: 字符串 - 全局函数
        if (function is string functionName)
        {
            var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(functionName);
            if (globalFunc != null)
            {
                return CreateGlobalFunctionInfoDict(globalFunc);
            }
            throw new InvalidOperationException($"全局函数 '{functionName}' 不存在");
        }

        // 情况 2: FuncLangValue - 普通函数或原生函数
        if (function is FuncLangValue funcValue)
        {
            if (funcValue.Method != null)
            {
                return CreateNativeFunctionInfoDict(funcValue.Method, funcValue.Id!.IdName);
            }
            return CreateUserFunctionInfoDict(funcValue);
        }

        throw new InvalidOperationException("GetFunctionInfo 需要：全局函数名（字符串）或函数对象（FuncLangValue）");
    }

    /// <summary>
    /// 获取函数信息（支持类方法）
    /// </summary>
    public static object GetFunctionInfo(object obj, string methodName)
    {
        if (obj is AnyLangValue anyValue)
        {
            var methods = anyValue.Metadata.MethodTable.LookupMethod(methodName);
            if (methods is null || methods.Count == 0)
            {
                throw new AttributeError(anyValue, methodName, anyValue.ClassId.IdName);
            }
            return CreateClassMethodInfoDict(methods[0], anyValue.ClassId.IdName);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 创建全局函数信息字典
    /// </summary>
    private static object CreateGlobalFunctionInfoDict(IGlobalFunction func)
    {
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(func.Names[0])]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("global_function")]),
            new TupleLangValue([
                new StringLangValue("names"),
                new ListLangValue(func.Names.Select(n => (LangValueType)new StringLangValue(n)).ToList())
            ]),
            new TupleLangValue([new StringLangValue("minParameterCount"), new IntLangValue(func.MinParameterCount)]),
            new TupleLangValue([new StringLangValue("maxParameterCount"), new IntLangValue(func.MaxParameterCount)]),
            new TupleLangValue([new StringLangValue("returnType"), new StringLangValue("object")])
        };
        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建用户函数信息字典
    /// </summary>
    private static object CreateUserFunctionInfoDict(FuncLangValue func)
    {
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(func.Id!.IdName)]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("user_function")]),
            new TupleLangValue([
                new StringLangValue("parameters"),
                new ListLangValue(func.Ids!.Select(id => (LangValueType)new StringLangValue(id.IdName)).ToList())
            ]),
            new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(func.Ids!.Count)]),
            new TupleLangValue([new StringLangValue("hasDefaultParams"), new BoolLangValue(func.Ids!.Any(id => id.DefaultValue != null))]),
            new TupleLangValue([new StringLangValue("isGeneric"), new BoolLangValue(func.GenericParameters?.Count > 0)])
        };
        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建原生函数信息字典
    /// </summary>
    private static object CreateNativeFunctionInfoDict(MethodInfo method, string name)
    {
        var parameters = method.GetParameters();
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(name)]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("native_function")]),
            new TupleLangValue([
                new StringLangValue("parameters"),
                new ListLangValue(parameters.Select(p => (LangValueType)new StringLangValue(p.Name ?? "param")).ToList())
            ]),
            new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(parameters.Length)]),
            new TupleLangValue([new StringLangValue("returnType"), new StringLangValue(method.ReturnType.Name)]),
            new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(method.IsStatic)])
        };
        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建类方法信息字典
    /// </summary>
    private static object CreateClassMethodInfoDict(LangMethodInfo method, string className)
    {
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(method.MethodName)]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("class_method")]),
            new TupleLangValue([new StringLangValue("className"), new StringLangValue(className)]),
            new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(method.ParameterCount)]),
            new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(method.IsStatic)]),
            new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!method.HasModifier(AccessModifierType.Private))])
        };
        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 获取对象的类名
    /// </summary>
    public static string GetClassName(object obj)
    {
        if (obj is AnyLangValue anyValue)
        {
            return anyValue.ClassId.IdName;
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 获取类的所有方法名列表
    /// </summary>
    public static object GetClassMethods(object obj)
    {
        if (obj is AnyLangValue anyValue)
        {
            var methods = anyValue.Metadata.MethodTable.GetAllMethods()
                .Select(m => new StringLangValue(m.MethodName))
                .Cast<LangValueType>()
                .ToList();
            return new ListLangValue(methods);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 获取类的所有字段名列表
    /// </summary>
    public static object GetClassFields(object obj)
    {
        if (obj is AnyLangValue anyValue)
        {
            var fields = anyValue.Metadata.FieldTable.GetAllFields()
                .Select(f => new StringLangValue(f.FieldName))
                .Cast<LangValueType>()
                .ToList();
            return new ListLangValue(fields);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 获取方法详细信息
    /// </summary>
    public static object GetMethodInfo(object obj, string methodName)
    {
        if (obj is AnyLangValue anyValue)
        {
            var methods = anyValue.Metadata.MethodTable.LookupMethod(methodName);
            if (methods is null || methods.Count == 0)
            {
                throw new AttributeError(anyValue, methodName, anyValue.ClassId.IdName);
            }

            // 如果有多个重载，返回第一个
            var method = methods[0];

            var tuples = new List<TupleLangValue>
            {
                new TupleLangValue([new StringLangValue("name"), new StringLangValue(method.MethodName)]),
                new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(method.IsStatic)]),
                new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!method.HasModifier(AccessModifierType.Private))]),
                new TupleLangValue([new StringLangValue("isPrivate"), new BoolLangValue(method.HasModifier(AccessModifierType.Private))]),
                new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(method.ParameterCount)])
            };
            return new DictionaryLangValue(tuples);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 获取字段详细信息
    /// </summary>
    public static object GetFieldInfo(object obj, string fieldName)
    {
        if (obj is AnyLangValue anyValue)
        {
            var field = anyValue.Metadata.FieldTable.LookupField(fieldName);
            if (field is null)
            {
                throw new AttributeError(anyValue, fieldName, anyValue.ClassId.IdName);
            }

            var tuples = new List<TupleLangValue>
            {
                new TupleLangValue([new StringLangValue("name"), new StringLangValue(field.FieldName)]),
                new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(field.IsStatic)]),
                new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!field.HasModifier(AccessModifierType.Private))]),
                new TupleLangValue([new StringLangValue("isPrivate"), new BoolLangValue(field.HasModifier(AccessModifierType.Private))])
            };
            return new DictionaryLangValue(tuples);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 动态调用方法
    /// </summary>
    public static object? InvokeMethod(object obj, string methodName, object args, VariateManager? manager)
    {
        if (obj is AnyLangValue anyValue)
        {
            if (args is not ListLangValue argsList)
            {
                throw new InvalidOperationException("参数必须是列表");
            }

            // 将 LangValueType 转换为 LangExpression
            var arguments = argsList.Values.Select(v => new ValueExpression(v)).Cast<LangExpression>().ToList();

            return anyValue.ReflectionInvokeMethod(methodName, arguments, manager);
        }

        // IL 模式下对象是动态程序集里的原生类型：按名称在 CLR 类型上查找并调用
        var nativeArguments = ToNativeArguments(args);
        var method = obj.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(candidate => candidate.Name == methodName &&
                                candidate.GetParameters().Length == nativeArguments.Length)
            .OrderBy(candidate => candidate.GetParameters().Length)
            .FirstOrDefault();
        if (method is null)
        {
            throw new InvalidOperationException($"找不到方法 {methodName}");
        }

        return method.Invoke(obj, nativeArguments);
    }

    /// <summary>
    /// 把解释器的参数列表或 IL 的原生集合统一成反射调用需要的实参数组。
    /// </summary>
    private static object?[] ToNativeArguments(object? args)
    {
        return args switch
        {
            null => [],
            ListLangValue list => list.Values.Select(UnwrapLangValue).ToArray(),
            object?[] array => array,
            System.Collections.IEnumerable enumerable => enumerable.Cast<object?>().ToArray(),
            _ => throw new InvalidOperationException("参数必须是列表")
        };
    }

    /// <summary>
    /// 把解释器的语言值转换成反射调用可用的原生值。
    /// </summary>
    private static object? UnwrapLangValue(LangValueType value) => value.GetValue();

    /// <summary>
    /// 动态获取字段值
    /// </summary>
    public static object? GetField(object obj, string fieldName)
    {
        if (obj is AnyLangValue anyValue)
        {
            return anyValue.ReflectionGetField(fieldName);
        }

        var field = FindNativeField(obj, fieldName);
        return field.GetValue(obj);
    }

    /// <summary>
    /// 在原生对象的 CLR 类型（含基类链）上查找字段。
    /// </summary>
    private static FieldInfo FindNativeField(object obj, string fieldName)
    {
        var type = obj.GetType();
        while (type is not null && type != typeof(object))
        {
            var field = type.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field is not null)
            {
                return field;
            }

            type = type.BaseType;
        }

        throw new AttributeError(new NullLangValue(), fieldName, obj.GetType().Name);
    }

    /// <summary>
    /// 动态设置字段值
    /// </summary>
    public static void SetField(object obj, string fieldName, object? value)
    {
        if (obj is AnyLangValue anyValue)
        {
            if (value is not LangValueType langValue)
            {
                throw new InvalidOperationException("值必须是 LangValueType");
            }

            anyValue.ReflectionSetField(fieldName, langValue);
            return;
        }

        var field = FindNativeField(obj, fieldName);
        var fieldType = field.FieldType;
        var nativeValue = value is LangValueType langValue2 ? UnwrapLangValue(langValue2) : value;
        if (nativeValue is not null && fieldType.IsInstanceOfType(nativeValue))
        {
            field.SetValue(obj, nativeValue);
            return;
        }

        // 数值等可转换的场景交给 Convert
        field.SetValue(obj, nativeValue is null
            ? null
            : Convert.ChangeType(nativeValue, Nullable.GetUnderlyingType(fieldType) ?? fieldType));
    }

    /// <summary>
    /// 动态创建实例
    /// </summary>
    public static object? CreateInstance(string className, object args, VariateManager? manager)
    {
        // 先按 IL 动态程序集里的原生类型创建；找不到再回退到解释器的类模板。
        var nativeType = FindNativeType(className);
        if (nativeType is not null)
        {
            var instance = Activator.CreateInstance(nativeType);
            var nativeArguments = ToNativeArguments(args);
            if (nativeArguments.Length > 0)
            {
                var init = nativeType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.Name == "init" &&
                                     method.GetParameters().Length == nativeArguments.Length)
                    .FirstOrDefault();
                init?.Invoke(instance, nativeArguments);
            }

            return instance;
        }

        if (args is not ListLangValue argsList)
        {
            throw new InvalidOperationException("参数必须是列表");
        }

        // 查找类型
        var typeTemplate = TypeTemplate.FindType(className);
        if (typeTemplate is null)
        {
            throw new NameError(new SourcePosition(), className);
        }

        // 创建实例
        var created = typeTemplate.CreateInstance(manager!);
        created.Init(manager!.Interpreter);

        // 调用 init 构造函数
        var arguments = argsList.Values.Select(v => new ValueExpression(v)).Cast<LangExpression>().ToList();
        created.CallInit(arguments, manager);

        return created;
    }

    /// <summary>
    /// 在 IL 动态程序集里按类名查找已生成的类型。
    /// </summary>
    private static Type? FindNativeType(string className)
    {
        // 同一进程里多次编译会生成多个同名动态程序集（测试逐条编译/执行），
        // 必须优先取最近创建的那个，否则可能命中上一个用例里的同名类。
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
        {
            if (assembly.GetName().Name != "Old8LangDynamicAssembly")
            {
                continue;
            }

            try
            {
                var type = assembly.GetTypes().FirstOrDefault(candidate => candidate.Name == className);
                if (type is not null)
                {
                    return type;
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 动态程序集里可能还有未完成的类型，忽略后继续
            }
        }

        return null;
    }

    /// <summary>
    /// 检查对象是否是指定类的实例
    /// </summary>
    public static bool IsInstanceOf(object obj, string className)
    {
        if (obj is AnyLangValue anyValue)
        {
            return anyValue.ClassId.IdName == className;
        }
        if (obj is BytecodeObjectInstance instance)
        {
            return instance.ClassName == className;
        }
        return false;
    }

    /// <summary>
    /// 检查对象是否有指定方法
    /// </summary>
    public static bool HasMethod(object obj, string methodName)
    {
        if (obj is AnyLangValue anyValue)
        {
            return anyValue.Metadata.MethodTable.ContainsMethod(methodName);
        }
        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm != null)
            {
                var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
                if (classMetadata != null)
                {
                    return VMReflectionHelper.HasMethod(vm, classMetadata, methodName);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 检查对象是否有指定字段
    /// </summary>
    public static bool HasField(object obj, string fieldName)
    {
        if (obj is AnyLangValue anyValue)
        {
            return anyValue.Metadata.FieldTable.ContainsField(fieldName);
        }
        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm != null)
            {
                var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
                if (classMetadata != null)
                {
                    return VMReflectionHelper.HasField(vm, classMetadata, fieldName);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 从类型名获取 TypeLangValue
    /// </summary>
    public static TypeLangValue GetType(string typeName)
    {
        var template = TypeTemplate.FindType(typeName);
        if (template is null)
        {
            throw new InvalidOperationException($"找不到类型: {typeName}");
        }
        return new TypeLangValue(template);
    }

    /// <summary>
    /// 获取所有已注册类型
    /// </summary>
    public static ListLangValue GetAllTypes()
    {
        var typeNames = TypeTemplate.GetAllRegisteredTypes();
        var typeValues = typeNames
            .Select(name => new TypeLangValue(name))
            .Cast<LangValueType>()
            .ToList();
        return new ListLangValue(typeValues);
    }

    /// <summary>
    /// 从对象获取类型
    /// </summary>
    public static TypeLangValue TypeOf(object? obj)
    {
        if (obj is AnyLangValue anyValue)
        {
            var className = anyValue.ClassId.IdName;
            var template = TypeTemplate.FindType(className);
            return template is not null ? new TypeLangValue(template) : new TypeLangValue(className);
        }

        if (obj is not null)
        {
            var nativeTemplate = TypeTemplate.FindType(obj.GetType().Name);
            if (nativeTemplate is not null)
            {
                return new TypeLangValue(nativeTemplate);
            }
        }

        // 非类实例（基本类型、枚举值、集合、元组、null 等）与解释器/VM 统一返回语言类型名。
        return new TypeLangValue(VMReflectionHelper.DescribeValueType(obj));
    }

    /// <summary>
    /// 获取类型的详细信息
    /// </summary>
    public static DictionaryLangValue GetTypeInfo(string typeName)
    {
        var template = TypeTemplate.FindType(typeName);
        if (template is null)
        {
            throw new InvalidOperationException($"找不到类型: {typeName}");
        }

        // 需要一个 VariateManager 来构建元数据
        // 在 IL 模式下，我们需要从当前上下文获取
        // 这里暂时创建一个临时的 manager
        var manager = new VariateManager();
        var metadata = template.BuildMetadata(manager);

        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(metadata.ClassName)]),
            new TupleLangValue([new StringLangValue("isInterface"), new BoolLangValue(metadata.IsInterface)]),
            new TupleLangValue([new StringLangValue("isAbstract"), new BoolLangValue(metadata.IsAbstract)]),
            new TupleLangValue([new StringLangValue("isMixin"), new BoolLangValue(metadata.IsMixin)]),
            new TupleLangValue([
                new StringLangValue("baseClass"),
                metadata.ParentClassName is not null
                    ? new StringLangValue(metadata.ParentClassName)
                    : new NullLangValue()
            ]),
            new TupleLangValue([
                new StringLangValue("interfaces"),
                new ListLangValue(
                    metadata.InterfaceNames
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([
                new StringLangValue("mixins"),
                new ListLangValue(
                    metadata.MixinNames
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([
                new StringLangValue("methods"),
                new ListLangValue(
                    metadata.MethodTable.GetAllMethodNames()
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([
                new StringLangValue("fields"),
                new ListLangValue(
                    metadata.FieldTable.GetAllFieldNames()
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([new StringLangValue("isGeneric"), new BoolLangValue(template.IsGeneric)])
        };

        return new DictionaryLangValue(tuples);
    }

}

/// <summary>
/// ValueExpression - 用于包装 LangValueType 为 LangExpression
/// </summary>
internal class ValueExpression(LangValueType value) : LangExpression
{
    public override TResult Accept<TResult>(AST.Visitor.IVisitor<TResult> visitor)
    {
        // ValueExpression 是一个辅助类，直接返回值
        return (TResult)(object)value;
    }
}
