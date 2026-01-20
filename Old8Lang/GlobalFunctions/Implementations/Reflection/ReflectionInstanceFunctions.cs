using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// CreateInstance 函数 - 动态创建实例
/// </summary>
public sealed class CreateInstanceFunction : BaseGlobalFunction
{
    public override string[] Names => ["CreateInstance"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var className = ((StringLangValue)results[0]).Value;
        var args = results[1];

        if (args is not ListLangValue argsList)
        {
            throw new InvalidOperationError(position, "参数必须是列表");
        }

        // 查找类型
        var typeTemplate = TypeTemplate.FindType(className);
        if (typeTemplate is null)
        {
            throw new NameError(position, className);
        }

        // 创建实例
        var instance = typeTemplate.CreateInstance(manager);
        instance.Init(manager.Interpreter);

        // 调用 init 构造函数
        var arguments = argsList.Value.Select(v => (LangExpression)v).ToList();
        instance.CallInit(arguments, manager);

        return instance;
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载类名参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载参数列表
        parameters[1].LoadIlValue(ilGenerator, local);

        // 加载 manager 参数
        var interpreterField = typeof(LocalManager).GetField("Interpreter");
        ilGenerator.Emit(OpCodes.Ldsfld, interpreterField!);
        var managerProperty = typeof(LangInterpreter).GetProperty("Manager");
        ilGenerator.Emit(OpCodes.Callvirt, managerProperty!.GetMethod!);

        // 调用 ReflectionHelper.CreateInstance(string, object, VariateManager)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.CreateInstance));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        throw new NotImplementedException("CreateInstance 在 VM 模式下暂不支持");
    }
}

/// <summary>
/// IsInstanceOf 函数 - 检查对象是否是指定类的实例
/// </summary>
public sealed class IsInstanceOfFunction : BaseGlobalFunction
{
    public override string[] Names => ["IsInstanceOf"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var className = ((StringLangValue)results[1]).Value;

        if (obj is AnyLangValue anyValue)
        {
            return new BoolLangValue(anyValue.ClassId.IdName == className);
        }
        return new BoolLangValue(false);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载类名参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.IsInstanceOf(object, string)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.IsInstanceOf));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return ReflectionHelper.IsInstanceOf(arguments[0]!, (string)arguments[1]!);
    }
}

/// <summary>
/// HasMethod 函数 - 检查对象是否有指定方法
/// </summary>
public sealed class HasMethodFunction : BaseGlobalFunction
{
    public override string[] Names => ["HasMethod"];
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

        if (obj is AnyLangValue anyValue)
        {
            return new BoolLangValue(anyValue.Metadata.MethodTable.ContainsMethod(methodName));
        }
        return new BoolLangValue(false);
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

        // 调用 ReflectionHelper.HasMethod(object, string)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.HasMethod));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return ReflectionHelper.HasMethod(arguments[0]!, (string)arguments[1]!);
    }
}

/// <summary>
/// HasField 函数 - 检查对象是否有指定字段
/// </summary>
public sealed class HasFieldFunction : BaseGlobalFunction
{
    public override string[] Names => ["HasField"];
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

        if (obj is AnyLangValue anyValue)
        {
            return new BoolLangValue(anyValue.Metadata.FieldTable.ContainsField(fieldName));
        }
        return new BoolLangValue(false);
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

        // 调用 ReflectionHelper.HasField(object, string)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.HasField));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return ReflectionHelper.HasField(arguments[0]!, (string)arguments[1]!);
    }
}
