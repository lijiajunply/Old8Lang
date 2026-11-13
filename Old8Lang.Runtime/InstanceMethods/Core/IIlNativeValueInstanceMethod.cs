namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 标记接口：该实例方法的 IL 实现接收「原生 .NET 值」作为接收者。
/// </summary>
/// <remarks>
/// IL 模式下集合字面量（<c>{...}</c>、<c>[...]</c>、字典字面量）在栈上是原生
/// <c>List&lt;T&gt;</c> / <c>T[]</c> / <c>Dictionary&lt;K,V&gt;</c>，
/// 而 <see cref="Old8Lang.AST.Expression.OperationHelpers.DotOperatorILHelper"/> 默认按
/// <see cref="Old8Lang.AST.Expression.Value.LangValueType"/> 家族解析实例方法。
/// <para>
/// 实现该接口表示两件事：
/// 1. 当接收者是原生集合时，允许把接收者按「等价 LangValueType」参与实例方法解析；
/// 2. 该方法的 <c>GenerateIlInternal</c> 会自行把接收者与函数值参数转换成
///    <see cref="Old8Lang.AST.Expression.Value.LangValueType"/>（通常借助
///    <see cref="IlValueBridge"/>），并把结果再转回原生表示。
/// </para>
/// </remarks>
public interface IIlNativeValueInstanceMethod
{
}
