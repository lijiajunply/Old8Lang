using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetClassName(obj) - 返回类名字符串
/// </summary>
public sealed class GetClassNameFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetClassName"];
    public override string[] ParameterNames => ["obj"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        if (obj is not AnyLangValue anyValue)
            throw new InvalidOperationError(position, "对象不是类实例");
        return new StringLangValue(anyValue.ClassId.IdName);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        => throw new NotSupportedException("GetClassName 暂不支持编译模式");

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local) => typeof(string);

    protected override object? ExecuteInVMInternal(object?[] arguments) => throw new NotSupportedException("GetClassName 暂不支持 VM 模式");
}

/// <summary>
/// GetClassMethods(obj) - 返回方法名列表
/// </summary>
public sealed class GetClassMethodsFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetClassMethods"];
    public override string[] ParameterNames => ["obj"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        if (obj is not AnyLangValue anyValue)
            throw new InvalidOperationError(position, "对象不是类实例");
        var methods = anyValue.Metadata.MethodTable.GetAllMethods()
            .Select(m => (LangValueType)new StringLangValue(m.MethodName))
            .ToList();
        return new ListLangValue(methods);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        => throw new NotSupportedException("GetClassMethods 暂不支持编译模式");

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local) => typeof(object);

    protected override object? ExecuteInVMInternal(object?[] arguments) => throw new NotSupportedException("GetClassMethods 暂不支持 VM 模式");
}

/// <summary>
/// GetClassFields(obj) - 返回字段名列表
/// </summary>
public sealed class GetClassFieldsFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetClassFields"];
    public override string[] ParameterNames => ["obj"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        if (obj is not AnyLangValue anyValue)
            throw new InvalidOperationError(position, "对象不是类实例");
        var fields = anyValue.Metadata.FieldTable.GetAllFields()
            .Select(f => (LangValueType)new StringLangValue(f.FieldName))
            .ToList();
        return new ListLangValue(fields);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        => throw new NotSupportedException("GetClassFields 暂不支持编译模式");

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local) => typeof(object);

    protected override object? ExecuteInVMInternal(object?[] arguments) => throw new NotSupportedException("GetClassFields 暂不支持 VM 模式");
}

/// <summary>
/// GetMethodInfo(obj, methodName) - 返回方法信息字典
/// </summary>
public sealed class GetMethodInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetMethodInfo"];
    public override string[] ParameterNames => ["obj", "methodName"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var memberName = ((StringLangValue)results[1]).Value;
        if (obj is not AnyLangValue anyValue)
            throw new InvalidOperationError(position, "对象不是类实例");
        var methods = anyValue.Metadata.MethodTable.LookupMethod(memberName);
        if (methods is null || methods.Count == 0)
            throw new AttributeError(anyValue, memberName, anyValue.ClassId.IdName);
        var method = methods[0];
        var tuples = new List<TupleLangValue>
        {
            new([new StringLangValue("name"), new StringLangValue(method.MethodName)]),
            new([new StringLangValue("isPublic"), new BoolLangValue(!method.HasModifier(AccessModifierType.Private))]),
            new([new StringLangValue("isPrivate"), new BoolLangValue(method.HasModifier(AccessModifierType.Private))]),
            new([new StringLangValue("parameterCount"), new IntLangValue(method.ParameterCount)])
        };
        return new DictionaryLangValue(tuples);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        => throw new NotSupportedException("GetMethodInfo 暂不支持编译模式");

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local) => typeof(object);

    protected override object? ExecuteInVMInternal(object?[] arguments) => throw new NotSupportedException("GetMethodInfo 暂不支持 VM 模式");
}

/// <summary>
/// GetFieldInfo(obj, fieldName) - 返回字段信息字典
/// </summary>
public sealed class GetFieldInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetFieldInfo"];
    public override string[] ParameterNames => ["obj", "fieldName"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var memberName = ((StringLangValue)results[1]).Value;
        if (obj is not AnyLangValue anyValue)
            throw new InvalidOperationError(position, "对象不是类实例");
        var field = anyValue.Metadata.FieldTable.LookupField(memberName);
        if (field is null)
            throw new AttributeError(anyValue, memberName, anyValue.ClassId.IdName);
        var tuples = new List<TupleLangValue>
        {
            new([new StringLangValue("name"), new StringLangValue(field.FieldName)]),
            new([new StringLangValue("isPublic"), new BoolLangValue(!field.HasModifier(AccessModifierType.Private))]),
            new([new StringLangValue("isPrivate"), new BoolLangValue(field.HasModifier(AccessModifierType.Private))])
        };
        return new DictionaryLangValue(tuples);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        => throw new NotSupportedException("GetFieldInfo 暂不支持编译模式");

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local) => typeof(object);

    protected override object? ExecuteInVMInternal(object?[] arguments) => throw new NotSupportedException("GetFieldInfo 暂不支持 VM 模式");
}

/// <summary>
/// HasMethod(obj, methodName) - 检查是否有指定方法
/// </summary>
public sealed class HasMethodFunction : BaseGlobalFunction
{
    public override string[] Names => ["HasMethod"];
    public override string[] ParameterNames => ["obj", "methodName"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var memberName = ((StringLangValue)results[1]).Value;
        if (obj is AnyLangValue anyValue)
            return new BoolLangValue(anyValue.Metadata.MethodTable.ContainsMethod(memberName));
        return new BoolLangValue(false);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        => throw new NotSupportedException("HasMethod 暂不支持编译模式");

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local) => typeof(bool);

    protected override object? ExecuteInVMInternal(object?[] arguments) => throw new NotSupportedException("HasMethod 暂不支持 VM 模式");
}

/// <summary>
/// HasField(obj, fieldName) - 检查是否有指定字段
/// </summary>
public sealed class HasFieldFunction : BaseGlobalFunction
{
    public override string[] Names => ["HasField"];
    public override string[] ParameterNames => ["obj", "fieldName"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var memberName = ((StringLangValue)results[1]).Value;
        if (obj is AnyLangValue anyValue)
            return new BoolLangValue(anyValue.Metadata.FieldTable.ContainsField(memberName));
        return new BoolLangValue(false);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        => throw new NotSupportedException("HasField 暂不支持编译模式");

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local) => typeof(bool);

    protected override object? ExecuteInVMInternal(object?[] arguments) => throw new NotSupportedException("HasField 暂不支持 VM 模式");
}
