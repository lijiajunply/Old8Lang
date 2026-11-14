using Old8Lang.AST.Expression;

namespace Old8Lang.AST.Statement;

/// <summary>
/// IL 模式下函数返回类型的统一校正。
/// </summary>
/// <remarks>
/// 可空值类型注解（<c>int?</c> / <c>bool?</c> / <c>double?</c> / <c>char?</c>）在 IL 模式里
/// 没有 CLR 载体：类型注解被解析成基础值类型，无法表示 <c>null</c>。函数体执行
/// <c>return null</c> 时返回路径会对空引用做 <c>unbox.any</c>，运行期抛
/// <c>NullReferenceException</c>；解释器与虚拟机则正常返回 <c>null</c>。
/// 因此这类返回类型统一改用 <c>object</c> 承载：具体值由装箱承载，<c>null</c> 直接是空引用，
/// 与「可空」的语义一致。引用类型（<c>string?</c>）与联合类型（<c>int? | string?</c>）
/// 本来就能表示 <c>null</c>，不受影响。
/// </remarks>
internal static class IlReturnTypeHelper
{
    /// <summary>
    /// 按返回类型注解校正 IL 方法/委托的返回类型。
    /// </summary>
    /// <param name="id">函数标识符（携带返回类型注解）。</param>
    /// <param name="resolvedType">按注解解析出的 CLR 类型。</param>
    public static Type Resolve(LangId? id, Type? resolvedType)
    {
        if (resolvedType is null)
        {
            return typeof(object);
        }

        var annotation = id?.AssumptionType?.Trim();
        if (string.IsNullOrEmpty(annotation) || !annotation.EndsWith('?'))
        {
            return resolvedType;
        }

        // 联合类型等在前面已解析为 object/引用类型；这里只处理「单个可空值类型」。
        // Nullable<T> 本身能承载 null，不需要再退化。
        return resolvedType.IsValueType && Nullable.GetUnderlyingType(resolvedType) is null
            ? typeof(object)
            : resolvedType;
    }
}
