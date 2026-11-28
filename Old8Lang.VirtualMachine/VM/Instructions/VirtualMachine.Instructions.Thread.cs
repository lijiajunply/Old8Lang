using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
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
                var funcObj = Stack.PopObject();
                var argCount = Stack.PopInt32();

                // 弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = Stack.PopObject();
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
                        $"Invalid function for ThreadCreate: {DescribeValueType(funcObj)}");
                }

                // 创建线程前开启全局槽位并发模式，保证主线程与子线程之间无撕裂
                _globalsState.IsConcurrent = true;
                var threadIdHolder = new int[1];
                var threadId = Concurrency.ResourceManager.CreateThread(() =>
                {
                    var threadVm = CreateWorkerVirtualMachine();
                    var result = threadVm.ExecuteFunctionAndGetResult(function, args);
                    Concurrency.ResourceManager.SetThreadResult(threadIdHolder[0], result);
                });
                threadIdHolder[0] = threadId;

                Stack.PushInt(threadId);
            }
                break;

            case OpCode.ThreadStart:
            {
                var threadId = Stack.PopInt32();
                Concurrency.ResourceManager.StartThread(threadId);
            }
                break;

            case OpCode.ThreadJoin:
            {
                var threadId = Stack.PopInt32();
                var result = Concurrency.ResourceManager.JoinThread(threadId);
                Stack.PushObject(result);
            }
                break;

            case OpCode.ThreadIsAlive:
            {
                var threadId = Stack.PopInt32();
                var isAlive = Concurrency.ResourceManager.IsThreadAlive(threadId);
                Stack.PushBool(isAlive);
            }
                break;

            case OpCode.ThreadDispose:
            {
                var threadId = Stack.PopInt32();
                Concurrency.ResourceManager.DisposeThread(threadId);
            }
                break;
        }
    }
}
