using Old8Lang.Compiler.Helpers;
using Old8Lang.Interpreter;

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
                await emitter.YieldAsync(1);
                step = 1;
                await emitter.YieldAsync(2);
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
        Assert.Equal(1, iterator.Current);
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

    [Fact]
    public async Task ManySuspendedGenerators_DoNotStarveThreadPool()
    {
        const int generatorCount = 128;
        var enumerators = new List<IAsyncEnumerator<object?>>(generatorCount);

        try
        {
            for (var i = 0; i < generatorCount; i++)
            {
                var id = i;
                var stream = new IlAsyncGeneratorStream(async emitter =>
                {
                    await emitter.YieldAsync(id);
                    await emitter.YieldAsync(id + 1000);
                    return null;
                });
                var enumerator = stream.GetAsyncEnumerator();
                enumerators.Add(enumerator);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal(id, enumerator.Current);
            }

            // 所有生成器处于 yield 挂起态时，线程池应保持畅通并立刻响应新工作项。
            var poolResponsive = await Task.Run(() => 42).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(42, poolResponsive);

            for (var i = 0; i < generatorCount; i++)
            {
                Assert.True(await enumerators[i].MoveNextAsync());
                Assert.Equal(i + 1000, enumerators[i].Current);
                Assert.False(await enumerators[i].MoveNextAsync());
            }
        }
        finally
        {
            foreach (var enumerator in enumerators)
            {
                await enumerator.DisposeAsync();
            }
        }
    }

    [Fact]
    public void CompiledAsyncGenerator_YieldInTryCatchFinallyAndEarlyBreak_WorksWithoutBlocking()
    {
        const string code = @"
            cleanupCount <- 0

            async func genWithCleanup(n:int) {
                try {
                    i <- 0
                    while i < n {
                        await Task.Delay(1)
                        yield i * 10
                        i <- i + 1
                    }
                } finally {
                    await Task.Delay(1)
                    cleanupCount <- cleanupCount + 1
                }
            }

            async func genYieldInCatch() {
                try {
                    yield 1
                    throw ""boom""
                } catch (e) {
                    yield 99
                }
                yield 2
            }

            async func main() -> int {
                sum <- 0
                async for x in genWithCleanup(10) {
                    sum <- sum + x
                    if x == 20 {
                        break
                    }
                }

                catchSum <- 0
                async for y in genYieldInCatch() {
                    catchSum <- catchSum + y
                }

                return sum + catchSum + cleanupCount * 1000
            }

            result <- await main()
            Assert.Equal(1132, result)
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "async_gen_nonblocking", interpreter)();
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task CompiledAsyncGenerator_HighConcurrencySuspendedStress_DoesNotBlockThreadPool()
    {
        const string code = @"
            async func makePair(seed:int) {
                yield seed
                await Task.Delay(1)
                yield seed * 2
            }

            async func consume(stream:object) -> int {
                acc <- 0
                async for item in stream {
                    acc <- acc + item
                }
                return acc
            }

            async func fanOut(count:int) -> int {
                streams <- {}
                i <- 0
                while i < count {
                    streams.Add(makePair(i + 1))
                    i <- i + 1
                }

                tasks <- {}
                for s in streams {
                    tasks.Add(consume(s))
                }

                results <- await Task.WhenAll(tasks)
                total <- 0
                for v in results {
                    total <- total + v
                }
                return total
            }

            total <- await fanOut(300)
            // sum_{k=1..300} (k + 2k) = 3 * 300 * 301 / 2 = 135450
            Assert.Equal(135450, total)
        ";

        // 同时保持 1000 个纯流式枚举器挂起在 yield 位置，验证 0 线程占用。
        const int pausedCount = 1000;
        var paused = new List<IAsyncEnumerator<object?>>(pausedCount);
        try
        {
            for (var i = 0; i < pausedCount; i++)
            {
                var v = i;
                var s = new IlAsyncGeneratorStream(async emitter =>
                {
                    await emitter.YieldAsync(v);
                    await emitter.YieldAsync(v + 1);
                    return null;
                });
                var e = s.GetAsyncEnumerator();
                paused.Add(e);
                Assert.True(await e.MoveNextAsync());
                Assert.Equal(v, e.Current);
            }

            // 在 1000 个生成器全部挂起的同时，在线程池运行 300 并发编译期异步生成器。
            await Task.Run(() =>
            {
                var interpreter = new LangInterpreter();
                var ast = interpreter.Build(code);
                Old8Lang.Compiler.Compiler.Compile(ast, "async_gen_stress", interpreter)();
            }).WaitAsync(TimeSpan.FromSeconds(15));

            for (var i = 0; i < pausedCount; i++)
            {
                Assert.True(await paused[i].MoveNextAsync());
                Assert.Equal(i + 1, paused[i].Current);
                Assert.False(await paused[i].MoveNextAsync());
            }
        }
        finally
        {
            foreach (var e in paused)
            {
                await e.DisposeAsync();
            }
        }
    }

    private static async IAsyncEnumerable<int> Numbers()
    {
        await Task.Yield();
        yield return 42;
    }
}
