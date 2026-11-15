using Old8Lang.AST.Expression;

namespace Old8Lang.Compiler.Helpers;

/// <summary>
/// 携带原始语言值的抛出载体。
/// </summary>
/// <remarks>
/// IL 模式下 <c>throw expr</c> 原先只有「ToString 后 <c>new Exception(string)</c>」这一条路，
/// 被抛出的值（字符串、字典、类实例……）在抛出点就丢掉了，catch 只能拿到
/// <see cref="ExceptionWrapper"/>。这里把原始语言值挂在异常上，
/// catch 处再用 <see cref="ExceptionWrapper.Bind"/> 解包回原生值，
/// 让 <c>try { ... } catch (e) { x &lt;- e }</c> 与解释器的语义一致。
/// </remarks>
public sealed class LangThrownException(LangValueType value) : Exception(value.ToDisplayString())
{
    /// <summary>被抛出的原始语言值。</summary>
    public LangValueType Value { get; } = value;
}
