using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;

namespace Old8Lang.Runtime;

/// <summary>
/// 反射运行时辅助类
/// 提供编译器模式下的反射支持
/// </summary>
public static class ReflectionHelper
{
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
    public static object InvokeMethod(object obj, string methodName, object args, VariateManager manager)
    {
        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationException("对象不是类实例");
        }

        if (args is not ListLangValue argsList)
        {
            throw new InvalidOperationException("参数必须是列表");
        }

        // 将 LangValueType 转换为 LangExpression
        var arguments = argsList.Values.Select(v => new ValueExpression(v)).Cast<LangExpression>().ToList();

        return anyValue.ReflectionInvokeMethod(methodName, arguments, manager);
    }

    /// <summary>
    /// 动态获取字段值
    /// </summary>
    public static object GetField(object obj, string fieldName)
    {
        if (obj is AnyLangValue anyValue)
        {
            return anyValue.ReflectionGetField(fieldName);
        }
        throw new InvalidOperationException("对象不是类实例");
    }

    /// <summary>
    /// 动态设置字段值
    /// </summary>
    public static void SetField(object obj, string fieldName, object value)
    {
        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationException("对象不是类实例");
        }

        if (value is not LangValueType langValue)
        {
            throw new InvalidOperationException("值必须是 LangValueType");
        }

        anyValue.ReflectionSetField(fieldName, langValue);
    }

    /// <summary>
    /// 动态创建实例
    /// </summary>
    public static object CreateInstance(string className, object args, VariateManager manager)
    {
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
        var instance = typeTemplate.CreateInstance(manager);
        instance.Init(manager.Interpreter);

        // 调用 init 构造函数
        var arguments = argsList.Values.Select(v => new ValueExpression(v)).Cast<LangExpression>().ToList();
        instance.CallInit(arguments, manager);

        return instance;
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
                    return VMReflectionHelper.HasMethod(classMetadata, methodName);
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
                    return VMReflectionHelper.HasField(classMetadata, fieldName);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 获取全局函数的参数信息
    /// </summary>
    public static object GetFunctionInfo(string functionName)
    {
        // 1. 尝试从全局函数注册表查找
        var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(functionName);
        if (globalFunc != null)
        {
            return CreateGlobalFunctionInfo(globalFunc);
        }

        // 2. 未找到函数
        throw new InvalidOperationException($"函数 '{functionName}' 不存在");
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
    public static TypeLangValue TypeOf(object obj)
    {
        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationException("对象不是类实例");
        }

        var className = anyValue.ClassId.IdName;
        var template = TypeTemplate.FindType(className);

        if (template is null)
        {
            throw new InvalidOperationException($"找不到类型: {className}");
        }

        return new TypeLangValue(template);
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
        // 在编译器模式下，我们需要从当前上下文获取
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

    /// <summary>
    /// 创建全局函数信息
    /// </summary>
    private static object CreateGlobalFunctionInfo(IGlobalFunction func)
    {
        var tuples = new List<TupleLangValue>
        {
            // 基本信息
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(func.Names[0])]),
            new TupleLangValue([
                new StringLangValue("names"),
                new ListLangValue(func.Names.Select(n => (LangValueType)new StringLangValue(n)).ToList())
            ]),

            // 参数信息
            new TupleLangValue([
                new StringLangValue("parameters"),
                CreateParameterList(func)
            ]),

            // 参数数量
            new TupleLangValue([new StringLangValue("minParameterCount"), new IntLangValue(func.MinParameterCount)]),
            new TupleLangValue([new StringLangValue("maxParameterCount"), new IntLangValue(func.MaxParameterCount)]),

            // 返回类型（全局函数通常是动态类型）
            new TupleLangValue([new StringLangValue("returnType"), new StringLangValue("object")]),

            // 标记
            new TupleLangValue([new StringLangValue("isGlobalFunction"), new BoolLangValue(true)])
        };

        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建参数列表
    /// </summary>
    private static LangValueType CreateParameterList(IGlobalFunction func)
    {
        var paramList = new List<LangValueType>();

        if (func.ParameterNames != null && func.ParameterNames.Length > 0)
        {
            foreach (var paramName in func.ParameterNames)
            {
                var paramTuples = new List<TupleLangValue>
                {
                    new TupleLangValue([new StringLangValue("name"), new StringLangValue(paramName)]),
                    new TupleLangValue([new StringLangValue("type"), new StringLangValue("object")])
                };
                paramList.Add(new DictionaryLangValue(paramTuples));
            }
        }

        return new ListLangValue(paramList);
    }
}

/// <summary>
/// ValueExpression - 用于包装 LangValueType 为 LangExpression
/// </summary>
internal class ValueExpression(LangValueType value) : LangExpression
{
    public override LangValueType Run(VariateManager manager) => value;

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器模式下不应该调用这个方法
        throw new NotImplementedException();
    }

    public override Type OutputType(LocalManager local) => value.GetType();

    public override TResult Accept<TResult>(AST.Visitor.IVisitor<TResult> visitor)
    {
        // ValueExpression 是一个辅助类，直接返回值
        return (TResult)(object)value;
    }
}
