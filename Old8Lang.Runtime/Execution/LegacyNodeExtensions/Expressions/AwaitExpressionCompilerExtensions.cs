using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// await 表达式的 IL 编译辅助实现。
/// </summary>
internal static class AwaitExpressionCompilerExtensions
{
    /// <summary>
    /// 生成异步状态机相关的等待代码
    /// </summary>
    internal static void GenerateAsyncAwait(this AwaitExpression node, ILGenerator il, LocalManager local, Type exprType)
    {
        var generator = local.AsyncStateMachineGenerator!;
        var stateIndex = generator.GetStateIndex(node);

        // 1. 获取 Awaiter
        // 栈上已有 Task 对象

        // 如果类型是 object 或未携带静态 GetAwaiter，则运行期规范化为 Task<object>
        if (exprType == typeof(object) || exprType.GetMethod("GetAwaiter") is null)
        {
            il.Emit(OpCodes.Call,
                typeof(Old8Lang.InstanceMethods.Core.IlRuntimeFunctions)
                    .GetMethod(nameof(Old8Lang.InstanceMethods.Core.IlRuntimeFunctions.ToAwaitableTask))!);
            exprType = typeof(Task<object>);
        }

        var getAwaiterMethod = exprType.GetMethod("GetAwaiter")!;
        if (getAwaiterMethod == null)
            throw new InvalidOperationException($"Cannot find GetAwaiter on {exprType.FullName}");

        il.Emit(OpCodes.Callvirt, getAwaiterMethod);

        var awaiterType = getAwaiterMethod.ReturnType;
        var awaiterLocal = il.DeclareLocal(awaiterType);
        il.Emit(OpCodes.Stloc, awaiterLocal);

        // 2. 检查 IsCompleted
        var isCompletedProperty = awaiterType.GetProperty("IsCompleted")!;
        if (isCompletedProperty == null)
            throw new InvalidOperationException($"Cannot find IsCompleted property on {awaiterType.FullName}");

        var isCompletedGetMethod = isCompletedProperty.GetGetMethod() ?? awaiterType.GetMethod("get_IsCompleted");

        if (isCompletedGetMethod == null)
        {
            // Fallback: try to find on interface or explicit impl?
            // TaskAwaiter struct has public IsCompleted.
            throw new InvalidOperationException($"Cannot find IsCompleted getter on {awaiterType.FullName}");
        }

        il.Emit(OpCodes.Ldloca, awaiterLocal);
        il.Emit(OpCodes.Call, isCompletedGetMethod);

        var fastPathLabel = il.DefineLabel();
        il.Emit(OpCodes.Brtrue, fastPathLabel);

        // 3. 挂起 (Yield)
        generator.EmitAwaitYield(il, stateIndex, awaiterLocal);

        // 4. 恢复 (Resume) - 也是 Switch 的跳转目标
        generator.EmitAwaitResume(il, stateIndex, awaiterLocal);

        // 5. 快速路径 / 恢复后执行
        il.MarkLabel(fastPathLabel);

        // 6. 获取结果
        il.Emit(OpCodes.Ldloca, awaiterLocal);
        var getResultMethod = awaiterType.GetMethod("GetResult")!;
        if (getResultMethod == null)
            throw new InvalidOperationException($"Cannot find GetResult on {awaiterType.FullName}");

        il.Emit(OpCodes.Call, getResultMethod);

        // 如果 GetResult 返回 void (例如 Task)，我们需要压入 null 以保持表达式值的一致性
        if (getResultMethod.ReturnType == typeof(void))
        {
            il.Emit(OpCodes.Ldnull);
        }
    }
}
