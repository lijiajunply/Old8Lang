using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 全局函数抽象基类 - 提供通用的参数验证和错误处理
/// </summary>
public abstract class BaseGlobalFunction : IGlobalFunction
{
    /// <summary>
    /// 函数名称（支持多个别名）
    /// </summary>
    public abstract string[] Names { get; }

    /// <summary>
    /// 最小参数数量
    /// </summary>
    public abstract int MinParameterCount { get; }

    /// <summary>
    /// 最大参数数量（-1 表示不限制）
    /// </summary>
    public abstract int MaxParameterCount { get; }

    /// <summary>
    /// 解释器模式执行
    /// </summary>
    public LangValueType Execute(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        ValidateParameterCount(parameters.Count, position);
        return ExecuteInternal(parameters, manager, position);
    }

    /// <summary>
    /// 编译器模式生成 IL 代码
    /// </summary>
    public void GenerateIl(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ValidateParameterCount(parameters.Count, position);
        GenerateIlInternal(parameters, ilGenerator, local, position);
    }

    /// <summary>
    /// 编译器模式获取返回类型
    /// </summary>
    public Type GetReturnType(List<LangExpression> parameters, LocalManager local)
    {
        return GetReturnTypeInternal(parameters, local);
    }

    /// <summary>
    /// 字节码模式执行（虚拟机模式）
    /// </summary>
    public object? ExecuteInVM(object?[] arguments)
    {
        ValidateParameterCountForVM(arguments.Length);
        return ExecuteInVMInternal(arguments);
    }

    /// <summary>
    /// 解释器模式执行的内部实现
    /// </summary>
    protected abstract LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position);

    /// <summary>
    /// 编译器模式生成 IL 代码的内部实现
    /// </summary>
    protected abstract void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position);

    /// <summary>
    /// 编译器模式获取返回类型的内部实现
    /// </summary>
    protected abstract Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local);

    /// <summary>
    /// 字节码模式执行的内部实现
    /// </summary>
    protected abstract object? ExecuteInVMInternal(object?[] arguments);

    /// <summary>
    /// 验证参数数量
    /// </summary>
    protected void ValidateParameterCount(int count, SourcePosition position)
    {
        if (count < MinParameterCount)
        {
            throw new ArgumentError(position, $"{Names[0]} 函数需要至少 {MinParameterCount} 个参数，但只提供了 {count} 个");
        }

        if (MaxParameterCount != -1 && count > MaxParameterCount)
        {
            throw new ArgumentError(position, $"{Names[0]} 函数最多接受 {MaxParameterCount} 个参数，但提供了 {count} 个");
        }
    }

    /// <summary>
    /// 验证参数数量（字节码模式）
    /// </summary>
    protected void ValidateParameterCountForVM(int count)
    {
        if (count < MinParameterCount)
        {
            throw new ArgumentException($"{Names[0]} 函数需要至少 {MinParameterCount} 个参数，但只提供了 {count} 个");
        }

        if (MaxParameterCount != -1 && count > MaxParameterCount)
        {
            throw new ArgumentException($"{Names[0]} 函数最多接受 {MaxParameterCount} 个参数，但提供了 {count} 个");
        }
    }

    /// <summary>
    /// 执行参数表达式并返回结果
    /// </summary>
    protected List<LangValueType> EvaluateParameters(List<LangExpression> parameters, VariateManager manager)
    {
        return parameters.Select(p => p.Run(manager)).ToList();
    }

    /// <summary>
    /// 获取主函数名称（第一个名称）
    /// </summary>
    protected string PrimaryName => Names[0];
}
