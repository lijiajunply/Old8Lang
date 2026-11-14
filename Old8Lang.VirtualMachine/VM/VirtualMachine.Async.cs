using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Error;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 恢复异步生成器执行。
    /// </summary>
    /// <remarks>
    /// <para><b>必须整体同步执行，不能在这里 <c>await</c>。</b>虚拟机的求值栈、调用栈与帧池都按线程存放
    /// （<c>_callStack</c> 等是 <c>ThreadLocal</c>／<c>ThreadStatic</c>），一旦某个 <c>await</c> 让线程池线程
    /// 接管后续代码，恢复出来的就是那个线程上的空栈，表现为
    /// <c>IteratorMoveNext</c> 报「求值栈为空」，或 <c>_callStack.Pop()</c> 抛 <c>Stack empty</c>。
    /// 生成器体内的 <c>await</c> 因此改为阻塞等待（与 <c>OpCode.Await</c> 的普通路径一致）。</para>
    /// </remarks>
    public LangValueType? ResumeAsyncGenerator(int asyncGeneratorId,
        CancellationToken cancellationToken = default)
    {
        if (!_asyncGenerators.TryGetValue(asyncGeneratorId, out var asyncGeneratorState))
        {
            throw new Exception($"异步生成器 {asyncGeneratorId} 不存在");
        }

        // 检查生成器状态
        if (asyncGeneratorState.Status == GeneratorStatus.Completed)
        {
            return null;
        }

        // 检查取消令牌
        cancellationToken.ThrowIfCancellationRequested();

        int ip;
        object?[] locals;
        Stack<LangValueType> stack;

        // 首次执行：初始化参数到局部变量
        if (asyncGeneratorState.Status == GeneratorStatus.NotStarted)
        {
            ip = 0;
            locals = new object?[asyncGeneratorState.Function.LocalCount];
            stack = new Stack<LangValueType>();

            // 将参数复制到局部变量（参数占据前N个局部变量槽位）
            if (asyncGeneratorState.Arguments != null)
            {
                for (int i = 0; i < asyncGeneratorState.Arguments.Length && i < locals.Length; i++)
                {
                    locals[i] = asyncGeneratorState.Arguments[i];
                }
            }
        }
        else
        {
            // 恢复执行状态
            asyncGeneratorState.RestoreState(out ip, out locals, out stack);
        }

        // 创建调用帧
        var frame = new CallFrame(asyncGeneratorState.Function, asyncGeneratorState.Function.LocalCount)
        {
            IP = ip,
            AsyncGeneratorId = asyncGeneratorId
        };

        // 设置局部变量
        for (int i = 0; i < locals.Length && i < frame.Locals.Length; i++)
        {
            frame.Locals[i] = locals[i];
        }

        // 保存调用方的求值栈：生成器执行期间 _stack 要交给生成器自己的栈使用，
        // 与同步生成器 ResumeGenerator 的做法一致。不保存的话，调用方压在栈上的
        // 迭代器对象会被生成器的栈覆盖，恢复后就再也找不回来。
        var callerStack = new Stack<object?>(_stack.Reverse());

        // 恢复栈
        _stack.Clear();
        foreach (var item in stack.Reverse())
        {
            _stack.Push(item);
        }

        _callStack.Push(frame);

        try
        {
            // 将状态重置为NotStarted,这样当生成器自然完成时可以正确识别
            // (如果执行了yield,状态会被重新设置为Suspended)
            if (asyncGeneratorState.Status == GeneratorStatus.Suspended)
            {
                asyncGeneratorState.Status = GeneratorStatus.NotStarted;
            }

            // 继续执行指令直到下一个yield或函数结束
            while (frame.IP < asyncGeneratorState.Function.Instructions.Count)
            {
                var instruction = asyncGeneratorState.Function.Instructions[frame.IP];
                frame.IP++;

                try
                {
                    // 执行一条生成器指令（同步，见 ResumeAsyncGenerator 的线程说明）
                    ExecuteAsyncGeneratorInstruction(instruction, frame, cancellationToken);

                    // 检查是否遇到了yield（IP被设置到函数末尾）
                    if (frame.IP >= asyncGeneratorState.Function.Instructions.Count)
                    {
                        // 检查是否是yield（状态为Suspended）还是函数结束
                        if (asyncGeneratorState.Status == GeneratorStatus.Suspended)
                        {
                            // 刚刚执行了yield，返回值
                            return asyncGeneratorState.CurrentValue;
                        }
                        else
                        {
                            // 函数正常结束
                            asyncGeneratorState.Complete();
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 异常处理
                    ExecuteDefers(frame);
                    if (!HandleException(ex, frame, asyncGeneratorState.Function))
                    {
                        throw;
                    }
                }
            }

            // 函数正常结束
            asyncGeneratorState.Complete();
            return null;
        }
        finally
        {
            ExecuteDefers(frame);
            _callStack.Pop();

            // 还原调用方的求值栈
            _stack.Clear();
            foreach (var item in callerStack.Reverse())
            {
                _stack.Push(item);
            }
        }
    }

    /// <summary>
    /// 异步生成器恢复的异步接口（当前实现同步完成，见 <see cref="ResumeAsyncGenerator"/>）。
    /// </summary>
    /// <remarks>
    /// 返回的 <see cref="Task"/> 在调用时即已完成，因此调用方 <c>await</c> 之后仍在同一线程上继续执行。
    /// </remarks>
    public Task<LangValueType?> ResumeAsyncGeneratorAsync(int asyncGeneratorId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ResumeAsyncGenerator(asyncGeneratorId, cancellationToken));
    }

    /// <summary>
    /// 异步执行单条指令（用于异步生成器）
    /// </summary>
    private void ExecuteAsyncGeneratorInstruction(Instruction instruction, CallFrame frame,
        CancellationToken cancellationToken)
    {
        // 检查取消令牌
        cancellationToken.ThrowIfCancellationRequested();

        // 对于大多数指令，直接调用同步版本
        // 只有异步相关的指令需要特殊处理
        switch (instruction.OpCode)
        {
            case OpCode.Await:
            {
                // 从栈中弹出 TaskLangValue (或 Task ID)
                var value = _stack.Pop();

                TaskLangValue taskLangValue;
                if (value is TaskLangValue t)
                {
                    taskLangValue = t;
                }
                else if (value is int taskId)
                {
                    taskLangValue = GetTask(taskId);
                }
                else
                {
                    throw new TypeError(GetPosition(instruction),
                        $"await 只能用于 Task，但得到 {GetValueTypeName(value)}");
                }

                // 阻塞等待，保持在当前线程上（见 ResumeAsyncGenerator 的线程说明）
                var result = taskLangValue.Await();

                // 将结果压入栈（还原成栈上的原生表示）
                _stack.Push(UnwrapToVmValue(result));
            }
                break;

            case OpCode.AwaitYield:
            {
                // 异步生成器的 yield 操作
                // 从栈中弹出要 yield 的值
                var yieldValue = _stack.Pop();

                // 获取当前异步生成器状态
                if (frame.AsyncGeneratorId.HasValue)
                {
                    var asyncGeneratorId = frame.AsyncGeneratorId.Value;
                    if (_asyncGenerators.TryGetValue(asyncGeneratorId, out var asyncGeneratorState))
                    {
                        // 保存当前执行状态
                        var stackArray = _stack.ToArray();
                        var stackCopy = new Stack<LangValueType>();
                        foreach (var item in stackArray.Reverse())
                        {
                            if (item is LangValueType langValue)
                                stackCopy.Push(langValue);
                        }

                        // 保存状态并标记为暂停
                        asyncGeneratorState.SaveState(frame.IP, frame.Locals, stackCopy);

                        // 将 yield 的值转换为 LangValueType（无损），并保留栈上的原生表示供迭代使用
                        asyncGeneratorState.CurrentValue = yieldValue as LangValueType ??
                                                           ConvertResultToLangValue(yieldValue);

                        asyncGeneratorState.Status = GeneratorStatus.Suspended;

                        // 设置 IP 到函数末尾，触发返回
                        frame.IP = asyncGeneratorState.Function.Instructions.Count;
                    }
                }
            }
                break;

            default:
                // 其他指令使用同步版本
                ExecuteInstruction(instruction, frame);
                break;
        }
    }
}
