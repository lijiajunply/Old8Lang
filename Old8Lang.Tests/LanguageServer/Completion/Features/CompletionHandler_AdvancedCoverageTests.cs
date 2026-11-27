using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Completion.Features;

/// <summary>
/// 补全深度覆盖测试：核验泛型实例化成员、Extern 导入符号、文件头与预编译指令上下文、
/// 联合/交叉类型、多级成员链类型推断以及文档注释展示。
/// </summary>
public class CompletionHandler_AdvancedCoverageTests
{
    [Fact]
    public async Task GenericClassInstantiation_CompletesMembersWithDocumentation()
    {
        var code =
            "class Container<T> {\n" +
            "    /// 当前保存的元素数量\n" +
            "    public count: int <- 0\n" +
            "    /// 获取元素数量\n" +
            "    public func size() -> int {\n" +
            "        return this.count\n" +
            "    }\n" +
            "}\n" +
            "func main() -> void {\n" +
            "    box: Container<int> <- Container<int>()\n" +
            "    n <- box.\n" +
            "}\n";

        var (handler, uri) = Setup(code);
        var result = await CompleteAt(handler, uri, 10, 13);
        var items = result.Items.ToList();

        var sizeItem = items.FirstOrDefault(i => i.Label == "size");
        var countItem = items.FirstOrDefault(i => i.Label == "count");
        Assert.NotNull(sizeItem);
        Assert.NotNull(countItem);
        Assert.Equal(CompletionItemKind.Method, sizeItem.Kind);
        Assert.Contains("获取元素数量", sizeItem.Documentation?.String ?? string.Empty);
    }

    [Fact]
    public async Task ExternDeclarations_AppearInCompletionWithAliasSupport()
    {
        var code =
            "extern \"C#:System\" Math {\n" +
            "    func Pow(x: double, y: double) -> double as Power,\n" +
            "    func Sqrt(x: double) -> double\n" +
            "}\n" +
            "func run() -> double {\n" +
            "    return Pow\n" +
            "}\n";

        var (handler, uri) = Setup(code);
        var result = await CompleteAt(handler, uri, 5, 14);
        var items = result.Items.ToList();

        var powerItem = items.FirstOrDefault(i => i.Label == "Power");
        var sqrtItem = items.FirstOrDefault(i => i.Label == "Sqrt");
        Assert.NotNull(powerItem);
        Assert.NotNull(sqrtItem);
        Assert.Equal(CompletionItemKind.Function, powerItem.Kind);
        Assert.Equal(CompletionItemKind.Function, sqrtItem.Kind);
    }

    [Fact]
    public async Task FileHeaderDirectivesAndCode_CompleteSymbolsCleanly()
    {
        var code =
            "#!encoding utf-8\n" +
            "#!author Old8LangTeam\n" +
            "func greet(name: string) -> string {\n" +
            "    return \"Hi \" + name\n" +
            "}\n" +
            "msg <- gr\n";

        var (handler, uri) = Setup(code);
        var result = await CompleteAt(handler, uri, 5, 9);
        var items = result.Items.ToList();

        Assert.Contains(items, i => i.Label == "greet" && i.Kind == CompletionItemKind.Function);
    }

    [Fact]
    public async Task MultiLevelMemberChain_InfersReturnTypesAcrossClasses()
    {
        var code =
            "class Profile {\n" +
            "    /// 用户昵称\n" +
            "    public nickname: string <- \"\"\n" +
            "}\n" +
            "class User {\n" +
            "    public func getProfile() -> Profile {\n" +
            "        return Profile()\n" +
            "    }\n" +
            "}\n" +
            "class Repo {\n" +
            "    public func findUser(id: int) -> User {\n" +
            "        return User()\n" +
            "    }\n" +
            "}\n" +
            "func load() -> void {\n" +
            "    repo <- Repo()\n" +
            "    name <- repo.findUser(1).getProfile().\n" +
            "}\n";

        var (handler, uri) = Setup(code);
        var result = await CompleteAt(handler, uri, 16, 42);
        var nickname = result.Items.FirstOrDefault(i => i.Label == "nickname");

        Assert.NotNull(nickname);
        Assert.Contains("用户昵称", nickname.Documentation?.String ?? string.Empty);
    }

    [Fact]
    public async Task UnionIntersectionAndExpressions_PreserveScopeAndOperators()
    {
        var code =
            "func format(input: int? | string?) -> string {\n" +
            "    prefix <- \"val:\"\n" +
            "    out <- input != null ? prefix : \"none\"\n" +
            "    return out\n" +
            "}\n";

        var (handler, uri) = Setup(code);
        var result = await CompleteAt(handler, uri, 2, 27);
        var labels = result.Items.Select(i => i.Label).ToHashSet();

        Assert.Contains("input", labels);
        Assert.Contains("prefix", labels);
        Assert.Contains("|", labels);
        Assert.Contains("&", labels);
        Assert.Contains("->", labels);
    }

    [Fact]
    public async Task LambdaAndMatchExpressions_CompleteEnclosingAndLocalSymbols()
    {
        var code =
            "func transform(factor: int) -> int {\n" +
            "    baseVal <- 10\n" +
            "    fn <- (item: int) -> {\n" +
            "        scaled <- item * factor\n" +
            "        return scaled + baseVal\n" +
            "    }\n" +
            "    return fn(2)\n" +
            "}\n";

        var (handler, uri) = Setup(code);
        // Line 4 inside lambda body: should see lambda param `item`, lambda local `scaled`, outer `factor` & `baseVal`
        var result = await CompleteAt(handler, uri, 4, 15);
        var labels = result.Items.Select(i => i.Label).ToHashSet();

        Assert.Contains("item", labels);
        Assert.Contains("scaled", labels);
        Assert.Contains("factor", labels);
        Assert.Contains("baseVal", labels);
    }

    private static (CompletionHandler Handler, string Uri) Setup(string code)
    {
        const string uri = "file:///advanced-completion.old8";
        var dm = new DocumentManager();
        var wi = new WorkspaceIndex(dm);
        dm.AttachWorkspaceIndex(wi);
        dm.UpdateDocument(uri, code);
        return (new CompletionHandler(dm, wi), uri);
    }

    private static Task<CompletionList> CompleteAt(CompletionHandler handler, string uri, int line, int character) =>
        handler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(line, character)
        }, CancellationToken.None);
}
