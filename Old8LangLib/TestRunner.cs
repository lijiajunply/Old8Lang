using System.Diagnostics;

namespace Old8LangLib;

/// <summary>
/// 测试运行器 - 提供测试管理和执行功能
/// </summary>
public static class TestRunner
{
    /// <summary>
    /// 测试结果类
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public bool Passed { get; set; }
        public string? ErrorMessage { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// 测试套件类
    /// </summary>
    public class TestSuite
    {
        public string Name { get; set; } = "";
        public List<TestCase> Tests { get; set; } = [];
    }

    /// <summary>
    /// 测试用例类
    /// </summary>
    public class TestCase
    {
        public string Name { get; set; } = "";
        public Action TestAction { get; set; } = () => { };
    }

    private static readonly List<TestSuite> _testSuites = [];
    private static TestSuite? _currentSuite;

    // ===== 测试套件管理 =====

    /// <summary>
    /// 描述一个测试套件
    /// </summary>
    /// <param name="suiteName">套件名称</param>
    /// <param name="suiteSetup">套件设置函数</param>
    public static void Describe(string suiteName, Action suiteSetup)
    {
        var suite = new TestSuite { Name = suiteName };
        _testSuites.Add(suite);

        var previousSuite = _currentSuite;
        _currentSuite = suite;

        try
        {
            suiteSetup();
        }
        finally
        {
            _currentSuite = previousSuite;
        }
    }

    /// <summary>
    /// 定义一个测试用例
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="testAction">测试函数</param>
    public static void It(string testName, Action testAction)
    {
        if (_currentSuite == null)
        {
            throw new InvalidOperationException("必须在 Describe 块内部调用 It");
        }

        _currentSuite.Tests.Add(new TestCase
        {
            Name = testName,
            TestAction = testAction
        });
    }

    /// <summary>
    /// 定义一个测试用例 (Test 别名)
    /// </summary>
    public static void Test(string testName, Action testAction)
    {
        It(testName, testAction);
    }

    // ===== 测试执行 =====

    /// <summary>
    /// 运行所有测试
    /// </summary>
    /// <returns>所有测试的结果列表</returns>
    public static List<TestResult> RunAll()
    {
        var allResults = new List<TestResult>();

        foreach (var suite in _testSuites)
        {
            var results = RunSuite(suite);
            allResults.AddRange(results);
        }

        return allResults;
    }

    /// <summary>
    /// 运行指定的测试套件
    /// </summary>
    /// <param name="suite">测试套件</param>
    /// <returns>测试结果列表</returns>
    public static List<TestResult> RunSuite(TestSuite suite)
    {
        var results = new List<TestResult>();

        foreach (var test in suite.Tests)
        {
            var result = RunTest($"{suite.Name} > {test.Name}", test.TestAction);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 运行单个测试
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="testAction">测试函数</param>
    /// <returns>测试结果</returns>
    public static TestResult RunTest(string testName, Action testAction)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TestResult { TestName = testName };

        try
        {
            testAction();
            result.Passed = true;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 清除所有已注册的测试
    /// </summary>
    public static void Clear()
    {
        _testSuites.Clear();
        _currentSuite = null;
    }

    /// <summary>
    /// 获取已注册的测试套件数量
    /// </summary>
    public static int GetSuiteCount()
    {
        return _testSuites.Count;
    }

    /// <summary>
    /// 获取所有测试的总数量
    /// </summary>
    public static int GetTestCount()
    {
        var count = 0;
        foreach (var suite in _testSuites)
        {
            count += suite.Tests.Count;
        }
        return count;
    }

    // ===== 测试报告 =====

    /// <summary>
    /// 打印测试结果摘要
    /// </summary>
    /// <param name="results">测试结果列表</param>
    public static void PrintSummary(List<TestResult> results)
    {
        var passed = 0;
        var failed = 0;
        var totalTime = 0L;

        foreach (var result in results)
        {
            if (result.Passed)
                passed++;
            else
                failed++;
            totalTime += result.ElapsedMilliseconds;
        }

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("测试结果摘要");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"总测试数: {results.Count}");
        Console.WriteLine($"通过: {passed}");
        Console.WriteLine($"失败: {failed}");
        Console.WriteLine($"总耗时: {totalTime}ms");
        Console.WriteLine(new string('=', 60));
    }

    /// <summary>
    /// 打印详细的测试结果
    /// </summary>
    /// <param name="results">测试结果列表</param>
    public static void PrintDetailed(List<TestResult> results)
    {
        Console.WriteLine("\n详细测试结果:");
        Console.WriteLine(new string('-', 60));

        foreach (var result in results)
        {
            var status = result.Passed ? "✓ 通过" : "✗ 失败";
            Console.WriteLine($"{status} | {result.TestName} ({result.ElapsedMilliseconds}ms)");

            if (!result.Passed && result.ErrorMessage != null)
            {
                Console.WriteLine($"  错误: {result.ErrorMessage}");
            }
        }

        Console.WriteLine(new string('-', 60));
    }

    /// <summary>
    /// 运行所有测试并打印报告
    /// </summary>
    public static void RunAllWithReport()
    {
        var results = RunAll();
        PrintDetailed(results);
        PrintSummary(results);
    }

    /// <summary>
    /// 获取测试统计信息
    /// </summary>
    /// <param name="results">测试结果列表</param>
    /// <returns>包含统计信息的字典</returns>
    public static Dictionary<string, object> GetStatistics(List<TestResult> results)
    {
        var passed = 0;
        var failed = 0;
        var totalTime = 0L;
        var minTime = long.MaxValue;
        var maxTime = 0L;

        foreach (var result in results)
        {
            if (result.Passed)
                passed++;
            else
                failed++;

            totalTime += result.ElapsedMilliseconds;
            if (result.ElapsedMilliseconds < minTime)
                minTime = result.ElapsedMilliseconds;
            if (result.ElapsedMilliseconds > maxTime)
                maxTime = result.ElapsedMilliseconds;
        }

        var avgTime = results.Count > 0 ? totalTime / results.Count : 0;

        return new Dictionary<string, object>
        {
            { "total", results.Count },
            { "passed", passed },
            { "failed", failed },
            { "totalTime", totalTime },
            { "avgTime", avgTime },
            { "minTime", minTime == long.MaxValue ? 0 : minTime },
            { "maxTime", maxTime }
        };
    }
}
