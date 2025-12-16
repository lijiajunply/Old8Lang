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

                   // 全局变量用于线程间通信
                   result <- ""

                   func threadFunc() {
                       result <- "Thread executed"
                   }

                   // 创建并启动线程
                   thread <- Thread(threadFunc)
                   thread.Join()

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

                   result <- 0

                   func addNumbers(a, b) {
                       result <- a + b
                   }

                   // 创建并启动带参数的线程
                   thread <- Thread(() => addNumbers(10, 20))
                   thread.Join()

                   Assert(result, 30)

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadJoinWithTimeout_Test()
    {
        // 测试带超时的线程等待
        var code = """
                   // 带超时的线程等待测试

                   func longRunningTask() {
                       Thread.Sleep(200)
                   }

                   // 创建并启动线程
                   thread <- Thread(longRunningTask)

                   // 尝试在100ms内等待线程完成，应该返回false
                   completed <- thread.Join(100)
                   Assert(completed, false)

                   // 再次等待，这次应该完成
                   thread.Join()

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadExceptionHandling_Test()
    {
        // 测试线程异常处理
        var code = """
                   // 线程异常处理测试

                   // 使用异常标志来检测线程是否抛出异常
                   exceptionThrown <- false

                   func throwError() {
                       throw "Thread exception"
                   }

                   func catchThreadException() {
                       try {
                           throwError()
                       } catch (e) {
                           exceptionThrown <- true
                       }
                   }

                   // 创建并启动线程
                   thread <- Thread(catchThreadException)
                   thread.Join()

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

                   func sleepTask() {
                       Thread.Sleep(100)
                   }

                   // 创建并启动线程
                   thread <- Thread(sleepTask)

                   // 检查线程状态是否为 Running
                   state <- thread.State
                   Assert(state == "Running" || state == "WaitSleepJoin", "线程状态应为 Running 或 WaitSleepJoin")

                   // 等待线程完成
                   thread.Join()

                   // 检查线程状态是否为 Stopped
                   state <- thread.State
                   Assert(state == "Stopped", "线程状态应为 Stopped")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadMultipleCreation_Test()
    {
        // 测试创建多个线程
        var code = """
                   // 创建多个线程测试

                   count <- 0
                   lockObj <- Object()

                   func incrementCounter() {
                       for i in 0..100 {
                           lock(lockObj) {
                               count += 1
                           }
                       }
                   }

                   // 创建并启动多个线程
                   threads <- []
                   for i in 0..4 {
                       threads.Add(Thread(incrementCounter))
                   }

                   // 等待所有线程完成
                   for thread in threads {
                       thread.Join()
                   }

                   Assert(count, 505)  // 5个线程，每个执行101次循环 (0到100)

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadDelay_Test()
    {
        // 测试线程延迟
        var code = """
                   // 线程延迟测试
                   import Time

                   start <- Time.Now()
                   Thread.Sleep(100)
                   end <- Time.Now()

                   // 检查延迟是否在合理范围内 (90-150ms)
                   duration <- end - start
                   Assert(duration >= 90, "延迟时间太短")
                   Assert(duration <= 150, "延迟时间太长")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadCompletionCheck_Test()
    {
        // 测试线程完成状态检查
        var code = """
                   // 线程完成状态检查测试

                   func longRunningTask() {
                       Thread.Sleep(100)
                   }

                   // 创建并启动线程
                   thread <- Thread(longRunningTask)

                   // 检查线程是否未完成
                   Assert(thread.IsCompleted == false, "线程应未完成")

                   // 等待线程完成
                   thread.Join()

                   // 检查线程是否已完成
                   Assert(thread.IsCompleted == true, "线程应已完成")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadWhenAll_Test()
    {
        // 测试 Thread.WhenAll
        var code = """
                   // Thread.WhenAll 测试

                   // 全局变量用于线程间通信
                   results <- []
                   lockObj <- Object()

                   func task1() {
                       Thread.Sleep(50)
                       lock(lockObj) {
                           results.Add("Task 1")
                       }
                   }

                   func task2() {
                       Thread.Sleep(100)
                       lock(lockObj) {
                           results.Add("Task 2")
                       }
                   }

                   func task3() {
                       Thread.Sleep(150)
                       lock(lockObj) {
                           results.Add("Task 3")
                       }
                   }

                   // 创建线程列表
                   threads <- [
                       Thread(task1),
                       Thread(task2),
                       Thread(task3)
                   ]

                   // 等待所有线程完成
                   Thread.WhenAll(threads)

                   // 验证所有任务都已执行
                   Assert(results.Length, 3)
                   Assert("Task 1" in results, "Task 1 应执行")
                   Assert("Task 2" in results, "Task 2 应执行")
                   Assert("Task 3" in results, "Task 3 应执行")

                   """;
        
        ExecuteOld8Code(code);
    }
    
    [Fact]
    public void ThreadWhenAny_Test()
    {
        // 测试 Thread.WhenAny
        var code = """
                   // Thread.WhenAny 测试

                   // 全局变量用于线程间通信
                   firstResult <- ""
                   lockObj <- Object()

                   func fastTask() {
                       Thread.Sleep(50)
                       lock(lockObj) {
                           firstResult <- "Fast task"
                       }
                   }

                   func slowTask() {
                       Thread.Sleep(200)
                       lock(lockObj) {
                           if firstResult == "" {
                               firstResult <- "Slow task"
                           }
                       }
                   }

                   // 创建线程列表
                   threads <- [
                       Thread(slowTask),
                       Thread(fastTask)
                   ]

                   // 等待任意一个线程完成
                   Thread.WhenAny(threads)

                   // 验证先完成的是 fastTask
                   Assert(firstResult, "Fast task")

                   // 等待所有线程完成以清理资源
                   for thread in threads {
                       thread.Join()
                   }

                   """;
        
        ExecuteOld8Code(code);
    }
}
