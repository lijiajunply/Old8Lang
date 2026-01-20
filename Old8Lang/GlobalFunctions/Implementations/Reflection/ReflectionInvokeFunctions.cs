using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// InvokeMethod 函数 - 动态调用方法
/// </summary>
public sealed class InvokeMethodFunction : BaseGlobalFunction
{
    public override string[] Names => ["InvokeMethod"];
    public override int MinParameterCount => 3;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var methodName = ((StringLangValue)results[1]).Value;
        var args = results[2];

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        if (args is not ListLangValue argsList)
        {
            throw new InvalidOperationError(position, "参数必须是列表");
        }

        // 将 LangValueType 转换为 LangExpression
        var arguments = argsList.Value.Select(v => (LangExpression)v).ToList();

        return anyValue.ReflectionInvokeMethod(methodName, arguments, manager);
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

        // 加载参数列表
        parameters[2].LoadIlValue(ilGenerator, local);

        // 加载 manager 参数（需要从某处获取）
        // 这里我们需要传递当前的 VariateManager
        // 由于编译模式下没有直接的 manager，我们需要使用 Interpreter.Manager
        var interpreterField = typeof(LocalManager).GetField("Interpreter");
        ilGenerator.Emit(OpCodes.Ldsfld, interpreterField!);
        var managerProperty = typeof(LangInterpreter).GetProperty("Manager");
        ilGenerator.Emit(OpCodes.Callvirt, managerProperty!.GetMethod!);

        // 调用 ReflectionHelper.InvokeMethod(object, string, object, VariateManager)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.InvokeMethod));
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
        var args = arguments[2];

        if (obj is not BytecodeObjectInstance instance)
        {
            throw new InvalidOperationException("对象不是类实例");
        }

        var vm = VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("无法获取当前虚拟机实例");
        }

        // 查找类元数据
        var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
        if (classMetadata == null)
        {
            throw new InvalidOperationException($"找不到类 {instance.ClassName} 的元数据");
        }

        // 查找方法
        var method = VMReflectionHelper.FindMethod(classMetadata, methodName);
        if (method == null)
        {
            throw new InvalidOperationException($"找不到方法 {methodName}");
        }

        // 准备参数
        List<object?> methodArgs = [instance]; // 第一个参数是 this
        if (args is List<object?> argsList)
        {
            methodArgs.AddRange(argsList);
        }

        // 调用方法
        return vm.ExecuteFunctionAndGetResult(method.Function, methodArgs.ToArray());
    }
}
