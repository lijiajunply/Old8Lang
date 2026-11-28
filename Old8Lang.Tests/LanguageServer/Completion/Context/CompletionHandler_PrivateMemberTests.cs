using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Completion.Context;

/// <summary>
/// 私有与受保护成员补全可见性测试：验证按访问位置（外部、本类 this/同类实例、子类 this/super、
/// 静态类名访问、类方法体裸名称）精确控制 private / protected / public 成员展示。
/// </summary>
public class CompletionHandler_PrivateMemberTests
{
    private const string HierarchyCode =
        "class Base {\n" +                                          // 0
        "    public basePub: int <- 1\n" +                          // 1
        "    protected baseProt: int <- 2\n" +                      // 2
        "    private basePriv: int <- 3\n" +                        // 3
        "    public static basePubStatic: int <- 10\n" +            // 4
        "    protected static baseProtStatic: int <- 20\n" +        // 5
        "    private static basePrivStatic: int <- 30\n" +          // 6
        "    public func basePubMethod() -> int { return 1 }\n" +   // 7
        "    protected func baseProtMethod() -> int { return 2 }\n" + // 8
        "    private func basePrivMethod() -> int { return 3 }\n" + // 9
        "    public func inspectSelf(other: Base) -> void {\n" +    // 10
        "        a <- this.\n" +                                    // 11
        "        b <- other.\n" +                                   // 12
        "        c <- Base.\n" +                                    // 13
        "    }\n" +                                                 // 14
        "}\n" +                                                     // 15
        "\n" +                                                      // 16
        "class Child extends Base {\n" +                            // 17
        "    private childPriv: int <- 4\n" +                       // 18
        "    public func inspectChild() -> void {\n" +              // 19
        "        x <- this.\n" +                                    // 20
        "        y <- super.\n" +                                   // 21
        "        z <- Base.\n" +                                    // 22
        "        bare <- 0\n" +                                     // 23
        "    }\n" +                                                 // 24
        "}\n" +                                                     // 25
        "\n" +                                                      // 26
        "func externalCaller() -> void {\n" +                       // 27
        "    obj <- Base()\n" +                                     // 28
        "    e1 <- obj.\n" +                                        // 29
        "    e2 <- Base.\n" +                                       // 30
        "}\n";                                                      // 31

    [Fact]
    public async Task ExternalAccess_ShowsOnlyPublicMembers()
    {
        var (handler, uri) = Setup(HierarchyCode);

        // Line 29: `    e1 <- obj.` (dot at col 13, cursor at 14)
        var result = await CompleteAtAsync(handler, uri, 29, 14);
        var labels = result.Items.Select(i => i.Label).ToHashSet();

        Assert.Contains("basePub", labels);
        Assert.Contains("basePubMethod", labels);
        Assert.DoesNotContain("baseProt", labels);
        Assert.DoesNotContain("baseProtMethod", labels);
        Assert.DoesNotContain("basePriv", labels);
        Assert.DoesNotContain("basePrivMethod", labels);
    }

    [Fact]
    public async Task ExternalAccessOnAllPrivateClass_ReturnsEmptyWithoutLeakingGlobals()
    {
        var code =
            "class Vault {\n" +
            "    private secret: string <- \"hidden\"\n" +
            "    private func unlock() -> void {}\n" +
            "}\n" +
            "v <- Vault()\n" +
            "x <- v.\n";

        var (handler, uri) = Setup(code);
        var result = await CompleteAtAsync(handler, uri, 5, 7);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SameClassAccess_ThisAndOtherInstance_ShowPrivateProtectedAndPublic()
    {
        var (handler, uri) = Setup(HierarchyCode);

        // Line 11: `        a <- this.` (col 18)
        var thisResult = await CompleteAtAsync(handler, uri, 11, 18);
        var thisLabels = thisResult.Items.Select(i => i.Label).ToHashSet();
        Assert.Contains("basePub", thisLabels);
        Assert.Contains("baseProt", thisLabels);
        Assert.Contains("basePriv", thisLabels);
        Assert.Contains("basePrivMethod", thisLabels);

        // Line 12: `        b <- other.` (col 19)
        var otherResult = await CompleteAtAsync(handler, uri, 12, 19);
        var otherLabels = otherResult.Items.Select(i => i.Label).ToHashSet();
        Assert.Contains("basePub", otherLabels);
        Assert.Contains("baseProt", otherLabels);
        Assert.Contains("basePriv", otherLabels);
        Assert.Contains("basePrivMethod", otherLabels);
    }

    [Fact]
    public async Task SubclassAccess_ThisAndSuper_ShowProtectedAndHideBasePrivate()
    {
        var (handler, uri) = Setup(HierarchyCode);

        // Line 20: `        x <- this.` in Child (col 18)
        var childThis = await CompleteAtAsync(handler, uri, 20, 18);
        var childThisLabels = childThis.Items.Select(i => i.Label).ToHashSet();
        Assert.Contains("childPriv", childThisLabels);
        Assert.Contains("basePub", childThisLabels);
        Assert.Contains("baseProt", childThisLabels);
        Assert.Contains("baseProtMethod", childThisLabels);
        Assert.DoesNotContain("basePriv", childThisLabels);
        Assert.DoesNotContain("basePrivMethod", childThisLabels);

        // Line 21: `        y <- super.` in Child (col 19)
        var childSuper = await CompleteAtAsync(handler, uri, 21, 19);
        var childSuperLabels = childSuper.Items.Select(i => i.Label).ToHashSet();
        Assert.Contains("basePub", childSuperLabels);
        Assert.Contains("baseProt", childSuperLabels);
        Assert.Contains("baseProtMethod", childSuperLabels);
        Assert.DoesNotContain("basePriv", childSuperLabels);
        Assert.DoesNotContain("basePrivMethod", childSuperLabels);
    }

    [Fact]
    public async Task StaticAccess_RespectsPrivateProtectedAndPublicVisibilityByLocation()
    {
        var (handler, uri) = Setup(HierarchyCode);

        // Line 13: `Base.` inside Base -> sees private/protected/public static
        var insideBase = (await CompleteAtAsync(handler, uri, 13, 18)).Items.Select(i => i.Label).ToHashSet();
        Assert.Contains("basePubStatic", insideBase);
        Assert.Contains("baseProtStatic", insideBase);
        Assert.Contains("basePrivStatic", insideBase);

        // Line 22: `Base.` inside Child -> sees protected/public static, NOT private static
        var insideChild = (await CompleteAtAsync(handler, uri, 22, 18)).Items.Select(i => i.Label).ToHashSet();
        Assert.Contains("basePubStatic", insideChild);
        Assert.Contains("baseProtStatic", insideChild);
        Assert.DoesNotContain("basePrivStatic", insideChild);

        // Line 30: `Base.` outside -> sees ONLY public static
        var outside = (await CompleteAtAsync(handler, uri, 30, 15)).Items.Select(i => i.Label).ToHashSet();
        Assert.Contains("basePubStatic", outside);
        Assert.DoesNotContain("baseProtStatic", outside);
        Assert.DoesNotContain("basePrivStatic", outside);
    }

    [Fact]
    public async Task BareNameInsideSubclassMethod_IncludesOwnPrivateAndInheritedProtectedOnly()
    {
        var (handler, uri) = Setup(HierarchyCode);

        // Line 23: `        bare <- 0` inside Child.inspectChild
        var bareResult = await CompleteAtAsync(handler, uri, 23, 8);
        var labels = bareResult.Items.Select(i => i.Label).ToHashSet();

        Assert.Contains("childPriv", labels);
        Assert.Contains("basePub", labels);
        Assert.Contains("baseProt", labels);
        Assert.Contains("baseProtMethod", labels);
        Assert.DoesNotContain("basePriv", labels);
        Assert.DoesNotContain("basePrivMethod", labels);
    }

    private static (CompletionHandler Handler, string Uri) Setup(string code)
    {
        const string uri = "file:///private-visibility.old8";
        var dm = new DocumentManager();
        var wi = new WorkspaceIndex(dm);
        dm.AttachWorkspaceIndex(wi);
        dm.UpdateDocument(uri, code);
        return (new CompletionHandler(dm, wi), uri);
    }

    private static Task<CompletionList> CompleteAtAsync(CompletionHandler handler, string uri, int line, int character) =>
        handler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(line, character)
        }, CancellationToken.None);
}
