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
        var className = (string)arguments[0]!;
        var args = arguments[1];

        var vm = VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("无法获取当前虚拟机实例");
        }

        // 查找类元数据
        var classMetadata = VMReflectionHelper.GetClassMetadata(vm, className);
        if (classMetadata == null)
        {
            throw new InvalidOperationException($"找不到类 {className}");
        }

        // 创建实例
        var instance = new BytecodeObjectInstance(className);

        // 初始化字段（使用默认值）
        foreach (var field in classMetadata.Fields)
        {
            if (!field.IsStatic)
            {
                // 获取字段的默认值
                object? defaultValue = null;
                if (field.DefaultValueIndex >= 0)
                {
                    defaultValue = vm.GetConstant(field.DefaultValueIndex);
                }
                else if (field.IsDefaultNull)
                {
                    defaultValue = null;
                }
                instance.Fields[field.Name] = defaultValue;
            }
        }

        // 调用 init 构造函数（如果存在）
        var initMethod = classMetadata.Methods.FirstOrDefault(m => m.Name == "init");
        if (initMethod != null)
        {
            // 准备参数：第一个参数是 this（实例本身），后面是构造函数参数
            List<object?> methodArgs = [instance];

            // 将 args 转换为列表并添加到参数中
            if (args is object?[] argsArray)
            {
                methodArgs.AddRange(argsArray);
            }
            else if (args is List<object?> argsList)
            {
                methodArgs.AddRange(argsList);
            }

            vm.CallFunctionObject(initMethod.Function, methodArgs.ToArray());
        }

        return instance;
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
        var obj = arguments[0];
        var methodName = (string)arguments[1]!;

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
        var obj = arguments[0];
        var fieldName = (string)arguments[1]!;

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
