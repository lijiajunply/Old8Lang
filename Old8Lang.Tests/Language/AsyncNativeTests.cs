using Old8Lang.LangParser;

namespace Old8Lang.Tests.Language;

/// <summary>
/// 测试 Old8Lang 原生异步功能
/// </summary>
public class AsyncNativeTests
{
    /// <summary>
    /// 执行 Old8Lang 代码
    /// </summary>
    private void ExecuteOld8Code(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
    }
    
    [Fact]
    public void AsyncBasicExecution_Test()
    {
        // 测试基本异步函数执行
        var code = """
                   // 异步函数基本执行测试
                   async func hello() {
                       await Task.Delay(100)
                       return "Hello"
                   }

                   task <- hello()
                   result <- await task
                   Assert(result, "Hello")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void AsyncFunctionWithParameters_Test()
    {
        // 测试带参数的异步函数
        var code = """
                   // 带参数的异步函数测试
                   async func add(a:int, b:int) {
                       await Task.Delay(50)
                       return a + b
                   }

                   task <- add(10, 20)
                   sum <- await task
                   Assert(sum, 30)

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void AsyncNestedCalls_Test()
    {
        // 测试嵌套异步调用
        var code = """
                   // 嵌套异步调用测试
                   async func add(a:int, b:int) {
                       await Task.Delay(50)
                       return a + b
                   }

                   async func fetchAndProcess(value:int) {
                       task <- add(value, 5)
                       result <- await task
                       return result * 2
                   }

                   task <- fetchAndProcess(10)
                   finalResult <- await task
                   Assert(finalResult, 30)  // (10 + 5) * 2 = 30

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void AsyncExceptionHandling_Test()
    {
        // 测试异步异常处理
        var code = """
                   // 异步异常处理测试
                   async func throwError() {
                       await Task.Delay(50)
                       throw "异步异常"
                   }

                   try {
                       task <- throwError()
                       result <- await task
                       Assert(false, "应该抛出异常")
                   } catch (e) {
                       Assert(true, "捕获到异常")
                   }

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void AsyncTaskCombination_Test()
    {
        // 测试异步任务组合
        var code = """
                   // 异步任务组合测试
                   async func delayReturn(value:string, delay:int) {
                       await Task.Delay(delay)
                       return value
                   }

                   async func testWhenAll() {
                       tasks <- [delayReturn("A", 100), delayReturn("B", 50), delayReturn("C", 150)]
                       results <- await Task.WhenAll(tasks)
                       Assert(results[0], "A")
                       Assert(results[1], "B")
                       Assert(results[2], "C")
                       Assert(Len(results), 3)
                   }

                   async func testWhenAny() {
                       tasks <- [delayReturn("A", 100), delayReturn("B", 50), delayReturn("C", 150)]
                       result <- await Task.WhenAny(tasks)
                       Assert(result, "B")  // B 延迟最短，应该先完成
                   }

                   task1 <- testWhenAll()
                   await task1

                   task2 <- testWhenAny()
                   await task2

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void AsyncDelay_Test()
    {
        // 测试异步延迟
        var code = """
                   // 异步延迟测试
                   import Time

                   async func testDelay() {
                       start <- Time.Now()
                       await Task.Delay(100)
                       end <- Time.Now()
                       // 检查延迟是否在合理范围内 (90-150ms)
                       duration <- end - start
                       Assert(duration >= 90, "延迟时间太短")
                       Assert(duration <= 150, "延迟时间太长")
                   }

                   task <- testDelay()
                   await task

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void AsyncLambda_Test()
    {
        // 测试异步 Lambda 表达式
        var code = """
                   // 异步 Lambda 测试
                   async func testAsyncLambda() {
                       asyncLambda <- async () -> {
                           await Task.Delay(50)
                           return "Async Lambda"
                       }
                       
                       result <- await asyncLambda()
                       Assert(result, "Async Lambda")
                   }

                   task <- testAsyncLambda()
                   await task

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void AsyncCancellation_Test()
    {
        // 测试异步取消机制
        var code = """
                   // 异步取消机制测试
                   import Async

                   // 全局变量用于标记取消状态
                   canceled <- false

                   async func longRunningTask() {
                       try {
                           // 创建可取消的任务
                           cts <- Async.CreateCancellationTokenSource()
                           
                           // 在另一个任务中取消当前任务
                           cancelTask <- async () -> {
                               await Task.Delay(50)
                               Async.Cancel(cts)
                           }
                           
                           // 启动取消任务
                           cancelTask()
                           
                           // 尝试执行长时间任务，应该被取消
                           await Task.Delay(200, cts)
                       } catch (e) {
                           canceled <- true
                       }
                   }

                   task <- longRunningTask()
                   await task

                   Assert(canceled, true)

                   """;
        
        ExecuteOld8Code(code);
    }
}
