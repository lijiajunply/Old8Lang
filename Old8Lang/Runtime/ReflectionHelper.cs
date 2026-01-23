using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Old8Lang.Bytecode.Metadata;

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
}

/// <summary>
/// ValueExpression - 用于包装 LangValueType 为 LangExpression
/// </summary>
internal class ValueExpression(LangValueType value) : LangExpression
{
    public override LangValueType Run(VariateManager manager) => value;

    public override void LoadIlValue(ILGenerator ilGenerator, Compiler.LocalManager local)
    {
        // 编译器模式下不应该调用这个方法
        throw new NotImplementedException();
    }

    public override Type OutputType(Compiler.LocalManager local) => value.GetType();

    public override TResult Accept<TResult>(AST.Visitor.IVisitor<TResult> visitor)
    {
        // ValueExpression 是一个辅助类，直接返回值
        return (TResult)(object)value;
    }
}
