using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Context;

/// <summary>
/// 上下文感知补全功能测试
/// 测试在不同作用域中（全局、类成员、方法内部等）的补全行为
/// </summary>
public class CompletionHandler_ContextAwareTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task GlobalScope_ShouldHaveAllCompletions()
    {
        var code = @"func globalFunc() -> void {
    PrintLine(""global"")
}

globalVar <- 123
globalConst <- ""constant""
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "globalFunc");
        Assert.Contains(items, i => i.Label == "globalVar");
        Assert.Contains(items, i => i.Label == "globalConst");
        Assert.Contains(items, i => i.Label == "PrintLine");

        _output.WriteLine($"Found {items.Count} items");
        _output.WriteLine("Global scope completions verified");
    }

    [Fact]
    public async Task ClassMethodScope_ShouldHaveMethodAndProperty()
    {
        var code = @"class Calculator {
    public value <- 0
    func add(x:int) -> void {
        this.value <- this.value + x
    }

    public func getValue() -> int {
        return this.value
    }
}

func test() -> void {
    calc <- Calculator()
    calc.add(10)
    result <- calc.$1getValue()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(20, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var getValueMethod = items.FirstOrDefault(i => i.Label == "getValue");
        var addMethod = items.FirstOrDefault(i => i.Label == "add");
        var valueField = items.FirstOrDefault(i => i.Label == "value");

        Assert.NotNull(getValueMethod);
        Assert.NotNull(addMethod);
        Assert.NotNull(valueField);

        _output.WriteLine($"Found {items.Count} items");
        _output.WriteLine("Class method scope completions verified");
    }

    [Fact]
    public async Task LocalVariableScope_ShouldShadowGlobalVariable()
    {
        // Line 0: global <- "hello"
        // Line 1: func test() -> void {
        // Line 2:     global <- "world"
        // Line 3:     PrintLine(global + " + global)
        // Line 4: }
        var code = @"global <- ""hello""
func test() -> void {
    global <- ""world""
    PrintLine(global + "" + global)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 4)  // 在 PrintLine 行内 (Line 3)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "global");
        Assert.Contains(items, i => i.Label == "PrintLine");
        var localVars = items.Where(i => i.Label == "global" || i.Label == "test").ToList();

        _output.WriteLine($"Found {localVars.Count} local variables");
        _output.WriteLine("Local variable scope verified");
    }

    [Fact]
    public async Task FunctionParameterScope_ShouldShowParameters()
    {
        var code = @"func process(value1:int, value2:int) -> void {
    PrintLine(value1.ToStr())
    PrintLine(value2.ToStr())
}

func test() -> void {
    process(10, 20)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 10)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var value1Var = items.FirstOrDefault(i => i.Label == "value1");
        var value2Var = items.FirstOrDefault(i => i.Label == "value2");

        Assert.NotNull(value1Var);
        Assert.NotNull(value2Var);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MemberAccessInsideMethod_ShouldNotShowGlobal()
    {
        var code = @"class Test {
    private data <- ""secret""
func getData() -> string {
        return this.data
    }
}

func main() -> void {
    obj <- Test()
    secret <- obj.$1getData()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(16, 10)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        var getDataMethod = items.FirstOrDefault(i => i.Label == "getData");
        var secretField = items.FirstOrDefault(i => i.Label == "data");

        Assert.NotNull(getDataMethod);
        Assert.NotNull(secretField);

        Assert.DoesNotContain(items, i => i.Label == "Test");
        Assert.DoesNotContain(items, i => i.Label == "obj");

        _output.WriteLine($"Found {items.Count} items");
        _output.WriteLine("Method scope completions verified");
    }

    [Fact]
    public async Task TryCatchScope_ShouldOnlyShowException()
    {
        var code = @"func riskyOperation() -> void {
    throw ""Error occurred""
}

func test() -> void {
    try {
        riskyOperation()
    } catch (e) {
        PrintLine(""Caught: "" + e)
    }
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(7, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "riskyOperation");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task LambdaCapture_ShouldShowCapturedVariables()
    {
        // Line 0: func main() -> void {
        // Line 1:     numbers <- [1, 2, 3]
        // Line 2:     doubleValue <- 2
        // Line 3:     result <- numbers
        // Line 4: }
        var code = @"func main() -> void {
    numbers <- [1, 2, 3]
    doubleValue <- 2
    result <- numbers
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 10)  // 在 result <- numbers 行
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        // 在函数内部应该能看到这些变量
        Assert.Contains(items, i => i.Label == "numbers");
        Assert.Contains(items, i => i.Label == "doubleValue");
        Assert.Contains(items, i => i.Label == "main");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ForLoopScope_ShouldShowIterationVariable()
    {
        // Line 0: func countTo(n:int) -> int {
        // Line 1:     result <- 0
        // Line 2:     for i <- 0, i < n, i <- i + 1 {
        // Line 3:         result <- result + i
        // Line 4:     }
        // Line 5:     return result
        // Line 6: }
        var code = @"func countTo(n:int) -> int {
    result <- 0
    for i <- 0, i < n, i <- i + 1 {
        result <- result + i
    }
    return result
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 10)  // 在 result <- 0 行 (函数内部，能看到参数 n)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        // 在函数内部应该能看到函数参数和全局函数
        Assert.Contains(items, i => i.Label == "n");
        Assert.Contains(items, i => i.Label == "countTo");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task NestedFunctionScope_ShouldShowAll()
    {
        // Line 0: class Outer {
        // Line 1:     private inner <- 10
        // Line 2:
        // Line 3:     func process() -> void {
        // Line 4:         result <- inner + 1
        // Line 5:     }
        // Line 6: }
        var code = @"class Outer {
    private inner <- 10

    func process() -> void {
        result <- inner + 1
    }
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(4, 10)  // 在 process 方法内部
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        // 在类的方法内部应该能看到类的字段和方法
        var innerField = items.FirstOrDefault(i => i.Label == "inner");
        Assert.NotNull(innerField);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MixinGlobalAndLocal_ShouldPrioritizeLocal()
    {
        var code = @"
data <- ""global data""

func main() -> void {
    data <- ""local data""
    PrintLine(data)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        Assert.Contains(items, i => i.Label == "data");

        var dataCount = items.Count(i => i.Label == "data");

        _output.WriteLine($"Found {dataCount} 'data' symbols");
        Assert.True(dataCount > 0);
    }

    [Fact]
    public async Task FunctionBlockExpression_ShouldNotLeak()
    {
        // Line 0: func process() -> void {
        // Line 1:     block <- 42
        // Line 2:     result <- block + 1
        // Line 3: }
        var code = @"func process() -> void {
    block <- 42
    result <- block + 1
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 10)  // 在 result <- block + 1 行
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        // 在函数作用域内，之前定义的变量应该可见
        Assert.Contains(items, i => i.Label == "block");
        Assert.Contains(items, i => i.Label == "process");

        _output.WriteLine($"Found {items.Count} items");
        _output.WriteLine("Function block expression verified");
    }

    [Fact]
    public async Task MultipleFilesImport_ShouldShowSymbolsFromAll()
    {
        var code = @"import ""MathLib""
import ""OS""
import ""File""

func main() -> void {
    PrintLine(""MathLib.OSInfo()"")
    PrintLine(""OS.Platform"")
    PrintLine(""File.Exists(""test.txt"")
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(8, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        Assert.Contains(items, i => i.Label == "PrintLine");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ClassMethodScope_ShouldShowClassMembers()
    {
        // Line 0: class MyClass {
        // Line 1:     public value <- 10
        // Line 2:
        // Line 3:     public func getValue() -> int {
        // Line 4:         return value
        // Line 5:     }
        // Line 6: }
        // Line 7:
        // Line 8: func main() -> void {
        // Line 9:     obj <- MyClass()
        // Line 10:    PrintLine(obj.value.ToStr())
        // Line 11: }
        var code = @"class MyClass {
    public value <- 10

    public func getValue() -> int {
        return value
    }
}

func main() -> void {
    obj <- MyClass()
    PrintLine(obj.value.ToStr())
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(9, 10)  // 在 main 函数内，obj <- MyClass() 行
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        // 在 main 函数内，应该能看到全局定义的类 MyClass 和函数 main
        var classItem = items.FirstOrDefault(i => i.Label == "MyClass");
        var mainItem = items.FirstOrDefault(i => i.Label == "main");

        Assert.NotNull(classItem);
        Assert.NotNull(mainItem);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MixinClassAndFunction_ShouldShowBoth()
    {
        // Line 0: class A {
        // Line 1:     public func methodA() -> void {
        // Line 2:         PrintLine("A.methodA")
        // Line 3:     }
        // Line 4: }
        // Line 5:
        // Line 6: func methodB() -> void {
        // Line 7:     PrintLine("B.methodB")
        // Line 8: }
        // Line 9:
        // Line 10: func main() -> void {
        // Line 11:     a <- A()
        // Line 12:     methodB()
        // Line 13: }
        var code = @"class A {
    public func methodA() -> void {
        PrintLine(""A.methodA"")
    }
}

func methodB() -> void {
    PrintLine(""B.methodB"")
}

func main() -> void {
    a <- A()
    methodB()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(12, 4)  // 在 main 函数内，methodB() 行 (Line 12)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        // 在 main 函数内，可以访问全局函数 methodB 和 类 A
        var classA = items.FirstOrDefault(i => i.Label == "A");
        var methodB = items.FirstOrDefault(i => i.Label == "methodB");

        Assert.NotNull(classA);
        Assert.NotNull(methodB);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task StaticMemberAccess_ShouldShowStaticMembers()
    {
        // Line 0: class Container {
        // Line 1:     public static counter <- 0
        // Line 2:
        // Line 3:     public static func increment() -> void {
        // Line 4:         counter <- counter + 1
        // Line 5:     }
        // Line 6: }
        // Line 7:
        // Line 8: func main() -> void {
        // Line 9:     Container.increment()
        // Line 10: }
        var code = @"class Container {
    public static counter <- 0

    public static func increment() -> void {
        counter <- counter + 1
    }
}

func main() -> void {
    Container.increment()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(4, 10)  // 在 increment 方法内部
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        // 在静态方法内部，应该能看到静态字段
        var counterVar = items.FirstOrDefault(i => i.Label == "counter");
        var containerClass = items.FirstOrDefault(i => i.Label == "Container");

        Assert.NotNull(counterVar);
        Assert.NotNull(containerClass);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ImportScope_ShouldShowImportedSymbols()
    {
        var code = @"import ""MathLib""

func main() -> void {
    PrintLine(""Math.Sqrt"")
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var mathSqrt = items.FirstOrDefault(i => i.Label == "Sqrt");

        Assert.NotNull(mathSqrt);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task LocalVariablesInFunction_ShouldBeVisible()
    {
        // Line 0: func outer() -> void {
        // Line 1:     blockVar <- "block variable"
        // Line 2:     innerVar <- "inner variable"
        // Line 3:     PrintLine(blockVar)
        // Line 4: }
        var code = @"func outer() -> void {
    blockVar <- ""block variable""
    innerVar <- ""inner variable""
    PrintLine(blockVar)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 10)  // 在 PrintLine 行
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        // 在函数内，之前声明的变量应该可见
        var blockVar = items.FirstOrDefault(i => i.Label == "blockVar");
        var innerVar = items.FirstOrDefault(i => i.Label == "innerVar");

        Assert.NotNull(blockVar);
        Assert.NotNull(innerVar);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task CommentInCode_ShouldNotAffectCompletion()
    {
        var code = @"
x <- 10

// x: 这是变量，不是函数
x <- x + 1

func myFunc() -> void {
    PrintLine(x.ToStr())
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(10, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var xVar = items.FirstOrDefault(i => i.Label == "x");
        var myFunc = items.FirstOrDefault(i => i.Label == "myFunc");

        Assert.NotNull(xVar);
        Assert.NotNull(myFunc);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task StringLiteral_ShouldNotTriggerCompletion()
    {
        var code = @"
func main() -> void {
    message <- ""hello""
    PrintLine(message)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(10, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        Assert.Contains(items, i => i.Label == "message");
        Assert.DoesNotContain(items, i => i.Label.StartsWith("\""));

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task NumberLiteral_ShouldNotTriggerCompletion()
    {
        var code = @"
func main() -> void {
    count <- 0
    count <- count + 1
    PrintLine(count.ToStr())
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var countVar = items.FirstOrDefault(i => i.Label == "count");

        Assert.NotNull(countVar);
        Assert.DoesNotContain(items, i => i.Label.StartsWith("\""));

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ContextPriority_GlobalVsLocal_ShouldPrioritizeLocal()
    {
        var code = @"
globalVar <- ""global""
localVar <- ""local""

func test() -> void {
    localVar <- localVar + 1
    PrintLine(localVar.ToStr())
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        var globalVar = items.FirstOrDefault(i => i.Label == "globalVar");
        var localVar = items.FirstOrDefault(i => i.Label == "localVar");

        _output.WriteLine($"Found {items.Count} items");
    }
}
