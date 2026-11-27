using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Services;

/// <summary>
/// 成员访问类型推断、继承/Mixin/接口成员聚合、静态成员补全与跨文件类解析测试。
/// </summary>
public class MemberTypeInferenceTests
{
    private const string Uri = "file:///member-inference.old8";

    [Fact]
    public async Task InheritanceAndMixinMembers_AreCompletedAndNavigable()
    {
        var code =
            "mixin Timestamped {\n" +
            "    public func getTimestamp() -> int {\n" +
            "        return 123\n" +
            "    }\n" +
            "}\n" +
            "class Animal {\n" +
            "    public name: string <- \"\"\n" +
            "    protected secretTag: string <- \"tag\"\n" +
            "    private internalId: int <- 1\n" +
            "    public static speciesCount <- 0\n" +
            "    public func speak() -> string {\n" +
            "        return \"...\"\n" +
            "    }\n" +
            "}\n" +
            "class Dog extends Animal with Timestamped {\n" +
            "    public func bark() -> string {\n" +
            "        return this.secretTag\n" +
            "    }\n" +
            "    public func baseSpeak() -> string {\n" +
            "        return super.speak()\n" +
            "    }\n" +
            "}\n" +
            "func main() -> void {\n" +
            "    dog <- Dog()\n" +
            "    s <- dog.speak()\n" +
            "    t <- dog.getTimestamp()\n" +
            "    c <- Dog.speciesCount\n" +
            "}\n";

        var manager = new DocumentManager();
        manager.UpdateDocument(Uri, code);
        var completion = new CompletionHandler(manager);
        var definition = new DefinitionHandler(manager);

        // 1. 类外部 dog. 补全：包含自身 bark、继承自 Animal 的 name/speak、来自 Mixin 的 getTimestamp；不包含 private/protected
        var dogDot = await completion.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(24, 13) // s <- dog.|speak()
        }, CancellationToken.None);
        var dogLabels = dogDot.Items.Select(i => i.Label).ToList();
        Assert.Contains("bark", dogLabels);
        Assert.Contains("speak", dogLabels);
        Assert.Contains("name", dogLabels);
        Assert.Contains("getTimestamp", dogLabels);
        Assert.DoesNotContain("internalId", dogLabels);
        Assert.DoesNotContain("secretTag", dogLabels);

        // 2. Dog. 静态访问补全：只包含静态成员（含继承自 Animal 的 speciesCount），不包含实例方法 bark / speak
        var staticDot = await completion.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(26, 13) // c <- Dog.|speciesCount
        }, CancellationToken.None);
        var staticLabels = staticDot.Items.Select(i => i.Label).ToList();
        Assert.Contains("speciesCount", staticLabels);
        Assert.DoesNotContain("bark", staticLabels);
        Assert.DoesNotContain("speak", staticLabels);

        // 3. 子类方法内 this. 补全：可见父类的 protected secretTag，但不可见父类的 private internalId
        var thisDot = await completion.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(16, 20) // return this.|secretTag
        }, CancellationToken.None);
        var thisLabels = thisDot.Items.Select(i => i.Label).ToList();
        Assert.Contains("secretTag", thisLabels);
        Assert.Contains("bark", thisLabels);
        Assert.DoesNotContain("internalId", thisLabels);

        // 4. 子类方法内 super. 补全：指向父类 Animal 的 public/protected 成员
        var superDot = await completion.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(19, 21) // return super.|speak()
        }, CancellationToken.None);
        var superLabels = superDot.Items.Select(i => i.Label).ToList();
        Assert.Contains("speak", superLabels);
        Assert.Contains("secretTag", superLabels);
        Assert.DoesNotContain("bark", superLabels);
        Assert.DoesNotContain("internalId", superLabels);

        // 5. 跳转到继承方法定义：dog.speak() 应跳转到 Animal.speak（第 10 行）
        var speakDef = await definition.Handle(new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(24, 14)
        }, CancellationToken.None);
        Assert.Equal(10, Assert.Single(speakDef!).Location!.Range.Start.Line);
    }

    [Fact]
    public async Task PropertyInitializerTypeInferenceAndSignatureHelp_WorkOnMemberChains()
    {
        var code =
            "class Engine {\n" +
            "    public func start(rpm: int, turbo: bool) -> bool {\n" +
            "        return turbo\n" +
            "    }\n" +
            "}\n" +
            "class Car {\n" +
            "    public engine <- Engine()\n" +
            "}\n" +
            "func drive() -> void {\n" +
            "    car <- Car()\n" +
            "    ok <- car.engine.start(3000, true)\n" +
            "}\n";

        var manager = new DocumentManager();
        manager.UpdateDocument(Uri, code);

        // engine 字段无显式类型注解，通过 Engine() 初始化器推断类型
        var completion = new CompletionHandler(manager);
        var result = await completion.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(10, 21) // car.engine.|start
        }, CancellationToken.None);
        Assert.Contains(result.Items, i => i.Label == "start");

        // 成员方法调用的 SignatureHelp
        var sigHandler = new SignatureHelpHandler(manager);
        var sig = await sigHandler.Handle(new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(10, 33) // car.engine.start(3000, |true)
        }, CancellationToken.None);

        Assert.NotNull(sig);
        Assert.Equal(1, sig!.ActiveParameter);
        var info = Assert.Single(sig.Signatures);
        Assert.Contains("rpm:int", info.Label);
        Assert.Contains("turbo:bool", info.Label);
    }

    [Fact]
    public async Task CrossFileClass_MemberCompletionAndHover_WorkViaWorkspaceIndex()
    {
        var manager = new DocumentManager();
        var workspace = new WorkspaceIndex(manager);
        manager.AttachWorkspaceIndex(workspace);

        var modelUri = "file:///models.old8";
        var appUri = "file:///app.old8";

        manager.UpdateDocument(modelUri,
            "class Account {\n" +
            "    public balance: double <- 0.0\n" +
            "    public func deposit(amount: double) -> double {\n" +
            "        return this.balance + amount\n" +
            "    }\n" +
            "}\n");

        manager.UpdateDocument(appUri,
            "func run() -> void {\n" +
            "    acc <- Account()\n" +
            "    total <- acc.deposit(10.0)\n" +
            "}\n");

        var completion = new CompletionHandler(manager, workspace);
        var list = await completion.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(appUri) },
            Position = new Position(2, 17) // acc.|deposit
        }, CancellationToken.None);

        Assert.Contains(list.Items, i => i.Label == "deposit");
        Assert.Contains(list.Items, i => i.Label == "balance");

        var hover = new HoverHandler(manager, workspace);
        var hoverResult = await hover.Handle(new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(appUri) },
            Position = new Position(2, 18) // acc.deposit
        }, CancellationToken.None);

        Assert.NotNull(hoverResult);
        Assert.Contains("deposit", hoverResult!.Contents.MarkupContent!.Value);
    }

    [Fact]
    public void CyclicInheritance_DoesNotInfiniteLoop()
    {
        var code =
            "class A extends B {\n" +
            "    public a <- 1\n" +
            "}\n" +
            "class B extends A {\n" +
            "    public b <- 2\n" +
            "}\n";

        var manager = new DocumentManager();
        var doc = manager.UpdateDocument(Uri, code);
        var aClass = doc.SymbolTable!["A"];

        var members = TypeResolver.GetAllMembers(aClass, doc.SymbolTable);
        Assert.Contains(members, m => m.Name == "a");
        Assert.Contains(members, m => m.Name == "b");
    }
}
