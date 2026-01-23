using Old8Lang.Bytecode.Core;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行并发原语指令
    /// </summary>
    private void ExecuteConcurrencyOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.MutexCreate:
            {
                var mutexId = Concurrency.ResourceManager.CreateMutex();
                _stack.Push(mutexId);
            }
                break;

            case OpCode.MutexLock:
            {
                var mutexId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.LockMutex(mutexId);
            }
                break;

            case OpCode.MutexUnlock:
            {
                var mutexId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.UnlockMutex(mutexId);
            }
                break;

            case OpCode.MutexDispose:
            {
                var mutexId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.DisposeMutex(mutexId);
            }
                break;

            case OpCode.ChannelCreate:
            {
                var channelId = Concurrency.ResourceManager.CreateChannel();
                _stack.Push(channelId);
            }
                break;

            case OpCode.ChannelSend:
            {
                var value = _stack.Pop();
                var channelId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.SendChannel(channelId, value);
            }
                break;

            case OpCode.ChannelReceive:
            {
                var channelId = Convert.ToInt32(_stack.Pop());
                var value = Concurrency.ResourceManager.ReceiveChannel(channelId);
                _stack.Push(value);
            }
                break;

            case OpCode.ChannelClose:
            {
                var channelId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.CloseChannel(channelId);
            }
                break;

            case OpCode.ChannelTrySend:
            {
                // 栈顶: timeoutMs, value, channelId
                var timeoutMs = Convert.ToInt32(_stack.Pop());
                var value = _stack.Pop();
                var channelId = Convert.ToInt32(_stack.Pop());
                var success = Concurrency.ResourceManager.TrySendChannel(channelId, value, timeoutMs);
                _stack.Push(success);
            }
                break;

            case OpCode.ChannelTryReceive:
            {
                // 栈顶: timeoutMs, channelId
                var timeoutMs = Convert.ToInt32(_stack.Pop());
                var channelId = Convert.ToInt32(_stack.Pop());
                var result = Concurrency.ResourceManager.TryReceiveChannel(channelId, timeoutMs);
                _stack.Push(result);
            }
                break;

            case OpCode.SemaphoreCreate:
            {
                // 栈顶: maxCount, initialCount
                var maxCount = Convert.ToInt32(_stack.Pop());
                var initialCount = Convert.ToInt32(_stack.Pop());
                var semaphoreId = Concurrency.ResourceManager.CreateSemaphore(initialCount, maxCount);
                _stack.Push(semaphoreId);
            }
                break;

            case OpCode.SemaphoreAcquire:
            {
                var semaphoreId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.AcquireSemaphore(semaphoreId);
            }
                break;

            case OpCode.SemaphoreRelease:
            {
                var semaphoreId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.ReleaseSemaphore(semaphoreId);
            }
                break;

        }
    }
}
