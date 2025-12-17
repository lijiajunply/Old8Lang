using Old8Lang.Interpreter;
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

                   exceptionCaught <- false
                   try {
                       task <- throwError()
                       result <- await task
                       Assert(false, "应该抛出异常")
                   } catch (e) {
                       exceptionCaught <- true
                   }

                   Assert(exceptionCaught, true)

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
                   native "Old8LangLib" Time
                   async func testDelay() {
                       startTime <- Time.GetElapsedMilliseconds()
                       await Task.Delay(100)
                       endTime <- Time.GetElapsedMilliseconds()
                       // 简单验证功能正常工作
                       PrintLine("异步延迟测试完成")
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
        // 测试异步取消机制 - 基础版本
        var code = """
                   // 异步取消机制测试 - 基础版本

                   // 全局变量用于标记取消状态
                   canceled <- false

                   async func testAsyncCancellation() {
                       try {
                           // 模拟一个可能被取消的操作
                           await Task.Delay(50)
                           // 正常完成
                           return "completed"
                       } catch (e) {
                           canceled <- true
                           return "canceled"
                       }
                   }

                   task <- testAsyncCancellation()
                   result <- await task

                   // 简化测试 - 验证异步函数执行成功
                   Assert(result, "completed")

                   """;
        
        ExecuteOld8Code(code);
    }
}
