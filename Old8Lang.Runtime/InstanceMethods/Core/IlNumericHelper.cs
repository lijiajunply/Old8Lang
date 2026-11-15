namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL / 虚拟机数值实例方法的共享校验（与标准库 <c>MathLib</c> 保持同一语义）。
/// </summary>
/// <remarks>
/// 历史语言用法允许 <c>number.Sqrt()</c>；此前 IL 直接调 <c>Math.Sqrt</c>，
/// 负数得到 NaN，而 <c>Math.Sqrt(-1)</c> 会抛「平方根函数的参数必须是非负数」。
/// 这里统一走校验版本，避免同一函数在两种写法下语义不同。
/// </remarks>
public static class IlNumericHelper
{
    /// <summary>平方根；负数抛 <see cref="ArgumentOutOfRangeException"/>。</summary>
    public static double Sqrt(double value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "平方根函数的参数必须是非负数");
        }

        return Math.Sqrt(value);
    }
}
