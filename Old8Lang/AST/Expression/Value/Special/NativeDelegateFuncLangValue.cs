using System.Reflection;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Visitor;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Utilities;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 原生委托函数值类
/// 用于保存 P/Invoke 委托实例和方法信息
/// </summary>
public class NativeDelegateFuncLangValue(
    string idName,
    Delegate delegateInstance,
    SourcePosition position = default)
    : ImportInfo(position)
{
    public readonly LangId Id = new(idName);
    private readonly Delegate _delegateInstance = delegateInstance;

    /// <summary>
    /// 执行原生委托函数
    /// </summary>
    public LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
    {
        // 计算所有参数的值
        var values = ids.Select(expr => expr.Run(variateManagerFunc)).ToList();
        var convertedValues = Apis.ListToObjects(values).ToArray();

        object? result;
        try
        {
            // 入栈：记录函数调用
            Old8Exception.PushCallStack(_delegateInstance.Method.Name, Position);

            // 使用委托缓存优化反射调用性能
            // 重要: 传递委托实例作为 instance 参数
            result = MethodInvokerCache.Invoke(_delegateInstance.Method, _delegateInstance, convertedValues);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // 转换 .NET 异常为 Old8Lang 异常
            var innerException = ex.InnerException;

            // 基础异常信息
            string errorMessage = innerException.Message;
            string errorCode = "RUNTIME_ERROR";

            // 直接创建 Old8Exception，保留原始异常作为 innerException
            throw new Old8Exception(
                errorCode,
                errorMessage,
                Position,
                null,
                null,
                null,
                null,
                innerException);
        }
        finally
        {
            // 出栈：函数调用结束
            Old8Exception.PopCallStack();
        }

        // 转换返回值
        if (result is null)
            return new VoidLangValue();

        return ObjToValue(result);
    }

    /// <summary>
    /// 执行原生委托函数（支持命名参数）
    /// </summary>
    public LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> positionalArgs,
        List<NamedArgument>? namedArgs, SourcePosition callPosition, object? obj = null)
    {
        // 如果没有命名参数，使用原有的逻辑
        if (namedArgs is null || namedArgs.Count == 0)
        {
            return Run(variateManagerFunc, positionalArgs, obj);
        }

        // 重新排序参数以支持命名参数
        var reorderedArgs = ReorderArgumentsWithNamedParameters(positionalArgs, namedArgs, callPosition);

        // 使用重新排序后的参数调用原有方法
        return Run(variateManagerFunc, reorderedArgs, obj);
    }

    /// <summary>
    /// 将位置参数和命名参数重新排序为完整的位置参数列表
    /// </summary>
    private List<LangExpression> ReorderArgumentsWithNamedParameters(
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPosition)
    {
        // 获取委托方法的参数信息
        var delegateMethod = _delegateInstance.Method;
        var methodParams = delegateMethod.GetParameters();

        // 1. 验证命名参数的合法性
        var seenNames = new HashSet<string>();
        foreach (var namedArg in namedArgs)
        {
            if (!seenNames.Add(namedArg.Name))
            {
                throw new ArgumentError(namedArg.Position,
                    $"命名参数 '{namedArg.Name}' 重复指定");
            }
        }

        // 2. 创建参数槽位数组
        var paramSlots = new LangExpression?[methodParams.Length];
        var parameterFilled = new bool[methodParams.Length];

        // 3. 填充位置参数
        for (int i = 0; i < positionalArgs.Count; i++)
        {
            if (i >= methodParams.Length)
            {
                throw new ArgumentError(callPosition,
                    $"函数 '{Id.IdName}' 期望最多 {methodParams.Length} 个参数，但位置参数提供了 {positionalArgs.Count} 个");
            }

            paramSlots[i] = positionalArgs[i];
            parameterFilled[i] = true;
        }

        // 4. 填充命名参数
        foreach (var namedArg in namedArgs)
        {
            // 查找参数索引
            int paramIndex = -1;
            for (int i = 0; i < methodParams.Length; i++)
            {
                if (methodParams[i].Name == namedArg.Name)
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
            {
                throw new ArgumentError(namedArg.Position,
                    $"函数 '{Id.IdName}' 没有名为 '{namedArg.Name}' 的参数");
            }

            // 检查是否已经通过位置参数提供
            if (parameterFilled[paramIndex])
            {
                throw new ArgumentError(namedArg.Position,
                    $"参数 '{namedArg.Name}' 已经通过位置参数提供，不能重复指定");
            }

            paramSlots[paramIndex] = namedArg.Value;
            parameterFilled[paramIndex] = true;
        }

        // 5. 验证所有必需参数都已提供
        for (int i = 0; i < methodParams.Length; i++)
        {
            if (!parameterFilled[i] && !methodParams[i].HasDefaultValue)
            {
                throw new ArgumentError(callPosition,
                    $"函数 '{Id.IdName}' 的必需参数 '{methodParams[i].Name}' (第{i + 1}个参数) 未提供值");
            }
        }

        // 6. 转换为列表并返回（过滤掉未填充的可选参数）
        return paramSlots.OfType<LangExpression>().ToList();
    }

    public override string ToString()
    {
        return $"native delegate function: {Id.IdName}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is NativeDelegateFuncLangValue other)
        {
            return Id.IdName == other.Id.IdName && _delegateInstance == other._delegateInstance;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id.IdName, _delegateInstance);
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // NativeDelegateFuncLangValue 是包装原生 .NET 委托的特殊类型
        // 它不是标准的 AST 节点，不应该通过 Visitor 模式访问
        throw new InvalidOperationException("NativeDelegateFuncLangValue 是原生委托包装类，不支持 Visitor 模式访问");
    }
}