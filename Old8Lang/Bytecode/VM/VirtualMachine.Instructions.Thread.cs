using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行Thread 支持指令
    /// </summary>
    private void ExecuteThreadOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.ThreadCreate:
            {
                // 栈顶: func (int/string/Metadata), argCount (int)
                var funcObj = _stack.Pop();
                var argCount = Convert.ToInt32(_stack.Pop());

                // 弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                FunctionMetadata function;
                if (funcObj is int funcIndex)
                {
                    function = _bytecodeFile.Functions[funcIndex];
                }
                else if (funcObj is string funcName)
                {
                    function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName)
                               ?? throw new MethodNotFoundError(GetPosition(instruction), funcName);
                }
                else if (funcObj is FunctionMetadata funcMeta)
                {
                    function = funcMeta;
                }
                else
                {
                    throw new TypeError(GetPosition(instruction),
                        $"Invalid function for ThreadCreate: {funcObj?.GetType().Name}");
                }

                // 创建线程
                var threadId = Concurrency.ResourceManager.CreateThread(() =>
                {
                    // Create new VM instance
                    var threadVm = new VirtualMachine(_bytecodeFile, _baseDirectory);
                    foreach (var kvp in _globals) threadVm._globals[kvp.Key] = kvp.Value;

                    threadVm.CallFunction(function, args);
                });

                _stack.Push(threadId);
            }
                break;

            case OpCode.ThreadStart:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.StartThread(threadId);
            }
                break;

            case OpCode.ThreadJoin:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                var result = Concurrency.ResourceManager.JoinThread(threadId);
                _stack.Push(result);
            }
                break;

            case OpCode.ThreadIsAlive:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                var isAlive = Concurrency.ResourceManager.IsThreadAlive(threadId);
                _stack.Push(isAlive);
            }
                break;

            case OpCode.ThreadDispose:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.DisposeThread(threadId);
            }
                break;

        }
    }
}
