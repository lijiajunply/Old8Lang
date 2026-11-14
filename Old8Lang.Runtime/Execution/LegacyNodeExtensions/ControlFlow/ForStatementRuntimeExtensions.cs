using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Statement;

/// <summary>
/// ForStatement 的编译期辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static class ForStatementRuntimeExtensions
{
    /// <summary>
    /// 检测是否为简单的计数循环（可以优化）
    /// </summary>
    internal static bool IsSimpleCountingLoop(this ForStatement node)
    {
        // 简单启发式：检查是否是二元比较表达式
        return node.Condition is Operation;
    }

    internal static void EndLoopVariableShadow(
        LocalManager local,
        (string Name, LocalBuilder? PreviousLocal, bool HadPreviousLocal, Type? PreviousType, bool HadPreviousType)? shadow)
    {
        if (shadow is not { } info)
        {
            return;
        }

        if (info.HadPreviousLocal && info.PreviousLocal is not null)
        {
            local.LocalVar[info.Name] = info.PreviousLocal;
        }
        else
        {
            local.LocalVar.Remove(info.Name);
        }

        if (info.HadPreviousType && info.PreviousType is not null)
        {
            local.LocalVarTypes[info.Name] = info.PreviousType;
        }
        else
        {
            local.LocalVarTypes.Remove(info.Name);
        }
    }

    /// <summary>
    /// 为 C 风格循环变量建立独立局部作用域。
    /// </summary>
    /// <remarks>
    /// 初始化表达式在创建循环变量之前求值（解释器语义），因此读取同名外层局部或全局变量时应当看到
    /// 外层值，而不是尚未初始化的循环变量。完成后由 <see cref="EndLoopVariableShadow"/> 恢复外层绑定。
    /// </remarks>
    internal static (string Name, LocalBuilder? PreviousLocal, bool HadPreviousLocal, Type? PreviousType, bool HadPreviousType)? BeginLoopVariableShadow(
        this ForStatement node,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        var name = node.Init.Id?.IdName;
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        Type? type = null;
        if (!string.IsNullOrEmpty(node.Init.Id!.AssumptionType))
        {
            type = node.Init.Id.Accept(new TypeInferenceVisitor(local));
        }

        if (type is null || type == typeof(void))
        {
            type = node.Init.Value.Accept(new TypeInferenceVisitor(local));
        }

        if (type is null || type == typeof(void))
        {
            type = typeof(object);
        }

        // 先按未遮蔽的状态求值 `for i <- ...` 的右侧。
        node.Init.Value.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var loopLocal = ilGenerator.DeclareLocal(type);
        ilGenerator.Emit(OpCodes.Stloc, loopLocal);

        var hadPreviousLocal = local.LocalVar.TryGetValue(name, out var previousLocal);
        var hadPreviousType = local.LocalVarTypes.TryGetValue(name, out var previousType);
        local.LocalVar[name] = loopLocal;
        local.LocalVarTypes[name] = type;
        return (name, previousLocal, hadPreviousLocal, previousType, hadPreviousType);
    }
}
