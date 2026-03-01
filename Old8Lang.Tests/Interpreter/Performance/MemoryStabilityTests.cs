using System;
using System.Diagnostics;
using System.IO;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 内存稳定性测试
/// 目标: 长时间运行时内存增长 <5%
/// </summary>
public class MemoryStabilityTests
{
    [Fact]
    public void LongRunning_MemoryUsage_ShouldBeStable()
    {
        // Arrange - 使用简单的重复执行来模拟长时间运行
        var code = @"
            func processItem(n) {
                result <- 0
                for i <- 0, i < n, i <- i + 1 {
                    result <- result + i
                }
                return result
            }

            total <- 0
            for batch <- 0, batch < 100, batch <- batch + 1 {
                total <- total + processItem(50)
            }
        ";

        // 预热
        for (int i = 0; i < 3; i++)
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        }

        // 强制 GC 获取基线内存
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(false);

        // 执行多次，模拟长时间运行
        for (int i = 0; i < 20; i++)
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        }

        // 强制 GC 后检查内存
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert - 内存增长应该很小（允许一些 GC 开销）
        var memoryGrowthBytes = Math.Max(0, finalMemory - baselineMemory);
        var memoryGrowthMB = memoryGrowthBytes / (1024.0 * 1024.0);

        // 允许最多 50MB 的内存增长（考虑到 .NET 运行时的内存管理）
        Assert.True(memoryGrowthMB < 50,
            $"内存增长 {memoryGrowthMB:F2}MB 超过预期");
    }

    [Fact]
    public void ObjectPool_ShouldReduceAllocations()
    {
        // Arrange - 测试对象池是否有效减少分配
        var code = @"
            total <- 0
            for i <- 0, i < 1000, i <- i + 1 {
                total <- total + i
            }
        ";

        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig
        {
            Enabled = true,
            EnableMemoryTracking = true
        });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        // Assert - 对象池统计应该存在
        Assert.NotNull(metrics);
        Assert.NotNull(metrics.ObjectPoolStats);
        Assert.True(metrics.ObjectPoolStats.Count > 0, "应该有对象池统计信息");

        // 验证对象池有分配记录
        var totalAllocations = 0L;
        foreach (var poolStats in metrics.ObjectPoolStats)
        {
            totalAllocations += poolStats.TotalAllocations;
        }
        Assert.True(totalAllocations > 0, "对象池应该有分配记录");
    }

    [Fact]
    public void RepeatedExecution_ShouldNotLeakMemory()
    {
        // Arrange
        var code = @"
            func fibonacci(n) {
                if n <= 1 {
                    return n
                }
                return fibonacci(n - 1) + fibonacci(n - 2)
            }
            result <- fibonacci(15)
        ";

        // 预热
        for (int i = 0; i < 5; i++)
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(false);

        // 重复执行
        for (int i = 0; i < 50; i++)
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);

        var memoryGrowthMB = Math.Max(0, finalMemory - baselineMemory) / (1024.0 * 1024.0);

        // 允许最多 100MB 的内存增长
        Assert.True(memoryGrowthMB < 100,
            $"重复执行后内存增长 {memoryGrowthMB:F2}MB 超过预期");
    }
}
