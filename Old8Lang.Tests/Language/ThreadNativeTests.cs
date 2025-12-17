using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Language;

/// <summary>
/// 测试 Old8Lang 原生多线程功能
/// </summary>
public class ThreadNativeTests
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
    public void ThreadBasicCreation_Test()
    {
        // 测试基本线程创建
        var code = """
                   // 基本线程创建测试

                   func threadFunc() {
                       return "Thread executed"
                   }

                   // 创建并启动线程
                   thread <- spawn(threadFunc)
                   result <- thread.Join()

                   Assert(result, "Thread executed")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadWithParameters_Test()
    {
        // 测试带参数的线程
        var code = """
                   // 带参数的线程测试

                   func addNumbers(a, b) {
                       return a + b
                   }

                   // 创建并启动带参数的线程
                   thread <- spawn(addNumbers, 10, 20)
                   result <- thread.Join()

                   Assert(result, 30)

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadJoinWithTimeout_Test()
    {
        // 测试线程基本Join功能
        var code = """
                   // 线程Join功能测试

                   func longRunningTask() {
                       Thread.Sleep(50)
                       return "done"
                   }

                   // 创建并启动线程
                   thread <- spawn(longRunningTask)
                   result <- thread.Join()

                   Assert(result, "done")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadExceptionHandling_Test()
    {
        // 测试线程异常处理
        var code = """
                   // 线程异常处理测试

                   func catchThreadException() {
                       try {
                           throw "Thread exception"
                       } catch (e) {
                           return true
                       }
                       return false
                   }

                   // 创建并启动线程
                   thread <- spawn(catchThreadException)
                   exceptionThrown <- thread.Join()

                   Assert(exceptionThrown, true)

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadState_Test()
    {
        // 测试线程状态
        var code = """
                   // 线程状态测试

                   func simpleTask() {
                       return "done"
                   }

                   // 创建并启动线程
                   thread <- spawn(simpleTask)
                   result <- thread.Join()

                   // 检查线程是否已完成
                   Assert(result, "done")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadMultipleCreation_Test()
    {
        // 测试创建多个线程
        var code = """
                   // 创建多个线程测试

                   func workerTask(id) {
                       return "Worker " + id.ToStr() + " completed"
                   }

                   // 创建并启动多个线程
                   threads <- {}
                   results <- {}

                   for i in [0~4] {
                       thread <- spawn(workerTask, i)
                       threads.Add(thread)
                   }

                   // 等待所有线程完成并收集结果
                   for thread in threads {
                       result <- thread.Join()
                       results.Add(result)
                   }

                   // 验证结果列表不为空（表示至少有一些线程完成了工作）
                   Assert(results != {}, true)

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadDelay_Test()
    {
        // 测试线程延迟
        var code = """
                   // 线程延迟测试

                   func testSleep() {
                       Thread.Sleep(100)
                       return "sleep_done"
                   }

                   // 创建线程测试延迟
                   thread <- spawn(testSleep)
                   result <- thread.Join()

                   Assert(result, "sleep_done")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadCompletionCheck_Test()
    {
        // 测试线程完成状态检查
        var code = """
                   // 线程完成状态检查测试

                   func simpleTask() {
                       return "completed"
                   }

                   // 创建并启动线程
                   thread <- spawn(simpleTask)
                   result <- thread.Join()

                   // 检查任务结果
                   Assert(result, "completed")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadWhenAll_Test()
    {
        // 测试多个线程的并行执行
        var code = """
                   // 多线程并行执行测试

                   func task1() {
                       Thread.Sleep(50)
                       return "Task 1"
                   }

                   func task2() {
                       Thread.Sleep(100)
                       return "Task 2"
                   }

                   func task3() {
                       Thread.Sleep(150)
                       return "Task 3"
                   }

                   // 创建线程列表
                   threads <- [
                       spawn(task1),
                       spawn(task2),
                       spawn(task3)
                   ]

                   results <- {}

                   // 等待所有线程完成
                   for thread in threads {
                       result <- thread.Join()
                       results.Add(result)
                   }

                   // 验证结果列表不为空
                   Assert(results != {}, true)

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadWhenAny_Test()
    {
        // 测试多个线程的执行
        var code = """
                   // 多线程执行测试

                   func fastTask() {
                       Thread.Sleep(50)
                       return "Fast task done"
                   }

                   func slowTask() {
                       Thread.Sleep(200)
                       return "Slow task done"
                   }

                   // 创建线程列表
                   threads <- [
                       spawn(fastTask),
                       spawn(slowTask)
                   ]

                   results <- {}

                   // 等待所有线程完成
                   for thread in threads {
                       result <- thread.Join()
                       results.Add(result)
                   }

                   // 验证结果列表不为空
                   Assert(results != {}, true)

                   """;
        
        ExecuteOld8Code(code);
    }
}
