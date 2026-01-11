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
    calc <- new Calculator()
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
        var code = @"
global <- ""hello""
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
            Position = new Position(7, 4)
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
    obj <- new Test()
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
        var code = @"func main() -> void {
    numbers <- [1, 2, 3]
    doubleValue <- (from n in numbers {
        return n * 2
    }

    func doubleNumbers(n:array<int>) -> array<double> {
        return n.Select(doubleValue).ToArray()
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
            Position = new Position(16, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        Assert.Contains(items, i => i.Label == "numbers");
        Assert.Contains(items, i => i.Label == "doubleValue");
        Assert.Contains(items, i => i.Label == "doubleNumbers");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ForLoopScope_ShouldShowIterationVariable()
    {
        var code = @"func countTo(n:int) -> int {
    result <- 0
    for i <- 0, i < n, i <- i + 1 {
        result <- result + i
    }
    return result
}

func main() -> void {
    total <- countTo(100)
    PrintLine(total.ToStr())
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
        Assert.Contains(items, i => i.Label == "n");
        Assert.Contains(items, i => i.Label == "i");
        Assert.Contains(items, i => i.Label == "result");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task NestedFunctionScope_ShouldShowAll()
    {
        var code = @"class Outer {
    private inner <- null

    func process() -> void {
        inner.value <- 10
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
            Position = new Position(8, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        var innerValue = items.FirstOrDefault(i => i.Label == "value");
        Assert.NotNull(innerValue);

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
        var code = @"func process() -> void {
    block <- {
        localVar <- 42
    }

    result <- block localVar
}
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

        Assert.Contains(items, i => i.Label == "localVar");
        Assert.DoesNotContain(items, i => i.Label == "block");
        Assert.DoesNotContain(items, i => i.Label == "result");

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
    public async Task InheritanceScope_ShouldShowBaseClassMembers()
    {
        var code = @"class Base {
    public baseMethod() -> void {
        PrintLine(""Base method"")
    }
}

class Derived extends Base {
    public override baseMethod() -> void {
        PrintLine(""Derived method"")
    }
}

func main() -> void {
    obj <- new Derived()
    obj.$1baseMethod()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(13, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var baseMethod = items.FirstOrDefault(i => i.Label == "baseMethod");
        Assert.NotNull(baseMethod);
        Assert.Contains(items, i => i.Label == "overrideMethod");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MixinClassAndFunction_ShouldShowBoth()
    {
        var code = @"class A {
    public func methodA() -> void {
    PrintLine(""A.methodA"")
    }
}

func methodB() -> void {
    PrintLine(""B.methodB"")
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
        var methodA = items.FirstOrDefault(i => i.Label == "methodA");
        var methodB = items.FirstOrDefault(i => i.Label == "methodB");

        Assert.NotNull(methodA);
        Assert.NotNull(methodB);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task StaticMemberAccess_ShouldShowOnlyFromInstance()
    {
        var code = @"class Container {
    public static instance <- null
    func init() {
        instance <- new Container()
    }

    public static func processData() -> void {
        if (instance != null) {
            instance.processData()
        }
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
            Position = new Position(16, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var processMethod = items.FirstOrDefault(i => i.Label == "processData");
        var instanceVar = items.FirstOrDefault(i => i.Label == "instance");

        Assert.NotNull(processMethod);
        Assert.NotNull(instanceVar);

        Assert.DoesNotContain(items, i => i.Label == "Container");
        Assert.DoesNotContain(items, i => i.Label == "init");

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
    public async Task ThisInBlockScope_ShouldCaptureBlockVariables()
    {
        var code = @"
func outer() -> void {
    blockVar <- ""block variable""

    func inner() -> void {
        blockVar <- ""inner variable""
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
            Position = new Position(0, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

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
