using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler.CodeGeneration;
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
    /// 参数名称列表（用于支持命名参数）
    /// 默认返回 null，表示不支持命名参数
    /// 子类可以重写此属性以提供参数名称
    /// </summary>
    public virtual string[]? ParameterNames => null;

    /// <summary>
    /// 最小参数数量
    /// </summary>
    public abstract int MinParameterCount { get; }

    /// <summary>
    /// 最大参数数量（-1 表示不限制）
    /// </summary>
    public abstract int MaxParameterCount { get; }

    /// <summary>
    /// 参数类型列表（用于重载解析）
    /// 默认返回 null，表示接受任意类型（向后兼容）
    /// </summary>
    public virtual Type?[]? ParameterTypes => null;

    /// <summary>
    /// 声明的返回类型（用于 IDE 显示）
    /// 默认返回 null，表示动态类型
    /// </summary>
    public virtual Type? DeclaredReturnType => null;

    /// <summary>
    /// 函数文档说明
    /// 默认返回 null
    /// </summary>
    public virtual string? Documentation => null;

    /// <summary>
    /// 检查此函数是否可以接受给定的参数列表
    /// 默认实现：检查参数数量是否在范围内，以及类型是否匹配
    /// </summary>
    public virtual bool CanAccept(List<LangExpression> parameters, LocalManager? local)
    {
        var count = parameters.Count;

        // 检查参数数量
        if (count < MinParameterCount)
            return false;

        if (MaxParameterCount != -1 && count > MaxParameterCount)
            return false;

        // 如果没有指定参数类型，接受任意类型（向后兼容）
        if (ParameterTypes == null)
            return true;

        // 检查每个参数的类型
        for (int i = 0; i < count && i < ParameterTypes.Length; i++)
        {
            var expectedType = ParameterTypes[i];
            if (expectedType == null)
                continue; // null 表示接受任意类型

            // 尝试获取参数的类型
            var paramType = TryGetParameterType(parameters[i], local);
            if (paramType == null)
                continue; // 无法确定类型时，假设匹配

            // 检查类型是否兼容
            if (!IsTypeCompatible(paramType, expectedType))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 计算此函数与给定参数列表的匹配分数
    /// 分数越高表示匹配越精确
    /// </summary>
    public virtual int CalculateMatchScore(List<LangExpression> parameters, LocalManager? local)
    {
        if (!CanAccept(parameters, local))
            return -1;

        var count = parameters.Count;

        // 如果没有指定参数类型，返回基础分数（向后兼容）
        if (ParameterTypes == null)
            return 0;

        int score = 0;
        for (int i = 0; i < count && i < ParameterTypes.Length; i++)
        {
            var expectedType = ParameterTypes[i];
            if (expectedType == null)
            {
                // 任意类型：+0 分
                continue;
            }

            var paramType = TryGetParameterType(parameters[i], local);
            if (paramType == null)
            {
                // 无法确定类型：+10 分（中等优先级）
                score += 10;
                continue;
            }

            if (paramType == expectedType)
            {
                // 精确匹配：+100 分
                score += 100;
            }
            else if (expectedType.IsAssignableFrom(paramType))
            {
                // 隐式转换匹配：+50 分
                score += 50;
            }
            else if (IsNumericConversion(paramType, expectedType))
            {
                // 数值类型转换：+30 分
                score += 30;
            }
        }

        return score;
    }

    /// <summary>
    /// 尝试获取表达式的类型
    /// </summary>
    protected virtual Type? TryGetParameterType(LangExpression expression, LocalManager? local)
    {
        if (local == null)
            return null;

        try
        {
            return expression.OutputType(local);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 检查两个类型是否兼容
    /// </summary>
    protected virtual bool IsTypeCompatible(Type sourceType, Type targetType)
    {
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        // 数值类型之间的隐式转换
        if (IsNumericConversion(sourceType, targetType))
            return true;

        return false;
    }

    /// <summary>
    /// 检查是否是数值类型之间的转换
    /// </summary>
    protected virtual bool IsNumericConversion(Type sourceType, Type targetType)
    {
        var numericTypes = new HashSet<Type>
        {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        return numericTypes.Contains(sourceType) && numericTypes.Contains(targetType);
    }

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
