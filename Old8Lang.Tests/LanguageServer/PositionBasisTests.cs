using MediatR;
using Moq;
using Old8Lang;
using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 位置基准约定的回归测试。
/// </summary>
/// <remarks>
/// 约定（与词法器一致，也正是 LSP 的约定）：
/// <list type="bullet">
/// <item>行号：<see cref="LangToken.Line"/> / <see cref="SourcePosition.Line"/> 从 1 开始，LSP 的
/// <c>Position.Line</c> 从 0 开始，跨界时需要 <c>- 1</c>。</item>
/// <item>列号：<see cref="LangToken.Column"/> / <see cref="SourcePosition.Column"/> 是从行首算起的 0 起始偏移量，
/// LSP 的 <c>Position.Character</c> 同样是 0 起始，跨界时不做任何转换。</item>
/// </list>
/// 这里曾经同时存在两种错误：折叠、语义高亮、诊断、作用域分析误以为列号是 1 起始而多减了 1；
/// 符号表回退路径与文档链接则漏掉了行号转换。下面的用例把各条出口的列号/行号都钉死，
/// 只断言数量（"至少 N 个折叠区域"）是不够的——那正是这些偏差长期没被发现的原因。
/// </remarks>
public class PositionBasisTests(ITestOutputHelper o)
{
    // 第 1 行（0 起始）为空行，所以源码第 2 行（1 起始）对应 LSP 的第 1 行。
    private const string Code =
        "\nfunc testFunction(a:int, b:int) -> int {\n    return a + b\n}\n\nresult <- testFunction(1, 2)\n";

    private const string Uri = "file:///position-basis.old8";

    private static string[] Lines(string code) => code.Split('\n');

    private static DocumentManager ManagerWith(string code)
    {
        var manager = new DocumentManager();
        manager.UpdateDocument(Uri, code);
        return manager;
    }

    [Fact]
    public void Tokenizer_UsesOneBasedLineAndZeroBasedColumn()
    {
        var tokens = LangTokenizer.Tokenize(Code);

        var func = tokens.Single(t => t is { Type: LangTokenType.Func, Value: "func" });
        Assert.Equal(2, func.Line);   // 源码第 2 行（1 起始）
        Assert.Equal(0, func.Column); // 行首（0 起始）

        var name = tokens.First(t => t is { Type: LangTokenType.Identifier, Value: "testFunction" });
        Assert.Equal(2, name.Line);
        Assert.Equal(5, name.Column); // "func " 之后

        var call = tokens.Last(t => t is { Type: LangTokenType.Identifier, Value: "testFunction" });
        Assert.Equal(6, call.Line);
        Assert.Equal(10, call.Column); // "result <- " 之后
    }

    [Fact]
    public void SourcePosition_ColumnIsZeroBased_WhileToStringRendersOneBased()
    {
        var position = new SourcePosition(2, 5);

        Assert.Equal(5, position.Column); // 原始列号保持 0 起始
        Assert.Equal("2:6", position.ToString()); // 面向人的展示才转成 1 起始
    }

    [Fact]
    public async Task FoldingRange_BraceFold_UsesZeroBasedCharacters()
    {
        var handler = new FoldingRangeHandler(ManagerWith(Code));

        var ranges = (await handler.Handle(
            new FoldingRangeRequestParam { TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) } },
            CancellationToken.None))!.ToList();

        var fold = Assert.Single(ranges);
        var source = Lines(Code);

        // 起点是函数体的 '{'，终点是 '}' 的下一列（LSP 的结束位置是独占的）
        Assert.Equal(1, fold.StartLine);
        Assert.Equal(source[1].IndexOf('{'), fold.StartCharacter);
        Assert.Equal(3, fold.EndLine);
        Assert.Equal(source[3].IndexOf('}') + 1, fold.EndCharacter);
    }

    [Fact]
    public async Task SemanticTokens_AreEmittedAtTheZeroBasedTokenColumns()
    {
        var handler = new SemanticTokensHandler(ManagerWith(Code));

        var data = (await handler.Handle(
            new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) } },
            CancellationToken.None))!.Data!.ToArray();

        // 曾经的回归：图例数组被声明成 string[]，Array.IndexOf 退化成用 Equals 比较
        // string 与 SemanticTokenType，索引恒为 -1，于是这里一个标记都不会产生。
        Assert.NotEmpty(data);

        var source = Lines(Code);
        var decoded = new List<string>();
        var line = 0;
        var startCharacter = 0;

        for (var i = 0; i + 4 < data.Length; i += 5)
        {
            line += data[i];
            startCharacter = data[i] == 0 ? startCharacter + data[i + 1] : data[i + 1];
            var length = data[i + 2];

            Assert.InRange(line, 0, source.Length - 1);
            Assert.InRange(startCharacter, 0, source[line].Length);
            Assert.InRange(startCharacter + length, 0, source[line].Length);

            o.WriteLine($"token line={line} char={startCharacter} len={length} type={data[i + 3]}");
            decoded.Add(source[line].Substring(startCharacter, length));
        }

        // 每个标记覆盖的源码片段必须正好是它标出的那个词元，
        // 列号只要偏移一格，取出来的片段就会错位。
        Assert.Equal(
            ["func", "testFunction", "a", "int", "b", "int", "->", "int",
             "return", "a", "+", "b",
             "result", "<-", "testFunction", "1", "2"],
            decoded);
    }

    [Fact]
    public void SymbolTable_AllLocationsUseZeroBasedLineAndColumn()
    {
        var document = ManagerWith(Code).GetDocument(Uri)!;
        var source = Lines(Code);
        var seen = new List<string>();

        foreach (var (name, symbol) in document.SymbolTable!)
        {
            AssertLocation(symbol.Location, name, source, seen);

            foreach (var parameter in symbol.Parameters)
                AssertLocation(parameter.Location, $"{name}.{parameter.Name}", source, seen);

            foreach (var (memberName, member) in symbol.Members)
                AssertLocation(member.Location, $"{name}.{memberName}", source, seen);
        }

        Assert.Contains("testFunction", seen);
        Assert.Contains("testFunction.a", seen);
        Assert.Contains("testFunction.b", seen);
        Assert.Contains("result", seen);

        // 参数走的是 AST 回退路径（token 查不到），这里曾经漏掉了行号转换，返回 1 起始的行号
        var a = document.SymbolTable["testFunction"].Parameters.Single(p => p.Name == "a");
        const int declarationLine = 1; // 0 起始：Code 的第 0 行是空行，func 声明落在第 1 行
        Assert.Equal(declarationLine, a.Location.Line);
        Assert.Equal(source[declarationLine].IndexOf('a'), a.Location.Column);
    }

    [Fact]
    public void ScopeAnalyzer_LocalSymbolColumnsAreZeroBased()
    {
        var document = ManagerWith(Code).GetDocument(Uri)!;
        var source = Lines(Code);

        // 光标放在 "return a + b" 这一行内部，以便收集到函数作用域的局部符号
        var analyzer = new ScopeAnalyzer(document.Ast!, new Position(2, 4), document.SymbolTable!, Uri);
        var visible = analyzer.GetVisibleSymbols();
        var tokens = LangTokenizer.Tokenize(Code);

        var checkedNames = new List<string>();
        foreach (var symbol in visible)
        {
            // 每个符号报出的位置必须正好落在它自己的标识符词元上，列号偏移一格就会对不上
            var token = tokens.First(t => t.Type == LangTokenType.Identifier && t.Value == symbol.Name);
            Assert.Equal(token.Line - 1, symbol.Location.Line);
            Assert.Equal(token.Column, symbol.Location.Column);
            Assert.Equal(source[token.Line - 1].Length, source[symbol.Location.Line].Length);
            checkedNames.Add(symbol.Name);
        }

        Assert.Contains("a", checkedNames);
        Assert.Contains("b", checkedNames);
    }

    [Fact]
    public async Task PublishedDiagnostics_UseZeroBasedLineAndColumn()
    {
        // 未定义符号会落在行内某个非零列上，正好能暴露多减 1 的问题
        var code = "x <- 1\ny <- undefinedThing\n";
        var source = Lines(code);
        var expectedColumn = source[1].IndexOf("undefinedThing", StringComparison.Ordinal);

        var documentManager = new DocumentManager();
        var published = new List<PublishDiagnosticsParams>();

        // PublishDiagnostics 是扩展方法，无法直接 Mock，改为拦截它底层发出的通知
        var textDocument = new Mock<ITextDocumentLanguageServer>();
        textDocument
            .Setup(x => x.SendNotification(It.IsAny<IRequest>()))
            .Callback<IRequest>(request => published.Add((PublishDiagnosticsParams)request));

        var facade = new Mock<ILanguageServerFacade>();
        facade.SetupGet(x => x.TextDocument).Returns(textDocument.Object);

        var handler = new TextDocumentSyncHandler(documentManager, facade.Object);
        await handler.Handle(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem { Uri = new Uri(Uri), LanguageId = "old8lang", Version = 1, Text = code }
        }, CancellationToken.None);

        var info = documentManager.GetDocument(Uri)!.Diagnostics.Single(d => d.Message.Contains("undefinedThing"));
        var range = published.Single().Diagnostics.Single(d => d.Message.Contains("undefinedThing")).Range;

        Assert.True(info.Column > 0, "用例本身要求诊断落在非零列上，否则测不出列偏移");
        Assert.Equal(info.Line - 1, range.Start.Line); // 行号：1 起始 -> 0 起始
        Assert.Equal(info.Column, range.Start.Character); // 列号：两边都是 0 起始
        Assert.Equal(expectedColumn, range.Start.Character);
    }

    private static void AssertLocation(SourceLocation location, string label, string[] source, List<string> seen)
    {
        Assert.InRange(location.Line, 0, source.Length - 1);
        Assert.InRange(location.Column, 0, source[location.Line].Length);
        Assert.Equal(location.Line, location.EndLine);
        Assert.True(location.EndColumn >= location.Column, $"{label} 的结束列不应早于起始列");
        seen.Add(label);
    }
}
