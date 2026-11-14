namespace Old8Lang.Runtime;

/// <summary>调用运行期才可确定类型的委托。</summary>
public static class DynamicDelegateInvoker
{
    /// <summary>调用委托，非委托值会抛出异常。</summary>
    public static object? Invoke(object? callable, object?[] arguments)
    {
        if (callable is not Delegate function)
        {
            throw new InvalidOperationException("表达式结果不是可调用的函数值");
        }

        return function.DynamicInvoke(arguments);
    }
}
