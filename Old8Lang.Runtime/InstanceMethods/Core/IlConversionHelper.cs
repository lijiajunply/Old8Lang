namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL 模式下 <c>as</c> 的运行期转换（沿用解释器的 <c>Converse</c> 语义）。
/// </summary>
/// <remarks>
/// 静态类型为 <c>object</c> 的值可能装着字符串/数字；之前直接
/// <c>unbox.any int</c>，`"1" as int` 抛 InvalidCastException。
/// 这里包装成语言值后调用 <see cref="LangValueType.Converse"/>，
/// 与解释器一致（字符串会解析、无法解析时抛 FORMAT_ERROR）。
/// </remarks>
public static class IlConversionHelper
{
    /// <summary>把 <paramref name="value"/> 转换为语言类型名对应的值。</summary>
    public static object? ConvertTo(object? value, string typeName)
    {
        var wrapped = IlValueBridge.Wrap(value);
        var target = new Old8Lang.AST.Expression.Value.TypeLangValue(typeName);
        var result = wrapped.Converse(target, new Old8Lang.Interpreter.VariateManager());
        return IlValueBridge.Unwrap(result);
    }
}
