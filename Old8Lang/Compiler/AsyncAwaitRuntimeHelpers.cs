using System.Runtime.CompilerServices;

namespace Old8Lang.Compiler;

public static class AsyncAwaitRuntimeHelpers
{
    public static void AwaitUnsafeOnCompleted<TAwaiter>(
        ref AsyncTaskMethodBuilder<object> builder,
        ref TAwaiter awaiter,
        ref IAsyncStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
    {
        builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}

