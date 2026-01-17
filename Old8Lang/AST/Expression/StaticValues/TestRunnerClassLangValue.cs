using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8LangLib;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// TestRunner 类的全局对象,提供静态方法访问
/// </summary>
public partial class TestRunnerClassLangValue : LangValueType
{
    private static readonly TestRunnerClassLangValue Instance = new();

    /// <summary>
    /// 获取 TestRunner 类的全局单例
    /// </summary>
    public static TestRunnerClassLangValue GetInstance() => Instance;

    public override string TypeToString() => "TestRunnerClass";

    public override string ToDisplayString() => "TestRunner";

    /// <summary>
    /// 生成 IL 代码，返回 TestRunner 类型本身
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 对于 TestRunner 类静态方法，我们不需要加载实例
        // 直接返回 TestRunner 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(TestRunnerClassLangValue));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);
    }

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 处理 TestRunner.Describe(...) 形式的调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            // 返回一个包装函数,用于调用静态方法
            Func<List<LangValueType>, SourcePosition, LangValueType>? method = methodName switch
            {
                "Describe" => Describe,
                "It" => It,
                "Test" => Test,
                "RunAll" => RunAll,
                "RunSuite" => RunSuite,
                "RunTest" => RunTest,
                "Clear" => Clear,
                "GetSuiteCount" => GetSuiteCount,
                "GetTestCount" => GetTestCount,
                "PrintSummary" => PrintSummary,
                "PrintDetailed" => PrintDetailed,
                "RunAllWithReport" => RunAllWithReport,
                "GetStatistics" => GetStatistics,
                _ => null
            };

            if (method is null)
            {
                throw new AttributeError(dotExpression.Position, methodName, "TestRunner");
            }

            // 使用 ExternalManager 或传入的 manager 执行参数
            var currentManager = ExternalManager ?? manager;
            var args = instance.Ids.Select(id => id.Run(currentManager)).ToList();
            return method(args, instance.Position);
        }

        // 处理 TestRunner.Describe 形式的访问（不带调用）
        if (dotExpression is LangId memberId)
        {
            var methodName = memberId.IdName;

            // 返回一个包装函数,用于调用静态方法
            return methodName switch
            {
                "Describe" => new TestRunnerStaticMethodWrapper("Describe", Describe),
                "It" => new TestRunnerStaticMethodWrapper("It", It),
                "Test" => new TestRunnerStaticMethodWrapper("Test", Test),
                "RunAll" => new TestRunnerStaticMethodWrapper("RunAll", RunAll),
                "RunSuite" => new TestRunnerStaticMethodWrapper("RunSuite", RunSuite),
                "RunTest" => new TestRunnerStaticMethodWrapper("RunTest", RunTest),
                "Clear" => new TestRunnerStaticMethodWrapper("Clear", Clear),
                "GetSuiteCount" => new TestRunnerStaticMethodWrapper("GetSuiteCount", GetSuiteCount),
                "GetTestCount" => new TestRunnerStaticMethodWrapper("GetTestCount", GetTestCount),
                "PrintSummary" => new TestRunnerStaticMethodWrapper("PrintSummary", PrintSummary),
                "PrintDetailed" => new TestRunnerStaticMethodWrapper("PrintDetailed", PrintDetailed),
                "RunAllWithReport" => new TestRunnerStaticMethodWrapper("RunAllWithReport", RunAllWithReport),
                "GetStatistics" => new TestRunnerStaticMethodWrapper("GetStatistics", GetStatistics),
                _ => throw new AttributeError(dotExpression.Position, methodName, "TestRunner")
            };
        }

        throw new AttributeError(dotExpression.Position,
            dotExpression.ToString() ?? "unknown", "TestRunner");
    }

    /// <summary>
    /// Describe 静态方法实现
    /// </summary>
    private static LangValueType Describe(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"Describe 期望 2 个参数(suiteName, suiteSetup)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue suiteName)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not FuncLangValue suiteSetup)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "function", args[1].TypeToString());
        }

        try
        {
            TestRunner.Describe(suiteName.Value, () => suiteSetup.Run(new VariateManager(), []));
        }
        catch (Exception ex)
        {
            throw new Exception($"Describe 执行失败: {ex.Message}");
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// It 静态方法实现
    /// </summary>
    private static LangValueType It(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"It 期望 2 个参数(testName, testAction)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue testName)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not FuncLangValue testAction)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "function", args[1].TypeToString());
        }

        try
        {
            TestRunner.It(testName.Value, () => testAction.Run(new VariateManager(), []));
        }
        catch (Exception ex)
        {
            throw new Exception($"It 执行失败: {ex.Message}");
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// Test 静态方法实现（It 的别名）
    /// </summary>
    private static LangValueType Test(List<LangValueType> args, SourcePosition position)
    {
        return It(args, position);
    }

    /// <summary>
    /// RunAll 静态方法实现
    /// </summary>
    private static LangValueType RunAll(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(position,
                $"RunAll 期望 0 个参数，但提供了 {args.Count} 个");
        }

        try
        {
            var results = TestRunner.RunAll();
            var resultList = results.Select(ConvertTestResult).ToList();
            return new ListLangValue(resultList);
        }
        catch (Exception ex)
        {
            throw new Exception($"RunAll 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// RunSuite 静态方法实现
    /// </summary>
    private static LangValueType RunSuite(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"RunSuite 期望 1 个参数(suite)，但提供了 {args.Count} 个");
        }

        // 这里简化处理，假设传入的是一个测试套件名称字符串
        if (args[0] is not StringLangValue)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        // 由于无法直接从字符串获取 TestSuite 对象，这里返回空列表
        return new ListLangValue(new List<LangExpression>());
    }

    /// <summary>
    /// RunTest 静态方法实现
    /// </summary>
    private static LangValueType RunTest(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"RunTest 期望 2 个参数(testName, testAction)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue testName)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not FuncLangValue testAction)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "function", args[1].TypeToString());
        }

        try
        {
            var result = TestRunner.RunTest(testName.Value, () => testAction.Run(new VariateManager(), []));
            return ConvertTestResult(result);
        }
        catch (Exception ex)
        {
            throw new Exception($"RunTest 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear 静态方法实现
    /// </summary>
    private static LangValueType Clear(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(position,
                $"Clear 期望 0 个参数，但提供了 {args.Count} 个");
        }

        TestRunner.Clear();
        return new VoidLangValue();
    }

    /// <summary>
    /// GetSuiteCount 静态方法实现
    /// </summary>
    private static LangValueType GetSuiteCount(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(position,
                $"GetSuiteCount 期望 0 个参数，但提供了 {args.Count} 个");
        }

        return new IntLangValue(TestRunner.GetSuiteCount());
    }

    /// <summary>
    /// GetTestCount 静态方法实现
    /// </summary>
    private static LangValueType GetTestCount(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(position,
                $"GetTestCount 期望 0 个参数，但提供了 {args.Count} 个");
        }

        return new IntLangValue(TestRunner.GetTestCount());
    }

    /// <summary>
    /// PrintSummary 静态方法实现
    /// </summary>
    private static LangValueType PrintSummary(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"PrintSummary 期望 1 个参数(results)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList resultList)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "list", args[0].TypeToString());
        }

        try
        {
            var results = ConvertToTestResults(resultList);
            TestRunner.PrintSummary(results);
        }
        catch (Exception ex)
        {
            throw new Exception($"PrintSummary 执行失败: {ex.Message}");
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// PrintDetailed 静态方法实现
    /// </summary>
    private static LangValueType PrintDetailed(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"PrintDetailed 期望 1 个参数(results)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList resultList)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "list", args[0].TypeToString());
        }

        try
        {
            var results = ConvertToTestResults(resultList);
            TestRunner.PrintDetailed(results);
        }
        catch (Exception ex)
        {
            throw new Exception($"PrintDetailed 执行失败: {ex.Message}");
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// RunAllWithReport 静态方法实现
    /// </summary>
    private static LangValueType RunAllWithReport(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(position,
                $"RunAllWithReport 期望 0 个参数，但提供了 {args.Count} 个");
        }

        try
        {
            TestRunner.RunAllWithReport();
        }
        catch (Exception ex)
        {
            throw new Exception($"RunAllWithReport 执行失败: {ex.Message}");
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// GetStatistics 静态方法实现
    /// </summary>
    private static LangValueType GetStatistics(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"GetStatistics 期望 1 个参数(results)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList resultList)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "list", args[0].TypeToString());
        }

        try
        {
            var results = ConvertToTestResults(resultList);
            var statistics = TestRunner.GetStatistics(results);

            var tupleList = new List<TupleLangValue>();
            foreach (var kvp in statistics)
            {
                var key = new StringLangValue(kvp.Key);
                LangValueType value;
                if (kvp.Value is int intValue)
                    value = new IntLangValue(intValue);
                else if (kvp.Value is long longValue)
                    value = new DoubleLangValue(longValue);
                else
                    value = new StringLangValue(kvp.Value.ToString() ?? "");

                tupleList.Add(new TupleLangValue(key, value));
            }

            return new DictionaryLangValue(tupleList);
        }
        catch (Exception ex)
        {
            throw new Exception($"GetStatistics 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 将 TestResult 转换为 LangValueType
    /// </summary>
    private static LangValueType ConvertTestResult(TestRunner.TestResult result)
    {
        var tupleList = new List<TupleLangValue>
        {
            new(new StringLangValue("testName"), new StringLangValue(result.TestName)),
            new(new StringLangValue("passed"), new BoolLangValue(result.Passed)),
            new(new StringLangValue("elapsedMilliseconds"), new DoubleLangValue(result.ElapsedMilliseconds))
        };

        if (result.ErrorMessage is not null)
        {
            tupleList.Add(new(new StringLangValue("errorMessage"), new StringLangValue(result.ErrorMessage)));
        }

        return new DictionaryLangValue(tupleList);
    }

    /// <summary>
    /// 将 LangValueType 列表转换为 TestResult 列表
    /// </summary>
    private static List<TestRunner.TestResult> ConvertToTestResults(ILangList resultList)
    {
        var results = new List<TestRunner.TestResult>();

        foreach (var item in resultList.GetItems())
        {
            if (item is DictionaryLangValue dict)
            {
                var result = new TestRunner.TestResult();

                foreach (var kvp in dict.Value)
                {
                    if (kvp.Key is StringLangValue keyStr)
                    {
                        switch (keyStr.Value)
                        {
                            case "testName" when kvp.Value is StringLangValue name:
                                result.TestName = name.Value;
                                break;
                            case "passed" when kvp.Value is BoolLangValue passed:
                                result.Passed = passed.Value;
                                break;
                            case "elapsedMilliseconds" when kvp.Value is DoubleLangValue elapsed:
                                result.ElapsedMilliseconds = (long)elapsed.Value;
                                break;
                            case "errorMessage" when kvp.Value is StringLangValue error:
                                result.ErrorMessage = error.Value;
                                break;
                        }
                    }
                }

                results.Add(result);
            }
        }

        return results;
    }
}

/// <summary>
/// TestRunner 静态方法的包装器
/// </summary>
public partial class TestRunnerStaticMethodWrapper(
    string methodName,
    Func<List<LangValueType>, SourcePosition, LangValueType> method)
    : LangValueType
{
    public override string TypeToString() => "TestRunnerStaticMethod";

    public override string ToDisplayString() => $"TestRunner.{methodName}";

    /// <summary>
    /// 执行静态方法
    /// </summary>
    public LangValueType Invoke(List<LangValueType> args, SourcePosition position)
    {
        return method(args, position);
    }

    /// <summary>
    /// 生成 IL 代码，返回对应TestRunner静态方法的委托
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // TestRunner 静态方法在 IL 中通常通过直接调用来实现
        // 这里返回 null 作为占位符
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return typeof(Delegate);
    }
}