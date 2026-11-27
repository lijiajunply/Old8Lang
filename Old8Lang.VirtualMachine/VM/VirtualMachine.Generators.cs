using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Generators;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    private static object?[] CopyLocalsToObjects(VmValue[] locals, int count)
    {
        var len = Math.Min(count, locals.Length);
        var result = new object?[len];
        for (int i = 0; i < len; i++)
        {
            result[i] = locals[i].Kind == VmValueKind.UpValue
                ? locals[i].AsUpValue
                : locals[i].ToObject();
        }

        return result;
    }

    private static void RestoreLocalsFromObjects(object?[] source, VmValue[] target)
    {
        var len = Math.Min(source.Length, target.Length);
        for (int i = 0; i < len; i++)
        {
            target[i] = source[i] is UpValueCell cell
                ? VmValue.FromUpValue(cell)
                : VmValue.FromObject(source[i]);
        }
    }
    /// <summary>
    /// 恢复生成器执行
    /// </summary>
    /// <param name="generatorId">生成器ID</param>
    /// <returns>yield的值，如果生成器已完成则返回null</returns>
    public LangValueType? ResumeGenerator(int generatorId)
    {
        if (!_generators.TryGetValue(generatorId, out var generatorState))
        {
            throw new Exception($"生成器 {generatorId} 不存在");
        }

        // 检查生成器状态
        if (generatorState.Status == GeneratorStatus.Completed)
        {
            return null;
        }

        int ip;
        object?[] locals;
        Stack<LangValueType> stack;

        // 首次执行：初始化参数到局部变量
        if (generatorState.Status == GeneratorStatus.NotStarted)
        {
            ip = 0;
            locals = new object?[generatorState.Function.LocalCount];
            stack = new Stack<LangValueType>();

            // 将参数复制到局部变量（参数占据前N个局部变量槽位）
            if (generatorState.Arguments != null)
            {
                for (int i = 0; i < generatorState.Arguments.Length && i < locals.Length; i++)
                {
                    locals[i] = generatorState.Arguments[i];
                }
            }
        }
        else
        {
            // 恢复执行状态
            generatorState.RestoreState(out ip, out locals, out stack);
        }

        // 重置状态为运行中（重要：这样可以区分 yield 暂停和函数正常结束）
        generatorState.Status = GeneratorStatus.NotStarted; // 使用 NotStarted 作为"运行中"状态

        // 创建调用帧
        var frame = new CallFrame(generatorState.Function, generatorState.Function.LocalCount)
        {
            IP = ip,
            GeneratorId = generatorId
        };

        // 设置局部变量
        RestoreLocalsFromObjects(locals, frame.Locals);

        // 保存调用者的栈状态和调用栈
        var callerStack = _stack.SnapshotBottomToTop();
        var callerCallStackCount = _callStack.Count;

        // 恢复生成器的栈
        _stack.Clear();
        foreach (var item in stack.Reverse())
        {
            _stack.PushObject(item);
        }

        _callStack.Push(frame);

        try
        {
            // 继续执行指令直到下一个yield或函数结束
            while (frame.IP < generatorState.Function.Instructions.Count)
            {
                var instruction = generatorState.Function.Instructions[frame.IP];
                frame.IP++;

                try
                {
                    ExecuteInstruction(instruction, frame);

                    // 检查是否遇到了yield（通过检查状态是否变为 Suspended）
                    if (generatorState.Status == GeneratorStatus.Suspended)
                    {
                        // 返回yield的值
                        return generatorState.CurrentValue;
                    }
                }
                catch (Exception ex)
                {
                    // 异常处理
                    ExecuteDefers(frame);
                    if (!HandleException(ex, frame, generatorState.Function))
                    {
                        throw;
                    }
                }
            }

            // 函数正常结束
            generatorState.Complete();
            return null;
        }
        finally
        {
            ExecuteDefers(frame);
            _callStack.Pop();

            // 恢复调用者的栈状态
            _stack.RestoreFromBottom(callerStack);
        }
    }
}
