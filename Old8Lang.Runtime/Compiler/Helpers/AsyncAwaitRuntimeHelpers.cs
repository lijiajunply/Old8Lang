using System.Runtime.CompilerServices;

namespace Old8Lang.Compiler.Helpers;

public static class AsyncAwaitRuntimeHelpers
{
    public static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref AsyncTaskMethodBuilder<object> builder,
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : struct, IAsyncStateMachine
    {
        builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}

