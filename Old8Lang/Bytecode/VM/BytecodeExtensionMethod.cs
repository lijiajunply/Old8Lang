using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 字节码扩展方法包装器 - 在VM中执行扩展方法
/// </summary>
public class BytecodeExtensionMethod : IInstanceMethod
{
    private readonly Type _targetType;
    private readonly FunctionMetadata _function;
    private readonly VirtualMachine _vm;

    public BytecodeExtensionMethod(Type targetType, FunctionMetadata function, VirtualMachine vm)
    {
        _targetType = targetType;
        _function = function;
        _vm = vm;
    }

    public string[] Names => [_function.Name.Split('$').Last()]; // 从 "TargetType$MethodName" 提取方法名

    public Type TargetType => _targetType;

    public string[]? ParameterNames
    {
        get
        {
            // 跳过第一个参数（this），返回其余参数
            return _function.Parameters.Skip(1).ToArray();
        }
    }

    public int MinParameterCount => _function.Parameters.Count - 1; // 减去 this 参数

    public int MaxParameterCount => _function.Parameters.Count - 1; // 减去 this 参数

    public Type?[]? ParameterTypes => null; // 接受任意类型

    public Type? DeclaredReturnType => null; // 动态返回类型

    public string? Documentation => null;

    public bool CanAccept(List<LangExpression> parameters, LocalManager? local)
    {
        // 检查参数数量（不包括隐式的 this 参数）
        var expectedParamCount = _function.Parameters.Count - 1;
        return parameters.Count == expectedParamCount;
    }

    public int CalculateMatchScore(List<LangExpression> parameters, LocalManager? local)
    {
        if (!CanAccept(parameters, local))
        {
            return -1;
        }

        // 简单匹配：参数数量正确即可
        return 100;
    }

    public LangValueType Execute(
        LangValueType instance,
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        // 在解释模式下，不应该调用这个方法
        // 因为扩展方法应该通过 ExtensionMethodWrapper 执行
        throw new NotSupportedException("BytecodeExtensionMethod 不支持解释模式执行");
    }

    public void GenerateIl(
        LangExpression instance,
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        throw new NotSupportedException("BytecodeExtensionMethod 不支持编译模式");
    }

    public Type GetReturnType(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        // 返回动态类型
        return typeof(object);
    }

    public object? ExecuteInVM(object? instance, object?[] arguments)
    {
        // 在 VM 中执行扩展方法
        // 准备参数：第一个参数是 this（实例本身）
        var methodArgs = new object?[arguments.Length + 1];
        methodArgs[0] = instance;
        Array.Copy(arguments, 0, methodArgs, 1, arguments.Length);

        // 调用 VM 的公共方法来执行函数
        return _vm.ExecuteFunction(_function, methodArgs);
    }
}
