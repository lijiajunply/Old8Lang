using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 静态类 API（Task / Thread / Assert）
/// </summary>
/// <remarks>
/// 解释器把 <c>Task</c> / <c>Thread</c> / <c>Assert</c> 注册为运行期全局对象
/// （<c>LangInterpreter</c>），静态方法分发由各类的 <c>Dot(Instance, manager)</c> 完成。
/// 字节码模式没有这套对象，因此在编译期把 <c>类名.方法(参数)</c> 改写成带限定名的原生调用，
/// 由 <c>VirtualMachine</c> 在运行期分发。支持的方法集合见 <see cref="VmStaticClassRegistry"/>。
///
/// 注意 <c>Task.Delay(100)</c> 的语法树是
/// <c>Operation{ Dot, Left=LangId("Task"), Right=Instance("Delay",[100]) }</c>，
/// 不是 <c>FunctionCallExpression</c>，所以拦截点在本类的 <c>VisitOperation</c>。
/// </remarks>
public partial class BytecodeVisitor
{
    /// <summary>
    /// 尝试把静态类访问编译成静态类调用。
    /// </summary>
    /// <returns>该节点已被处理时返回 true；返回 false 时调用方按普通成员访问继续处理</returns>
    private bool TryCompileStaticClassAccess(Operation node)
    {
        if (node.Left is not LangId { IdName: var className } || !VmStaticClassRegistry.IsStaticClass(className))
        {
            return false;
        }

        // 用户自己定义的变量/字段/类与静态类同名时，按普通成员访问处理：
        // 解释器是在运行期按左操作数的实际类型分发的，同名声明自然会走用户的那份。
        if (IsShadowedName(className))
        {
            return false;
        }

        if (node.Right is Instance instance)
        {
            string methodName = instance.Id.IdName;

            if (!VmStaticClassRegistry.TryCanonicalize(className, methodName, out _, out var qualifiedName))
            {
                throw new VmUnsupportedError(node,
                    $"{className}.{methodName}（{className} 在虚拟机模式下支持：" +
                    $"{VmStaticClassRegistry.DescribeSupportedMethods(className)}）");
            }

            if (instance.NamedArgs is { Count: > 0 })
            {
                // 普通成员访问与解释器都会忽略命名参数，静默忽略只会退化成
                // “期望 N 个参数但提供了 0 个”，不如直接说明原因。
                throw new VmUnsupportedError(node, $"静态类方法 {qualifiedName} 的命名参数");
            }

            foreach (var argument in instance.Ids)
            {
                argument.Accept(this);
            }

            Emit(OpCode.CallNative, new object[] { instance.Ids.Count, qualifiedName });
            return true;
        }

        // 裸引用：Task、Task.Delay、Task.Factory、Thread.CurrentThread ...
        // 这些形式需要在栈上放一个函数值或静态类对象，虚拟机没有对应的值表示。
        throw new VmUnsupportedError(node,
            $"把静态类 {className} 或其方法当作值使用（{className} 在虚拟机模式下只能写成 " +
            $"{className}.方法(...)，支持：{VmStaticClassRegistry.DescribeSupportedMethods(className)}）");
    }

    /// <summary>
    /// 名称是否被用户自己的声明遮蔽。
    /// </summary>
    private bool IsShadowedName(string name)
    {
        return _compiler.IsLocalVariable(name)
               || _compiler.IsGlobalVariable(name)
               || _compiler.IsClassName(name)
               || _compiler.IsCapturedVariable(name)
               || _compiler.IsClassField(name);
    }
}
