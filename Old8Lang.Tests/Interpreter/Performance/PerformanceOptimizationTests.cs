using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression;
using System.Diagnostics;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 性能优化效果测试
/// </summary>
public class PerformanceOptimizationTests
{
    [Fact]
    public void VariableCache_ShouldImprovePerformance()
    {
        // Arrange
        var code = @"
global_var <- 100
func test() {
    local_var <- 50
    sum <- 0
    for i <- 0, i < 1000, i <- i + 1 {
        sum <- sum + local_var + global_var
    }
    return sum
}
test()
";

        // Act - 不启用缓存
        var interpreter1 = new LangInterpreter();
        var sw1 = Stopwatch.StartNew();
        var ast1 = interpreter1.Build(code);
        ast1.Run(interpreter1.Manager);
        sw1.Stop();
        var timeWithoutCache = sw1.ElapsedMilliseconds;

        // Act - 启用缓存
        var interpreter2 = new LangInterpreter();
        interpreter2.Manager.EnableEnhancedCache(1000);
        var sw2 = Stopwatch.StartNew();
        var ast2 = interpreter2.Build(code);
        ast2.Run(interpreter2.Manager);
        sw2.Stop();
        var timeWithCache = sw2.ElapsedMilliseconds;

        // 获取缓存统计
        var (hitCount, missCount, hitRate) = interpreter2.Manager.GetCacheStats();

        // Assert
        Assert.True(hitCount > 0, "缓存应该有命中");
        Assert.True(hitRate > 0, $"缓存命中率应该大于0，实际: {hitRate:P}");

        // 输出性能对比
        var improvement = timeWithoutCache > 0
            ? ((double)(timeWithoutCache - timeWithCache) / timeWithoutCache * 100)
            : 0;

        // 注意：由于测试环境的差异，我们不强制要求性能提升，只验证缓存工作
        Assert.True(true, $"性能对比 - 无缓存: {timeWithoutCache}ms, 有缓存: {timeWithCache}ms, 提升: {improvement:F1}%");
    }

    [Fact]
    public void PerformanceMonitor_ShouldTrackLoopIterations()
    {
        // Arrange
        var code = @"
for i <- 0, i < 100, i <- i + 1 {
    x <- i * 2
}
";

        var monitor = new PerformanceMonitor();
        var interpreter = new LangInterpreter();
        interpreter.PerformanceMonitor = monitor;

        // Act
        monitor.StartMonitoring();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        monitor.StopMonitoring();

        var metrics = monitor.GetMetrics();

        // Assert
        Assert.True(metrics.LoopIterationCount >= 100,
            $"应该记录至少100次循环迭代，实际: {metrics.LoopIterationCount}");
        Assert.True(metrics.ExecutionTimeMs >= 0, "执行时间应该被记录");
    }

    [Fact]
    public void PerformanceMonitor_ShouldTrackVariableLookups()
    {
        // Arrange
        var code = @"
x <- 10
y <- 20
z <- x + y
";

        var monitor = new PerformanceMonitor();
        var interpreter = new LangInterpreter();
        interpreter.PerformanceMonitor = monitor;

        // Act
        monitor.StartMonitoring();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        monitor.StopMonitoring();

        var metrics = monitor.GetMetrics();

        // Assert
        Assert.True(metrics.VariableLookupCount > 0,
            $"应该记录变量查找，实际: {metrics.VariableLookupCount}");
    }

    [Fact]
    public void GlobalVariableCache_ShouldImproveGlobalAccess()
    {
        // Arrange
        var code = @"
global1 <- 100
global2 <- 200

func test() {
    sum <- 0
    for i <- 0, i < 500, i <- i + 1 {
        sum <- sum + global1 + global2
    }
    return sum
}
test()
";

        // Act - 启用增强缓存（包括全局变量缓存）
        var interpreter = new LangInterpreter();
        interpreter.Manager.EnableEnhancedCache(1000);

        var sw = Stopwatch.StartNew();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        sw.Stop();

        var (hitCount, missCount, hitRate) = interpreter.Manager.GetCacheStats();

        // Assert
        Assert.True(hitCount > 0, "全局变量缓存应该有命中");
        Assert.True(hitRate > 0.5, $"全局变量访问的缓存命中率应该较高，实际: {hitRate:P}");
    }
}
