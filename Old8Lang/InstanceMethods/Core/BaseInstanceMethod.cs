using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 实例方法抽象基类 - 提供通用的参数验证和错误处理
/// </summary>
public abstract class BaseInstanceMethod : IInstanceMethod
{
    /// <summary>
    /// 方法名称（支持多个别名）
    /// </summary>
    public abstract string[] Names { get; }

    /// <summary>
    /// 目标类型（此方法适用的类型）
    /// </summary>
    public abstract Type TargetType { get; }

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
    /// 解释器模式执行
    /// </summary>
    public LangValueType Execute(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        ValidateInstance(instance, position);
        ValidateParameterCount(parameters.Count, position);
        return ExecuteInternal(instance, parameters, manager, position);
    }

    /// <summary>
    /// 编译器模式生成 IL 代码
    /// </summary>
    public void GenerateIl(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ValidateParameterCount(parameters.Count, position);
        GenerateIlInternal(instance, parameters, ilGenerator, local, position);
    }

    /// <summary>
    /// 编译器模式获取返回类型
    /// </summary>
    public Type GetReturnType(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return GetReturnTypeInternal(instanceType, parameters, local);
    }

    /// <summary>
    /// 字节码模式执行（虚拟机模式）
    /// </summary>
    public object? ExecuteInVM(object? instance, object?[] arguments)
    {
        ValidateInstanceForVM(instance);
        ValidateParameterCountForVM(arguments.Length);
        return ExecuteInVMInternal(instance, arguments);
    }

    /// <summary>
    /// 解释器模式执行的内部实现
    /// </summary>
    protected abstract LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position);

    /// <summary>
    /// 编译器模式生成 IL 代码的内部实现
    /// </summary>
    protected abstract void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position);

    /// <summary>
    /// 编译器模式获取返回类型的内部实现
    /// </summary>
    protected abstract Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local);

    /// <summary>
    /// 字节码模式执行的内部实现
    /// </summary>
    protected abstract object? ExecuteInVMInternal(object? instance, object?[] arguments);

    /// <summary>
    /// 验证实例类型
    /// </summary>
    protected void ValidateInstance(LangValueType instance, SourcePosition position)
    {
        if (instance == null)
        {
            throw new InvalidOperationError(position, $"{PrimaryName} 方法的实例不能为 null");
        }

        var instanceType = instance.GetType();
        if (!TargetType.IsAssignableFrom(instanceType))
        {
            throw new InvalidOperationError(position,
                $"{PrimaryName} 方法只能在 {TargetType.Name} 类型上调用，但实际类型是 {instanceType.Name}");
        }
    }

    /// <summary>
    /// 验证实例类型（字节码模式）
    /// </summary>
    protected void ValidateInstanceForVM(object? instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance), $"{PrimaryName} 方法的实例不能为 null");
        }

        var instanceType = instance.GetType();

        // 直接类型匹配
        if (TargetType.IsAssignableFrom(instanceType))
        {
            return;
        }

        // 检查类型等价（VM 模式下的类型映射）
        if (IsEquivalentType(instanceType, TargetType))
        {
            return;
        }

        throw new ArgumentException(
            $"{PrimaryName} 方法只能在 {TargetType.Name} 类型上调用，但实际类型是 {instanceType.Name}",
            nameof(instance));
    }

    /// <summary>
    /// 检查两个类型是否等价（用于 VM 模式）
    /// </summary>
    private static bool IsEquivalentType(Type actualType, Type targetType)
    {
        // object[] 等价于 ListLangValue
        if (actualType == typeof(object[]) && targetType == typeof(ListLangValue))
        {
            return true;
        }

        // List<object?> 等价于 ListLangValue
        if (actualType == typeof(List<object?>) && targetType == typeof(ListLangValue))
        {
            return true;
        }

        // object[] 和 List<object?> 等价于 ILangList 接口
        if (targetType == typeof(ILangList))
        {
            if (actualType == typeof(object[]) || actualType == typeof(List<object?>))
            {
                return true;
            }
        }

        // Dictionary<object, object?> 等价于 DictionaryLangValue
        if (actualType == typeof(Dictionary<object, object?>) && targetType == typeof(DictionaryLangValue))
        {
            return true;
        }

        // string 等价于 StringLangValue
        if (actualType == typeof(string) && targetType == typeof(StringLangValue))
        {
            return true;
        }

        // int 等价于 IntLangValue
        if (actualType == typeof(int) && targetType == typeof(IntLangValue))
        {
            return true;
        }

        // double 等价于 DoubleLangValue
        if (actualType == typeof(double) && targetType == typeof(DoubleLangValue))
        {
            return true;
        }

        // bool 等价于 BoolLangValue
        if (actualType == typeof(bool) && targetType == typeof(BoolLangValue))
        {
            return true;
        }

        // char 等价于 CharLangValue
        if (actualType == typeof(char) && targetType == typeof(CharLangValue))
        {
            return true;
        }

        // Tuple<object?, object?> 等价于 TupleLangValue (VM 模式下的元组表示)
        if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Tuple<,>) &&
            targetType == typeof(TupleLangValue))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 验证参数数量
    /// </summary>
    protected void ValidateParameterCount(int count, SourcePosition position)
    {
        if (count < MinParameterCount)
        {
            throw new ArgumentError(position,
                $"{PrimaryName} 方法需要至少 {MinParameterCount} 个参数，但只提供了 {count} 个");
        }

        if (MaxParameterCount != -1 && count > MaxParameterCount)
        {
            throw new ArgumentError(position,
                $"{PrimaryName} 方法最多接受 {MaxParameterCount} 个参数，但提供了 {count} 个");
        }
    }

    /// <summary>
    /// 验证参数数量（字节码模式）
    /// </summary>
    protected void ValidateParameterCountForVM(int count)
    {
        if (count < MinParameterCount)
        {
            throw new ArgumentException($"{PrimaryName} 方法需要至少 {MinParameterCount} 个参数，但只提供了 {count} 个");
        }

        if (MaxParameterCount != -1 && count > MaxParameterCount)
        {
            throw new ArgumentException($"{PrimaryName} 方法最多接受 {MaxParameterCount} 个参数，但提供了 {count} 个");
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
    /// 获取主方法名称（第一个名称）
    /// </summary>
    protected string PrimaryName => Names[0];
}
