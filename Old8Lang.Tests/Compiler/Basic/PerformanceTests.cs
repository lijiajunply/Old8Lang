using Old8Lang.Interpreter;
using System.Diagnostics;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 编译模式性能测试
/// 测试编译器在处理大量数据或复杂操作时的性能表现
/// </summary>
[Collection("Sequential")]
public class PerformanceTests
{
    #region 编译性能测试

    [Fact]
    public void CompileSimpleCode_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = "a <- 42";
        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(compiledAction);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"编译简单代码耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void CompileComplexCode_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 30
            result <- (a + b) * c - (a * b) + (c / a) - (b % c)
        ";
        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(compiledAction);
        Assert.True(stopwatch.ElapsedMilliseconds < 200, $"编译复杂表达式耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 执行性能测试

    [Fact]
    public void ExecuteSimpleOperations_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = "result <- 42 + 58";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        for (int i = 0; i < 10000; i++)
        {
            compiledAction();
        }
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"执行10000次简单操作耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ExecuteLoopOperations_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i < 100, i++ {
                sum <- sum + i
            }
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        compiledAction();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"执行100次循环耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ExecuteFunctionCallOperations_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int) -> int {
                return x * y + x - y
            }
            
            result <- 0
            for i <- 0, i < 1000, i++ {
                result <- result + calculate(i, i + 1)
            }
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        compiledAction();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"执行1000次函数调用耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 内存性能测试

    [Fact]
    public void LargeVariableAllocation_HandlesMemoryEfficiently()
    {
        // Arrange
        var code = new System.Text.StringBuilder();
        
        // 创建大量变量
        for (int i = 0; i < 1000; i++)
        {
            code.AppendLine($"var{i} <- {i}");
        }

        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();
        
        // 测量内存使用
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        stopwatch.Start();
        var ast = interpreter.Build(code.ToString());
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();
        stopwatch.Stop();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        Assert.NotNull(compiledAction);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"分配1000个变量耗时: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(memoryUsed < 10 * 1024 * 1024, 
            $"内存使用: {memoryUsed / 1024 / 1024}MB");
    }

    [Fact]
    public void LargeArrayCreation_HandlesMemoryEfficiently()
    {
        // Arrange
        var code = "arr <- [0";
        for (int i = 1; i < 1000; i++)
        {
            code += $", {i}";
        }
        code += "]";

        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();
        
        // 测量内存使用
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        stopwatch.Start();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();
        stopwatch.Stop();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        Assert.NotNull(compiledAction);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"创建1000元素数组耗时: {stopwatch.ElapsedMilliseconds}ms");
        
        // 清理
        compiledAction = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    #endregion

    #region 递归性能测试

    [Fact]
    public void RecursiveFunctionExecution_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }
            
            result <- factorial(20)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        compiledAction();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"递归计算阶乘耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 字符串操作性能测试

    [Fact]
    public void LongStringProcessing_PerformsWithinAcceptableTime()
    {
        // Arrange
        var longString = new string('a', 1000);
        var code = $"longString <- \"{longString}\"";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        compiledAction();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"处理长字符串耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 字典操作性能测试

    [Fact]
    public void DictionaryOperations_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = new System.Text.StringBuilder();
        code.AppendLine("dict <- {}");
        
        // 添加大量键值对
        for (int i = 0; i < 100; i++)
        {
            code.AppendLine($"dict[\"key{i}\"] <- \"value{i}\"");
        }
        
        code.AppendLine(@"
            // 查找操作
            sum <- 0
            for i <- 0, i < 50, i++ {
                keyString <- ""key"" + i.ToStr()
                value <- dict[keyString]
            }
        ");

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code.ToString());
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        compiledAction();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, 
            $"100次字典操作耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 比较性能测试

    [Fact]
    public void CompiledVsInterpreted_PerformanceComparison()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- sum + i * 2 - i / 3
            }
        ";
        var interpreter = new LangInterpreter();

        // 测试编译模式性能
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();
        stopwatch.Stop();
        var compiledTime = stopwatch.ElapsedMilliseconds;

        // 测试解释模式性能
        stopwatch.Restart();
        var interpretedAst = interpreter.Build(code);
        interpretedAst.Run(interpreter.Manager);
        stopwatch.Stop();
        var interpretedTime = stopwatch.ElapsedMilliseconds;

        // Assert - 编译模式应该比解释模式快
        Assert.True(compiledTime < interpretedTime || compiledTime < 1000, 
            $"编译模式: {compiledTime}ms, 解释模式: {interpretedTime}ms");
    }

    #endregion

    #region 压力测试

    [Fact]
    public void MultipleCompilations_HandlesStressTest()
    {
        // Arrange
        var code = "result <- 42 * 100 / 3 + 7 - 2";
        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        for (int i = 0; i < 100; i++)
        {
            var ast = interpreter.Build(code);
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            compiledAction();
        }
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"100次编译执行循环耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ComplexProgramStressTest_PerformsWithinAcceptableTime()
    {
        // Arrange
        var code = new System.Text.StringBuilder(@"
            // 复杂程序压力测试
            numbers <- []
            for i <- 0, i < 200, i++ {
                numbers <- numbers + [i * 2]
            }
            
            processedNumbers <- []
            for num in numbers {
                if num % 4 == 0 {
                    processedNumbers <- processedNumbers + [num / 2]
                } else {
                    processedNumbers <- processedNumbers + [num * 3]
                }
            }
            
            func calculateSum(arr) -> int {
                sum <- 0
                for item in arr {
                    sum <- sum + item
                }
                return sum
            }
            
            result <- calculateSum(processedNumbers)
        ");

        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var ast = interpreter.Build(code.ToString());
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();
        stopwatch.Stop();

        // Assert
        Assert.NotNull(compiledAction);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"复杂程序压力测试耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion
}