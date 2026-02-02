using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Generators;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
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
        for (int i = 0; i < locals.Length && i < frame.Locals.Length; i++)
        {
            frame.Locals[i] = locals[i];
        }

        // 保存调用者的栈状态和调用栈
        var callerStack = new Stack<object?>(_stack.Reverse());
        var callerCallStackCount = _callStack.Count;

        // 恢复生成器的栈
        _stack.Clear();
        foreach (var item in stack.Reverse())
        {
            _stack.Push(item);
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
            _stack.Clear();
            foreach (var item in callerStack.Reverse())
            {
                _stack.Push(item);
            }
        }
    }
}
