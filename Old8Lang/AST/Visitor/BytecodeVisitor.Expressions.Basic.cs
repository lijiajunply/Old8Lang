using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 基础表达式
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitLangId(LangId node)
    {
        string varName = node.IdName;

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.LoadLocal, localIndex);
        }
        // 检查是否是类名（优先于实例字段检查）
        else if (_compiler.IsClassName(varName))
        {
            // 这是一个类名，应该作为全局变量加载（类元数据）
            Emit(OpCode.LoadGlobal, varName);
        }
        // 检查是否是当前类的字段
        else if (_compiler.IsClassField(varName))
        {
            // 这是一个字段访问：this.field
            // 加载 this（第一个局部变量）
            Emit(OpCode.LoadLocal, 0);

            // 加载字段
            Emit(OpCode.GetField, varName);
        }
        // 解释器模式会把下面这些静态类注册为全局对象，字节码模式尚未提供对应实现。
        // 若不在此拦截，最终只会在运行时报出 "名称 'X' 未定义"，无法反映真实原因。
        // 用户自己定义的全局同名变量优先，因此放在全局变量判断之前仅作提示。
        else if (!_compiler.IsGlobalVariable(varName) && StaticClassesUnsupportedInVm.TryGetValue(varName, out var feature))
        {
            throw new VmUnsupportedError(node, feature);
        }
        else
        {
            // 全局变量
            Emit(OpCode.LoadGlobal, varName);
        }

        return null;
    }

    /// <summary>
    /// 解释器注册了、而在字节码模式中不能作为值使用的静态类名
    /// </summary>
    /// <remarks>
    /// key 为解释器模式下 <c>LangInterpreter</c> 注册的全局对象名，value 为错误信息中展示的特性描述。
    /// 支持矩阵见 Docs/MODE_SUPPORT.md。
    ///
    /// <c>Task</c> / <c>Thread</c> / <c>Assert</c> 三者在虚拟机下部分可用，但只能写成
    /// <c>类名.方法(...)</c> 的调用形式（由 <c>VisitOperation</c> 的静态类分支处理），
    /// 裸名字落在这里说明用户在当值用（例如 <c>f &lt;- Task</c>），此时要把原因讲清楚，
    /// 而不是让它退化成运行期的「名称 'Task' 未定义」。
    /// 支持的方法清单见 <see cref="Old8Lang.Bytecode.VmStaticClassRegistry"/>。
    /// </remarks>
    private static readonly Dictionary<string, string> StaticClassesUnsupportedInVm = new()
    {
        ["Task"] = "把 Task 当作值使用（虚拟机模式下只能写成 Task.方法(...)，如 Task.Delay / Task.WhenAll）",
        ["Thread"] = "把 Thread 当作值使用（虚拟机模式下只支持 Thread.Sleep）",
        ["Assert"] = "把 Assert 当作值使用（虚拟机模式下只能写成 Assert.方法(...)，如 Assert.Equal）",
        ["TaskScheduler"] = "TaskScheduler 静态 API",
        ["TaskCompletionSource"] = "TaskCompletionSource",
        ["CancellationTokenSource"] = "CancellationTokenSource",
        ["TestRunner"] = "TestRunner API",
        ["Mock"] = "Mock API"
    };

}
