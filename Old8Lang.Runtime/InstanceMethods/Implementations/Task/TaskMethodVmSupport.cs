using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.InstanceMethods.Implementations.Task;

/// <summary>
/// Task 实例方法在虚拟机模式下的公共逻辑。
/// </summary>
internal static class TaskMethodVmSupport
{
    /// <summary>
    /// 把 VM 函数对象（ClosureValue / FunctionMetadata）的返回值转换成 Old8Lang 值。
    /// </summary>
    /// <remarks>
    /// VM 求值栈上既可能是原生值（int / string / bool / ...），也可能已经是 <see cref="LangValueType"/>。
    /// 后者必须原样返回：<c>LangValueType.ObjToValue</c> 对它没有专门分支，会包成
    /// <c>NativeAnyLangValue</c>，打印出来是 <c>NativeObject(42)</c> 而不是 <c>42</c>。
    /// </remarks>
    public static LangValueType ToLangValue(object? raw) =>
        raw as LangValueType ?? LangValueType.ObjToValue(raw);
}
