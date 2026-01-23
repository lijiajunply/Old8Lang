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
