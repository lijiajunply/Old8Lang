using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Exceptions;

/// <summary>
/// 编译器模式下的异常处理测试 - 错误传播测试
/// </summary>
public class ErrorPropagationTests
{
    private readonly ITestOutputHelper _output;

    public ErrorPropagationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ErrorPropagatesThroughFunctionCalls_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func innerFunction() -> int {
                throw ""Inner error""
            }
            
            func outerFunction() -> int {
                return innerFunction()
            }
            
            caughtError <- """"
            try {
                result <- outerFunction()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Inner error"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughClassMethods_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class MyClass {
                public func method1() -> int {
                    return this.method2()
                }
                
                public func method2() -> int {
                    throw ""Method error""
                }
            }
            
            instance <- MyClass()
            caughtError <- """"
            
            try {
                result <- instance.method1()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Method error"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughLoops_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func processItems(items:list) -> void {
                i <- 0
                while i < items.Count() {
                    if items[i] == 3 {
                        throw ""Error at item 3""
                    }
                    i <- i + 1
                }
            }
            
            caughtError <- """"
            try {
                processItems({1, 2, 3, 4, 5})
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Error at item 3"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughConditionals_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func conditionalError(x:int) -> int {
                if x > 5 {
                    throw ""X is too large""
                }
                return x
            }
            
            caughtError <- """"
            try {
                result <- conditionalError(10)
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""X is too large"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughNestedTryCatch_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func level3() -> int {
                throw ""Level 3 error""
            }
            
            func level2() -> int {
                try {
                    return level3()
                } catch (e) {
                    throw ""Level 2 error: "" + e
                }
            }
            
            func level1() -> int {
                try {
                    return level2()
                } catch (e) {
                    throw ""Level 1 error: "" + e
                }
            }
            
            caughtError <- """"
            try {
                result <- level1()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Level 1 error: Level 2 error: Level 3 error"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughLambda_CompilesAndExecutesCorrectly()
    {
        var code = @"
            processor <- func(value:int) -> int {
                if value == 5 {
                    throw ""Invalid value 5""
                }
                return value * 2
            }
            
            caughtError <- """"
            try {
                result <- processor(5)
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Invalid value 5"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughCollections_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func processList(items:list) -> list {
                results <- {}
                i <- 0
                while i < items.Count() {
                    item <- items[i]
                    if item == ""error"" {
                        throw ""Found error in list""
                    }
                    results.Add(item.ToUpper())
                    i <- i + 1
                }
                return results
            }
            
            caughtError <- """"
            try {
                result <- processList({""hello"", ""world"", ""error"", ""test""})
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Found error in list"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class BaseClass {
                public func doWork() -> int {
                    throw ""Base class error""
                }
            }
            
            class DerivedClass : BaseClass {
                public func doWork() -> int {
                    return super.doWork()
                }
            }
            
            instance <- DerivedClass()
            caughtError <- """"
            
            try {
                result <- instance.doWork()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Base class error"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughSwitch_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func processSwitch(value:int) -> string {
                result <- """"
                switch value {
                    case 1 -> {
                        result <- ""one""
                    }
                    case 2 -> {
                        throw ""Error at case 2""
                    }
                    case 3 -> {
                        result <- ""three""
                    }
                    default -> {
                        result <- ""other""
                    }
                }
                return result
            }
            
            caughtError <- """"
            try {
                result <- processSwitch(2)
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Error at case 2"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughMatch_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func processMatch(value:int) -> string {
                result <- match value {
                    case 1 -> ""one""
                    case 2 -> {
                        throw ""Error at case 2""
                    }
                    case 3 -> ""three""
                    case _ -> ""other""
                }
                return result
            }
            
            caughtError <- """"
            try {
                result <- processMatch(2)
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Error at case 2"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagatesThroughRecursion_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func recursiveFunction(n:int) -> int {
                if n <= 0 {
                    throw ""Base case error""
                }
                return n + recursiveFunction(n - 1)
            }
            
            caughtError <- """"
            try {
                result <- recursiveFunction(5)
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Base case error"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagationWithFinally_CompilesAndExecutesCorrectly()
    {
        var code = @"
            cleanupExecuted <- false
            finallyExecuted <- false
            
            func riskyOperation() -> int {
                try {
                    cleanupExecuted <- true
                    throw ""Operation failed""
                } finally {
                    finallyExecuted <- true
                }
            }
            
            caughtError <- """"
            try {
                result <- riskyOperation()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Operation failed"", caughtError)
            Assert.True(cleanupExecuted)
            Assert.True(finallyExecuted)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagationWithMultipleCatchBlocks_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func throwError(type:string) -> void {
                if type == ""int"" {
                    throw ""Integer error""
                } else if type == ""string"" {
                    throw ""String error""
                } else {
                    throw ""Generic error""
                }
            }
            
            caughtError <- """"
            try {
                throwError(""int"")
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Integer error"", caughtError)
            
            caughtError <- """"
            try {
                throwError(""string"")
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""String error"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagationThroughChainedOperations_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func step1(x:int) -> int {
                return x + 10
            }
            
            func step2(x:int) -> int {
                if x > 15 {
                    throw ""Value too large in step2""
                }
                return x * 2
            }
            
            func step3(x:int) -> string {
                return ""result: "" + x.ToStr()
            }
            
            caughtError <- """"
            try {
                result1 <- step1(10)
                result2 <- step2(result1)
                result3 <- step3(result2)
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Value too large in step2"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagationThroughAsync_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func asyncError() -> string {
                await Task.Delay(50)
                throw ""Async error occurred""
            }
            
            async func testAsyncError() {
                caughtError <- """"
                try {
                    result <- await asyncError()
                } catch (e) {
                    caughtError <- e
                }
                
                Assert.Equal(""Async error occurred"", caughtError)
            }
            
            testAsyncError()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagationWithRethrow_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func inner() -> int {
                throw ""Original error""
            }
            
            func middle() -> int {
                try {
                    return inner()
                } catch (e) {
                    throw ""Wrapped error: "" + e
                }
            }
            
            func outer() -> int {
                try {
                    return middle()
                } catch (e) {
                    throw ""Final error: "" + e
                }
            }
            
            caughtError <- """"
            try {
                result <- outer()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Final error: Wrapped error: Original error"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagationWithMultipleLevels_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func level5() -> int {
                throw ""Level 5""
            }
            
            func level4() -> int {
                return level5()
            }
            
            func level3() -> int {
                return level4()
            }
            
            func level2() -> int {
                return level3()
            }
            
            func level1() -> int {
                return level2()
            }
            
            caughtError <- """"
            try {
                result <- level1()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Level 5"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ErrorPropagationWithComplexDataStructures_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class DataProcessor {
                public data : list
                
                func init(data:list) {
                    this.data <- data
                }
                
                public func process() -> void {
                    i <- 0
                    while i < this.data.Count() {
                        item <- this.data[i]
                        if item == ""error"" {
                            throw ""Error in data processing""
                        }
                        i <- i + 1
                    }
                }
            }
            
            processor <- DataProcessor({""ok"", ""ok"", ""error"", ""ok""})
            caughtError <- """"
            
            try {
                processor.process()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Error in data processing"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
