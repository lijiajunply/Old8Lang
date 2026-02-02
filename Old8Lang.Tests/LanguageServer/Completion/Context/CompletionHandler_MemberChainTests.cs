using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Context;

/// <summary>
/// 成员访问链补全功能测试
/// 测试多级成员访问（obj.prop.method().nested）的补全
/// </summary>
public class CompletionHandler_MemberChainTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ThreeLevelMemberAccess_ShouldComplete()
    {
        var code = @"class A {
    func getB() -> B {
        return B()
    }
}

class B {
    func getC() -> C {
        return C()
    }
}

class C {
    public value <- 0
    func getValue() -> int {
        return value
    }
}

func main() -> void {
    obj <- A()
    result <- obj.getB().getC().$1getValue()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(27, 10)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var getValueMethod = items.FirstOrDefault(i => i.Label == "getValue");
        Assert.NotNull(getValueMethod);

        output.WriteLine($"Found {items.Count} member items");
        foreach (var item in items)
        {
            output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact]
    public async Task NestedClassMethodCalls_ShouldComplete()
    {
        var code = @"class Outer {
    public inner:Inner <- null
    func init() {
        inner <- Inner()
    }
}

class Inner {
    private nested:Nested <- null
    func init() {
        nested <- Nested()
    }
}

class Nested {
    public value <- 0
    func getValue() -> int {
        return value
    }
}

func main() -> void {
    outer <- Outer()
    result <- outer.inner.$1nested.getValue()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(25, 14)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var getValueMethod = items.FirstOrDefault(i => i.Label == "getValue");
        Assert.NotNull(getValueMethod);

        output.WriteLine($"Found {items.Count} member items");
        foreach (var item in items)
        {
            output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact(Skip = "成员链类型推断功能尚未完全实现")]
    public async Task ChainOfPropertyAccess_ShouldComplete()
    {
        var code = @"class A {
    public b:B <- null
    func init() {
        b <- B()
    }
}

class B {
    public c:C <- null
    func init() {
        c <- C()
    }
}

class C {
    public value <- 0
}

func main() -> void {
    obj <- A()
    result <- obj.b.c.value
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(22, 20) // obj.b. 之后（点号后面）- 第23行(0-based为22)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var cProperty = items.FirstOrDefault(i => i.Label == "c");
        Assert.NotNull(cProperty);

        output.WriteLine($"Found {items.Count} member items");
    }

    [Fact]
    public async Task MethodCallAfterMethodCall_ShouldComplete()
    {
        var code = @"class Calculator {
    public value <- 0

    func add(x:int) -> void {
        this.value <- this.value + x
    }

    func multiply(x:int) -> int {
        return this.value * x
    }
}

func main() -> void {
    calc <- Calculator()
    calc.add(10)
    result <- calc.$1multiply(2)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(18, 10)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var multiplyMethod = items.FirstOrDefault(i => i.Label == "multiply");
        Assert.NotNull(multiplyMethod);

        output.WriteLine($"Found {items.Count} member items");
    }

    [Fact]
    public async Task MemberAccessInExpression_ShouldComplete()
    {
        var code = @"class Person {
    public name <- """"
    public age <- 0
}

func getFullName(person:Person) -> string {
    return person.name + "" ("" + person.age.ToStr() + "")""
}

func main() -> void {
    p <- Person()
    p.name <- ""Alice""
    p.age <- 30
    result <- getFullName(p.name)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(13, 28) // p. 之后
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var nameField = items.FirstOrDefault(i => i.Label == "name");
        Assert.NotNull(nameField);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ChainWithMethodParameters_ShouldComplete()
    {
        var code = @"class Container {
    public item:Item <- null
    func init() {
        item <- Item()
    }
}

class Item {
    public name <- """"
    func process(input:string) -> void {
        PrintLine(""Processing: "" + input)
    }
}

func main() -> void {
    container <- Container()
    container.item.process(""test"")
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(16, 19) // item. 之后
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var processMethod = items.FirstOrDefault(i => i.Label == "process");
        Assert.NotNull(processMethod);

        output.WriteLine($"Found {items.Count} member items");
    }

    [Fact]
    public async Task FourLevelDeepChain_ShouldComplete()
    {
        var code = @"class Level1 {
    public level2:Level2 <- null
    func init() {
        level2 <- Level2()
    }
}

class Level2 {
    public level3:Level3 <- null
    func init() {
        level3 <- Level3()
    }
}

class Level3 {
    public level4:Level4 <- null
    func init() {
        level4 <- Level4()
    }
}

class Level4 {
    public result <- 0
    func getValue() -> int {
        return result
    }
}

func main() -> void {
    obj <- Level1()
    value <- obj.level2.level3.level4.getValue()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(30, 30) // level3. 之后
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var level4Property = items.FirstOrDefault(i => i.Label == "level4");
        Assert.NotNull(level4Property);

        output.WriteLine($"Found {items.Count} member items");
    }

    [Fact]
    public async Task StaticMethodChain_ShouldComplete()
    {
        var code = @"class MathHelper {
    public static func add(a:int, b:int) -> int {
        return a + b
    }

    public static func multiply(a:int, b:int) -> int {
        return a * b
    }
}

func main() -> void {
    sum <- MathHelper.$1add(10, 20)
    product <- MathHelper.$1multiply(5, 6)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(16, 15)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var multiplyMethod = items.FirstOrDefault(i => i.Label == "multiply");
        Assert.NotNull(multiplyMethod);

        output.WriteLine($"Found {items.Count} member items");
    }

    [Fact]
    public async Task ThisInChain_ShouldComplete()
    {
        var code = @"class Outer {
    public inner:Inner <- null
    func init() {
        inner <- Inner()
    }

    func process() -> void {
        inner.value <- 10
    }
}

class Inner {
    public value <- 0
}

func main() -> void {
    obj <- Outer()
    obj.process()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(17, 8) // obj. 之后
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var processMethod = items.FirstOrDefault(i => i.Label == "process");
        Assert.NotNull(processMethod);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task SuperInChain_ShouldComplete()
    {
        var code = @"class Base {
    public value <- 0
    func getValue() -> int {
        return value
    }
}

class Derived extends Base {
    public extra <- 0
    func getSum() -> int {
        return this.value + this.extra
    }
}

func main() -> void {
    obj <- Derived()
    baseValue <- super.$1getValue()
    sum <- obj.$1getSum()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(20, 9)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var getSumMethod = items.FirstOrDefault(i => i.Label == "getSum");
        Assert.NotNull(getSumMethod);

        output.WriteLine($"Found {items.Count} items");
    }
}