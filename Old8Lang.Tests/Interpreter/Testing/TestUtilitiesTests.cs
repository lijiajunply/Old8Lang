using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Testing;

/// <summary>
/// 内置测试工具类测试 - 覆盖 Assert、TestRunner、Mock (MockLibClassLangValue) 的完整解释模式行为
/// </summary>
[Collection("Sequential")]
public class TestUtilitiesTests
{
    #region Assert 测试

    [Fact]
    public void Assert_EqualityTruthinessNullAndComparisonAssertions_PassWhenValid()
    {
        var code = @"
Assert.AssertEqual(42, 40 + 2)
Assert.Equal(""hi"", ""hi"")
Assert.AssertNotEqual(1, 2)
Assert.NotEqual(""a"", ""b"")

Assert.AssertTrue(true)
Assert.True(3 > 2)
Assert.AssertFalse(false)
Assert.False(1 > 5)

Assert.AssertNull(null)
Assert.Null(null)
Assert.AssertNotNull(123)
Assert.NotNull(""ok"")

Assert.AssertGreater(10, 5)
Assert.GreaterOrEqual(10, 10)
Assert.AssertLess(3, 8)
Assert.LessOrEqual(8, 8)

equalMethodRef <- Assert.AssertEqual
passed <- true
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("passed"))).Value);
        Assert.IsType<AssertStaticMethodWrapper>(interpreter.Manager.GetValue(new LangId("equalMethodRef")));
    }

    [Fact]
    public void Assert_StringCollectionExceptionAndTypeAssertions_PassWhenValid()
    {
        var code = @"
Assert.AssertContains(""hello world"", ""world"")
Assert.AssertNotContains(""hello world"", ""xyz"")
Assert.AssertStartsWith(""Old8Lang"", ""Old8"")
Assert.AssertEndsWith(""Old8Lang"", ""Lang"")
Assert.AssertMatches(""abc-123"", ""^[a-z]+-[0-9]+$"")

nums <- {10, 20, 30}
empty <- {}
Assert.AssertContainsItem(nums, 20)
Assert.AssertNotContainsItem(nums, 99)
Assert.AssertEmpty(empty)
Assert.AssertNotEmpty(nums)
Assert.AssertLength(nums, 3)

Assert.AssertThrows(() -> { throw ""boom"" })
Assert.AssertNotThrows(() -> { x <- 1 + 1 })

Assert.AssertInstanceOf(42, ""Int"")
Assert.AssertNotInstanceOf(42, ""String"")

passed <- true
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("passed"))).Value);
    }

    [Fact]
    public void Assert_FailedAssertion_ThrowsWithCustomMessage()
    {
        var code = @"
Assert.AssertEqual(1, 2, ""custom failure message"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var ex = Assert.Throws<Exception>(() => ast.Run(interpreter.Manager));
        Assert.Equal("custom failure message", ex.Message);
    }

    #endregion

    #region TestRunner 测试

    [Fact]
    public void TestRunner_DescribeItRunAllAndGetStatistics_TracksSuitesAndResults()
    {
        var code = @"
TestRunner.Clear()

TestRunner.Describe(""MathSuite"", () -> {
    TestRunner.It(""addition works"", () -> {
        Assert.AssertEqual(5, 2 + 3)
    })
    TestRunner.Test(""failing check"", () -> {
        Assert.AssertEqual(10, 99)
    })
})

suiteCount <- TestRunner.GetSuiteCount()
testCount <- TestRunner.GetTestCount()
results <- TestRunner.RunAll()
stats <- TestRunner.GetStatistics(results)

resultCount <- results.Count()
firstName <- results[0][""testName""]
firstPassed <- results[0][""passed""]
secondPassed <- results[1][""passed""]
totalStat <- stats[""total""]
passedStat <- stats[""passed""]
failedStat <- stats[""failed""]

TestRunner.Clear()
";
        var interpreter = new LangInterpreter();
        try
        {
            interpreter.Build(code).Run(interpreter.Manager);
        }
        finally
        {
            var cleanup = new LangInterpreter();
            cleanup.Build("TestRunner.Clear()").Run(cleanup.Manager);
        }

        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("suiteCount"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("testCount"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("resultCount"))).Value);
        Assert.Equal("MathSuite > addition works", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("firstName"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("firstPassed"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("secondPassed"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("totalStat"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("passedStat"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("failedStat"))).Value);
    }

    [Fact]
    public void TestRunner_RunTestSingleExecution_ReturnsPassAndFailDictionaries()
    {
        var code = @"
TestRunner.Clear()

passRes <- TestRunner.RunTest(""single pass"", () -> {
    Assert.AssertTrue(1 == 1)
})
failRes <- TestRunner.RunTest(""single fail"", () -> {
    Assert.AssertTrue(false, ""intentional fail"")
})

passName <- passRes[""testName""]
passOk <- passRes[""passed""]
failOk <- failRes[""passed""]
failErr <- failRes[""errorMessage""]

TestRunner.Clear()
";
        var interpreter = new LangInterpreter();
        try
        {
            interpreter.Build(code).Run(interpreter.Manager);
        }
        finally
        {
            var cleanup = new LangInterpreter();
            cleanup.Build("TestRunner.Clear()").Run(cleanup.Manager);
        }

        Assert.Equal("single pass", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("passName"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("passOk"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("failOk"))).Value);
        Assert.Contains("intentional fail", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("failErr"))).Value);
    }

    #endregion

    #region Mock (MockLibClassLangValue) 测试

    [Fact]
    public void Mock_CreateCallRecordVerifyAndReset_WorksEndToEnd()
    {
        var code = @"
m <- Mock.CreateMock(""UserService"")
mockName <- m.Name

m.SetReturnValue(""getUser"", ""Alice"")
configuredRet <- m.GetReturnValue(""getUser"")

callRet <- m.Call(""getUser"", 101)
m.RecordCall(""saveUser"", ""Bob"", 20)

Mock.VerifyCalled(m, ""getUser"")
Mock.VerifyCalledOnce(m, ""getUser"")
Mock.VerifyCallCount(m, ""saveUser"", 1)
Mock.VerifyNotCalled(m, ""deleteUser"")
Mock.VerifyLastCallArgs(m, ""saveUser"", ""Bob"", 20)

getUserCount <- m.GetCallCount(""getUser"")
allCalls <- m.GetCalls(""saveUser"")
lastArgs <- m.GetLastCall(""saveUser"")
summary <- Mock.GetCallSummary(m)

m.ResetMethod(""getUser"")
afterResetMethod <- m.GetCallCount(""getUser"")
m.Reset()
afterResetAll <- m.GetCallCount(""saveUser"")
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("UserService", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("mockName"))).Value);
        Assert.Equal("Alice", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("configuredRet"))).Value);
        Assert.Equal("Alice", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("callRet"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("getUserCount"))).Value);

        var allCalls = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("allCalls")));
        Assert.Single(allCalls.Values);

        var lastArgs = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("lastArgs")));
        Assert.Equal(2, lastArgs.Values.Count);
        Assert.Equal("Bob", Assert.IsType<StringLangValue>(lastArgs.Values[0]).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(lastArgs.Values[1]).Value);

        var summary = Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("summary"))).Value;
        Assert.Contains("UserService", summary);

        Assert.Equal(0, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("afterResetMethod"))).Value);
        Assert.Equal(0, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("afterResetAll"))).Value);
    }

    [Fact]
    public void Mock_CreateMockWithReturnsStubAndSpy_ConfigureAndVerifyCorrectly()
    {
        var code = @"
m <- Mock.CreateMockWithReturns(""CalcMock"", {""add"": 42, ""ping"": ""pong""})
r1 <- m.Call(""add"", 20, 22)
r2 <- m.Call(""ping"")

stub <- Mock.CreateStub(""DataStub"")
Mock.StubReturns(stub, {""load"": 99})
stubVal <- stub.Call(""load"")

spy <- Mock.CreateSpy(""EventSpy"")
spy.RecordCall(""emit"", ""click"")
spy.RecordCall(""emit"", ""hover"")
Mock.VerifyCallCount(spy, ""emit"", 2)
Mock.VerifyLastCallArgs(spy, ""emit"", ""hover"")
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(42, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("r1"))).Value);
        Assert.Equal("pong", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("r2"))).Value);
        Assert.Equal(99, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("stubVal"))).Value);
    }

    [Fact]
    public void Mock_VerifyCalledOnUncalledMethod_ThrowsException()
    {
        var code = @"
m <- Mock.CreateMock(""EmptyMock"")
Mock.VerifyCalled(m, ""neverCalled"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<Exception>(() => ast.Run(interpreter.Manager));
    }

    #endregion
}
