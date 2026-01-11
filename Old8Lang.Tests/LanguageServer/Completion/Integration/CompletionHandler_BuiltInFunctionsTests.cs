using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Integration;

/// <summary>
/// 内置函数补全功能测试
/// 测试 Old8Lang 标准库和扩展库的内置函数补全
/// </summary>
public class CompletionHandler_BuiltInFunctionsTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task PrintFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var printLine = items.FirstOrDefault(i => i.Label == "PrintLine");
        var print = items.FirstOrDefault(i => i.Label == "Print");

        Assert.NotNull(printLine);
        Assert.NotNull(print);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MathFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var sqrt = items.FirstOrDefault(i => i.Label == "Sqrt");
        var abs = items.FirstOrDefault(i => i.Label == "Abs");
        var max = items.FirstOrDefault(i => i.Label == "Max");
        var min = items.FirstOrDefault(i => i.Label == "Min");

        Assert.NotNull(sqrt);
        Assert.NotNull(abs);
        Assert.NotNull(max);
        Assert.NotNull(min);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task StringFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var len = items.FirstOrDefault(i => i.Label == "Len");
        var concat = items.FirstOrDefault(i => i.Label == "Concat");
        var substring = items.FirstOrDefault(i => i.Label == "Substring");
        var contains = items.FirstOrDefault(i => i.Label == "Contains");
        var toStr = items.FirstOrDefault(i => i.Label == "ToStr");
        var replace = items.FirstOrDefault(i => i.Label == "Replace");

        Assert.NotNull(len);
        Assert.NotNull(concat);
        Assert.NotNull(substring);
        Assert.NotNull(contains);
        Assert.NotNull(toStr);
        Assert.NotNull(replace);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task CollectionFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var add = items.FirstOrDefault(i => i.Label == "Add");
        var remove = items.FirstOrDefault(i => i.Label == "Remove");
        var count = items.FirstOrDefault(i => i.Label == "Count");
        var isEmpty = items.FirstOrDefault(i => i.Label == "IsEmpty");
        var clear = items.FirstOrDefault(i => i.Label == "Clear");

        Assert.NotNull(add);
        Assert.NotNull(remove);
        Assert.NotNull(count);
        Assert.NotNull(isEmpty);
        Assert.NotNull(clear);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ConversionFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var toInt = items.FirstOrDefault(i => i.Label == "ToInt");
        var toDouble = items.FirstOrDefault(i => i.Label == "ToDouble");
        var toStr = items.FirstOrDefault(i => i.Label == "ToStr");
        var toBool = items.FirstOrDefault(i => i.Label == "ToBool");
        var toChar = items.FirstOrDefault(i => i.Label == "ToChar");

        Assert.NotNull(toInt);
        Assert.NotNull(toDouble);
        Assert.NotNull(toStr);
        Assert.NotNull(toBool);
        Assert.NotNull(toChar);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task TypeFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var typeofFunc = items.FirstOrDefault(i => i.Label == "TypeOf");
        var isType = items.FirstOrDefault(i => i.Label == "IsType");

        Assert.NotNull(typeofFunc);
        Assert.NotNull(isType);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task AsyncFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var delay = items.FirstOrDefault(i => i.Label == "Delay");
        var whenAll = items.FirstOrDefault(i => i.Label == "WhenAll");
        var whenAny = items.FirstOrDefault(i => i.Label == "WhenAny");
        var awaitFunc = items.FirstOrDefault(i => i.Label == "Await" || i.Label == "await");

        Assert.NotNull(delay);
        Assert.NotNull(whenAll);
        Assert.NotNull(whenAny);
        Assert.NotNull(awaitFunc);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task FileFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var openFile = items.FirstOrDefault(i => i.Label == "OpenFile");
        var readFile = items.FirstOrDefault(i => i.Label == "ReadFile");
        var writeFile = items.FirstOrDefault(i => i.Label == "WriteFile");
        var closeFile = items.FirstOrDefault(i => i.Label == "CloseFile");
        var deleteFile = items.FirstOrDefault(i => i.Label == "DeleteFile");
        var exists = items.FirstOrDefault(i => i.Label == "FileExists");

        Assert.NotNull(openFile);
        Assert.NotNull(readFile);
        Assert.NotNull(writeFile);
        Assert.NotNull(closeFile);
        Assert.NotNull(deleteFile);
        Assert.NotNull(exists);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task OSFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var osInfo = items.FirstOrDefault(i => i.Label == "OsInfo");
        var platform = items.FirstOrDefault(i => i.Label == "Platform");
        var environ = items.FirstOrDefault(i => i.Label == "Environ");
        var getEnv = items.FirstOrDefault(i => i.Label == "GetEnv");

        Assert.NotNull(osInfo);
        Assert.NotNull(platform);
        Assert.NotNull(environ);
        Assert.NotNull(getEnv);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task TerminalFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var clear = items.FirstOrDefault(i => i.Label == "Clear");
        var write = items.FirstOrDefault(i => i.Label == "Write");
        var writeLine = items.FirstOrDefault(i => i.Label == "WriteLine");
        var read = items.FirstOrDefault(i => i.Label == "Read");

        Assert.NotNull(clear);
        Assert.NotNull(write);
        Assert.NotNull(writeLine);
        Assert.NotNull(read);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task JsonFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var parse = items.FirstOrDefault(i => i.Label == "Parse");
        var stringify = items.FirstOrDefault(i => i.Label == "Stringify");
        var jsonType = items.FirstOrDefault(i => i.Label == "JsonType");
        var toJson = items.FirstOrDefault(i => i.Label == "ToJson");

        Assert.NotNull(parse);
        Assert.NotNull(stringify);
        Assert.NotNull(jsonType);
        Assert.NotNull(toJson);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task TimeFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var now = items.FirstOrDefault(i => i.Label == "Now");
        var ticks = items.FirstOrDefault(i => i.Label == "Ticks");
        var unixTime = items.FirstOrDefault(i => i.Label == "UnixTime");
        var format = items.FirstOrDefault(i => i.Label == "Format");

        Assert.NotNull(now);
        Assert.NotNull(ticks);
        Assert.NotNull(unixTime);
        Assert.NotNull(format);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task RandomFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var random = items.FirstOrDefault(i => i.Label == "Random");
        var randomInt = items.FirstOrDefault(i => i.Label == "RandomInt");
        var randomDouble = items.FirstOrDefault(i => i.Label == "RandomDouble");

        Assert.NotNull(random);
        Assert.NotNull(randomInt);
        Assert.NotNull(randomDouble);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task VectorFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var create = items.FirstOrDefault(i => i.Label == "Create");
        var add = items.FirstOrDefault(i => i.Label == "Add");
        var dot = items.FirstOrDefault(i => i.Label == "Dot");
        var cross = items.FirstOrDefault(i => i.Label == "Cross");
        var magnitude = items.FirstOrDefault(i => i.Label == "Magnitude");

        Assert.NotNull(create);
        Assert.NotNull(add);
        Assert.NotNull(dot);
        Assert.NotNull(cross);
        Assert.NotNull(magnitude);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task CryptographyFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var md5 = items.FirstOrDefault(i => i.Label == "Md5");
        var sha256 = items.FirstOrDefault(i => i.Label == "Sha256");
        var sha512 = items.FirstOrDefault(i => i.Label == "Sha512");
        var aesEncrypt = items.FirstOrDefault(i => i.Label == "AesEncrypt");
        var aesDecrypt = items.FirstOrDefault(i => i.Label == "AesDecrypt");

        Assert.NotNull(md5);
        Assert.NotNull(sha256);
        Assert.NotNull(sha512);
        Assert.NotNull(aesEncrypt);
        Assert.NotNull(aesDecrypt);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task AllBuiltInFunctions_ShouldBeAvailable()
    {
        var code = @"";
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
        var functions = items.Where(i => i.Kind == CompletionItemKind.Function).ToList();

        _output.WriteLine($"Total built-in functions: {functions.Count}");
        foreach (var func in functions.Take(20))
        {
            _output.WriteLine($"  - {func.Label}");
        }

        Assert.True(functions.Count > 0, "Should have at least some built-in functions");
    }
}
