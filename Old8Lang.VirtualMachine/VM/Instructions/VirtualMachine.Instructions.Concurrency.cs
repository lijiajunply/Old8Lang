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
                Stack.PushInt(mutexId);
            }
                break;

            case OpCode.MutexLock:
            {
                var mutexId = Stack.PopInt32();
                Concurrency.ResourceManager.LockMutex(mutexId);
            }
                break;

            case OpCode.MutexUnlock:
            {
                var mutexId = Stack.PopInt32();
                Concurrency.ResourceManager.UnlockMutex(mutexId);
            }
                break;

            case OpCode.MutexDispose:
            {
                var mutexId = Stack.PopInt32();
                Concurrency.ResourceManager.DisposeMutex(mutexId);
            }
                break;

            case OpCode.ChannelCreate:
            {
                var channelId = Concurrency.ResourceManager.CreateChannel();
                Stack.PushInt(channelId);
            }
                break;

            case OpCode.ChannelSend:
            {
                var value = Stack.PopObject()!;
                var channelId = Stack.PopInt32();
                Concurrency.ResourceManager.SendChannel(channelId, value);
            }
                break;

            case OpCode.ChannelReceive:
            {
                var channelId = Stack.PopInt32();
                var value = Concurrency.ResourceManager.ReceiveChannel(channelId);
                Stack.PushObject(value);
            }
                break;

            case OpCode.ChannelClose:
            {
                var channelId = Stack.PopInt32();
                Concurrency.ResourceManager.CloseChannel(channelId);
            }
                break;

            case OpCode.ChannelTrySend:
            {
                // 栈顶: timeoutMs, value, channelId
                var timeoutMs = Stack.PopInt32();
                var value = Stack.PopObject()!;
                var channelId = Stack.PopInt32();
                var success = Concurrency.ResourceManager.TrySendChannel(channelId, value, timeoutMs);
                Stack.PushBool(success);
            }
                break;

            case OpCode.ChannelTryReceive:
            {
                // 栈顶: timeoutMs, channelId
                var timeoutMs = Stack.PopInt32();
                var channelId = Stack.PopInt32();
                var result = Concurrency.ResourceManager.TryReceiveChannel(channelId, timeoutMs);
                Stack.PushObject(result);
            }
                break;

            case OpCode.ChannelIsClosed:
            {
                var channelId = Stack.PopInt32();
                var isClosed = Concurrency.ResourceManager.IsChannelClosed(channelId);
                Stack.PushBool(isClosed);
            }
                break;

            case OpCode.SemaphoreCreate:
            {
                // 栈顶: maxCount, initialCount
                var maxCount = Stack.PopInt32();
                var initialCount = Stack.PopInt32();
                var semaphoreId = Concurrency.ResourceManager.CreateSemaphore(initialCount, maxCount);
                Stack.PushInt(semaphoreId);
            }
                break;

            case OpCode.SemaphoreAcquire:
            {
                var semaphoreId = Stack.PopInt32();
                Concurrency.ResourceManager.AcquireSemaphore(semaphoreId);
            }
                break;

            case OpCode.SemaphoreRelease:
            {
                var semaphoreId = Stack.PopInt32();
                Concurrency.ResourceManager.ReleaseSemaphore(semaphoreId);
            }
                break;
        }
    }
}
