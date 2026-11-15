namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL 模式下两个 <c>object</c> 操作数的大小比较（运行期按语言值语义分派）。
/// </summary>
/// <remarks>
/// <c>a &lt; b</c> 两侧静态类型都是 object（例如数组元素与从元素赋值的变量）时，
/// IL 之前落到数值/引用比较分支，恒为 false。这里包装成语言值后调用
/// <see cref="LangValueType.Less"/> 等实现，与解释器/虚拟机一致。
/// </remarks>
public static class IlDynamicComparison
{
    /// <summary>按运算符比较两个动态操作数。</summary>
    public static bool Evaluate(string op, object? left, object? right)
    {
        var a = IlValueBridge.Wrap(left);
        var b = IlValueBridge.Wrap(right);

        return op switch
        {
            "<" => a.Less(b),
            ">" => a.Greater(b),
            "<=" => a.LessEqual(b),
            ">=" => a.GreaterEqual(b),
            _ => throw new ArgumentException($"不支持的比较运算符: {op}")
        };
    }
}
