using Old8Lang.Compiler.Helpers;

namespace Old8Lang.Tests.Compiler.Async;

public class IlAsyncGeneratorStreamTests
{
    [Fact]
    public async Task YieldSuspendsUntilNextMoveAndDisposeWaitsForCleanup()
    {
        var step = 0;
        var stream = new IlAsyncGeneratorStream(async emitter =>
        {
            try
            {
                emitter.Yield(1);
                step = 1;
                emitter.Yield(2);
                return null;
            }
            finally
            {
                await Task.Delay(10);
                step = 2;
            }
        });
        var iterator = stream.GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync());
        Assert.Equal(0, step);
        await iterator.DisposeAsync();
        Assert.Equal(2, step);
    }

    [Fact]
    public async Task ValueTypeAsyncEnumerableCanBeAdapted()
    {
        using var iterator = IlAsyncLoopEnumerator.Create(Numbers());
        Assert.True(await iterator.MoveNextTask());
        Assert.Equal(42, iterator.Current);
        Assert.False(await iterator.MoveNextTask());
    }

    private static async IAsyncEnumerable<int> Numbers()
    {
        await Task.Yield();
        yield return 42;
    }
}
