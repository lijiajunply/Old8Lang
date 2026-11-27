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
                _stack.PushInt(mutexId);
            }
                break;

            case OpCode.MutexLock:
            {
                var mutexId = _stack.PopInt32();
                Concurrency.ResourceManager.LockMutex(mutexId);
            }
                break;

            case OpCode.MutexUnlock:
            {
                var mutexId = _stack.PopInt32();
                Concurrency.ResourceManager.UnlockMutex(mutexId);
            }
                break;

            case OpCode.MutexDispose:
            {
                var mutexId = _stack.PopInt32();
                Concurrency.ResourceManager.DisposeMutex(mutexId);
            }
                break;

            case OpCode.ChannelCreate:
            {
                var channelId = Concurrency.ResourceManager.CreateChannel();
                _stack.PushInt(channelId);
            }
                break;

            case OpCode.ChannelSend:
            {
                var value = _stack.PopObject()!;
                var channelId = _stack.PopInt32();
                Concurrency.ResourceManager.SendChannel(channelId, value);
            }
                break;

            case OpCode.ChannelReceive:
            {
                var channelId = _stack.PopInt32();
                var value = Concurrency.ResourceManager.ReceiveChannel(channelId);
                _stack.PushObject(value);
            }
                break;

            case OpCode.ChannelClose:
            {
                var channelId = _stack.PopInt32();
                Concurrency.ResourceManager.CloseChannel(channelId);
            }
                break;

            case OpCode.ChannelTrySend:
            {
                // 栈顶: timeoutMs, value, channelId
                var timeoutMs = _stack.PopInt32();
                var value = _stack.PopObject()!;
                var channelId = _stack.PopInt32();
                var success = Concurrency.ResourceManager.TrySendChannel(channelId, value, timeoutMs);
                _stack.PushBool(success);
            }
                break;

            case OpCode.ChannelTryReceive:
            {
                // 栈顶: timeoutMs, channelId
                var timeoutMs = _stack.PopInt32();
                var channelId = _stack.PopInt32();
                var result = Concurrency.ResourceManager.TryReceiveChannel(channelId, timeoutMs);
                _stack.PushObject(result);
            }
                break;

            case OpCode.ChannelIsClosed:
            {
                var channelId = _stack.PopInt32();
                var isClosed = Concurrency.ResourceManager.IsChannelClosed(channelId);
                _stack.PushBool(isClosed);
            }
                break;

            case OpCode.SemaphoreCreate:
            {
                // 栈顶: maxCount, initialCount
                var maxCount = _stack.PopInt32();
                var initialCount = _stack.PopInt32();
                var semaphoreId = Concurrency.ResourceManager.CreateSemaphore(initialCount, maxCount);
                _stack.PushInt(semaphoreId);
            }
                break;

            case OpCode.SemaphoreAcquire:
            {
                var semaphoreId = _stack.PopInt32();
                Concurrency.ResourceManager.AcquireSemaphore(semaphoreId);
            }
                break;

            case OpCode.SemaphoreRelease:
            {
                var semaphoreId = _stack.PopInt32();
                Concurrency.ResourceManager.ReleaseSemaphore(semaphoreId);
            }
                break;
        }
    }
}
