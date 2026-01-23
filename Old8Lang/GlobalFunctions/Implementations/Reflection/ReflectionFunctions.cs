using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetClassName 函数 - 获取对象的类名
/// </summary>
public sealed class GetClassNameFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetClassName"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        return new StringLangValue(anyValue.ClassId.IdName);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetClassName(object)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.GetClassName));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];

        if (obj is BytecodeObjectInstance instance)
        {
            return instance.ClassName;
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}

/// <summary>
/// GetClassMethods 函数 - 获取类的所有方法名列表
/// </summary>
public sealed class GetClassMethodsFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetClassMethods"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        var methods = anyValue.Metadata.MethodTable.GetAllMethods()
            .Select(m => new StringLangValue(m.MethodName))
            .Cast<LangValueType>()
            .ToList();
        return new ListLangValue(methods);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetClassMethods(object)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.GetClassMethods));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];

        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm == null)
            {
                throw new InvalidOperationException("无法获取当前虚拟机实例");
            }

            var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
            if (classMetadata == null)
            {
                throw new InvalidOperationException($"找不到类 {instance.ClassName} 的元数据");
            }

            var methodNames = VMReflectionHelper.GetAllMethodNames(classMetadata);
            // 转换为 List<object?> 以便虚拟机正确处理
            return methodNames.Cast<object?>().ToList();
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}

/// <summary>
/// GetClassFields 函数 - 获取类的所有字段名列表
/// </summary>
public sealed class GetClassFieldsFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetClassFields"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        var fields = anyValue.Metadata.FieldTable.GetAllFields()
            .Select(f => new StringLangValue(f.FieldName))
            .Cast<LangValueType>()
            .ToList();
        return new ListLangValue(fields);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetClassFields(object)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.GetClassFields));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];

        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm == null)
            {
                throw new InvalidOperationException("无法获取当前虚拟机实例");
            }

            var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
            if (classMetadata == null)
            {
                throw new InvalidOperationException($"找不到类 {instance.ClassName} 的元数据");
            }

            var fieldNames = VMReflectionHelper.GetAllFieldNames(classMetadata);
            // 转换为 List<object?> 以便虚拟机正确处理
            return fieldNames.Cast<object?>().ToList();
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}

/// <summary>
/// GetMethodInfo 函数 - 获取方法详细信息
/// </summary>
public sealed class GetMethodInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetMethodInfo"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var methodName = ((StringLangValue)results[1]).Value;

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

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

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载方法名参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetMethodInfo(object, string)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.GetMethodInfo));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];
        var methodName = (string)arguments[1]!;

        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm == null)
            {
                throw new InvalidOperationException("无法获取当前虚拟机实例");
            }

            var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
            if (classMetadata == null)
            {
                throw new InvalidOperationException($"找不到类 {instance.ClassName} 的元数据");
            }

            var method = VMReflectionHelper.FindMethod(classMetadata, methodName);
            if (method == null)
            {
                throw new InvalidOperationException($"找不到方法 {methodName}");
            }

            var tuples = VMReflectionHelper.CreateMethodInfoTuples(method);
            // 转换为 Dictionary<object, object?> 以便虚拟机正确处理
            var dict = new Dictionary<object, object?>();
            foreach (var (key, value) in tuples)
            {
                dict[key] = value;
            }
            return dict;
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}

/// <summary>
/// GetFieldInfo 函数 - 获取字段详细信息
/// </summary>
public sealed class GetFieldInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetFieldInfo"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var fieldName = ((StringLangValue)results[1]).Value;

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

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

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载字段名参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetFieldInfo(object, string)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.GetFieldInfo));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];
        var fieldName = (string)arguments[1]!;

        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm == null)
            {
                throw new InvalidOperationException("无法获取当前虚拟机实例");
            }

            var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
            if (classMetadata == null)
            {
                throw new InvalidOperationException($"找不到类 {instance.ClassName} 的元数据");
            }

            var field = VMReflectionHelper.FindField(classMetadata, fieldName);
            if (field == null)
            {
                throw new InvalidOperationException($"找不到字段 {fieldName}");
            }

            var tuples = VMReflectionHelper.CreateFieldInfoTuples(field);
            // 转换为 Dictionary<object, object?> 以便虚拟机正确处理
            var dict = new Dictionary<object, object?>();
            foreach (var (key, value) in tuples)
            {
                dict[key] = value;
            }
            return dict;
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}
