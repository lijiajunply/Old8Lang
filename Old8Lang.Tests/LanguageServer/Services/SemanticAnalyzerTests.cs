using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Services;

public class SemanticAnalyzerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestUndefinedSymbolDetection()
    {
        var code = @"
a <- 10
b <- undefinedVar
PrintLine(a)
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            RootScope = symbolTableBuilder.RootScope,
            Diagnostics = []
        };

        var analyzer = new SemanticAnalyzer(document);
        var diagnostics = analyzer.Analyze();

        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("undefinedVar"));
    }

    [Fact]
    public void TestDuplicateDefinitionDetection()
    {
        var code = @"
func foo() -> void {
    PrintLine(""foo"")
}

func foo() -> int {
    return 42
}
";
        var manager = new DocumentManager();
        var doc = manager.UpdateDocument("file:///dup.old8", code);

        foreach (var diag in doc.Diagnostics)
        {
            testOutputHelper.WriteLine($"  [{diag.Severity}] {diag.Message}");
        }

        Assert.Contains(doc.Diagnostics,
            d => d.Severity == DiagnosticSeverity.Error && d.Message.Contains("重复定义") && d.Message.Contains("foo"));
    }

    [Fact]
    public void TypeMismatch_AssignmentReturnAndCallArguments_ReportErrors()
    {
        var code =
            "func add(a: int, b: int) -> int {\n" +
            "    return \"not an int\"\n" +
            "}\n" +
            "func test() -> void {\n" +
            "    x: int <- \"wrong\"\n" +
            "    y <- add(\"bad\", 2)\n" +
            "    z <- add(1)\n" +
            "    PrintLine(x)\n" +
            "    PrintLine(y)\n" +
            "    PrintLine(z)\n" +
            "}\n";

        var manager = new DocumentManager();
        var doc = manager.UpdateDocument("file:///type-mismatch.old8", code);

        var errors = doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Contains(errors, d => d.Message.Contains("返回类型不匹配"));
        Assert.Contains(errors, d => d.Message.Contains("类型不匹配") && d.Message.Contains("'x'"));
        Assert.Contains(errors, d => d.Message.Contains("参数类型不匹配") && d.Message.Contains("'a'"));
        Assert.Contains(errors, d => d.Message.Contains("参数数量不匹配") && d.Message.Contains("'add'"));
    }

    [Fact]
    public void TypeCompatibility_NumericWideningNullableInheritanceAndUnion_NoFalsePositives()
    {
        var code =
            "class Animal {\n" +
            "    public name: string\n" +
            "}\n" +
            "class Dog extends Animal {\n" +
            "}\n" +
            "func demo(flag: bool) -> int | string {\n" +
            "    d: double <- 42\n" +
            "    n: int? <- null\n" +
            "    pet: Animal <- Dog()\n" +
            "    PrintLine(d)\n" +
            "    PrintLine(n)\n" +
            "    PrintLine(pet.name)\n" +
            "    if flag {\n" +
            "        return 1\n" +
            "    }\n" +
            "    return \"ok\"\n" +
            "}\n";

        var manager = new DocumentManager();
        var doc = manager.UpdateDocument("file:///type-ok.old8", code);

        var errors = doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void UnusedLocalVariable_ReportsWarning_AndIgnoresUnderscoreOrUsed()
    {
        var code =
            "func compute() -> int {\n" +
            "    used <- 10\n" +
            "    unused <- 20\n" +
            "    _ignored <- 30\n" +
            "    return used\n" +
            "}\n";

        var manager = new DocumentManager();
        var doc = manager.UpdateDocument("file:///unused.old8", code);

        var warnings = doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
        var unusedWarning = Assert.Single(warnings);
        Assert.Contains("未使用的变量 'unused'", unusedWarning.Message);
        Assert.DoesNotContain(doc.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void MemberDiagnostics_DetectNonExistentMemberStaticMisuseAndPrivateAccess()
    {
        var code =
            "class Base {\n" +
            "    public func baseOk() -> int {\n" +
            "        return 1\n" +
            "    }\n" +
            "}\n" +
            "class User extends Base {\n" +
            "    private secret: int <- 7\n" +
            "    public func greet() -> string {\n" +
            "        return \"hi\"\n" +
            "    }\n" +
            "}\n" +
            "func main() -> void {\n" +
            "    u <- User()\n" +
            "    a <- u.baseOk()\n" +
            "    b <- u.ToStr()\n" +
            "    c <- u.nonExistent()\n" +
            "    d <- u.secret\n" +
            "    e <- User.greet()\n" +
            "    PrintLine(a)\n" +
            "    PrintLine(b)\n" +
            "    PrintLine(c)\n" +
            "    PrintLine(d)\n" +
            "    PrintLine(e)\n" +
            "}\n";

        var manager = new DocumentManager();
        var doc = manager.UpdateDocument("file:///member-diag.old8", code);

        var errors = doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Contains(errors, d => d.Message.Contains("不存在成员 'nonExistent'"));
        Assert.Contains(errors, d => d.Message.Contains("私有成员 'secret'"));
        Assert.Contains(errors, d => d.Message.Contains("静态访问实例成员 'greet'"));
        Assert.DoesNotContain(errors, d => d.Message.Contains("baseOk"));
        Assert.DoesNotContain(errors, d => d.Message.Contains("ToStr"));
    }

    [Fact]
    public void CrossFileSemanticAnalysis_ResolvesImportedSymbolsAndDetectsMissingExports()
    {
        var root = Path.Combine(Path.GetTempPath(), "old8lang-sem-xfile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var libPath = Path.Combine(root, "lib.old8");
            var mainPath = Path.Combine(root, "main.old8");

            File.WriteAllText(libPath,
                "class Point {\n" +
                "    public x: int <- 0\n" +
                "}\n" +
                "func doubleIt(n: int) -> int {\n" +
                "    return n * 2\n" +
                "}\n");

            var mainCode =
                "import { doubleIt, missingSym } from \"./lib.old8\"\n" +
                "import \"./ghost.old8\"\n" +
                "func run() -> void {\n" +
                "    ok <- doubleIt(5)\n" +
                "    bad <- doubleIt(\"str\")\n" +
                "    p <- Point()\n" +
                "    x <- p.x\n" +
                "    m <- p.nope\n" +
                "    PrintLine(ok)\n" +
                "    PrintLine(bad)\n" +
                "    PrintLine(x)\n" +
                "    PrintLine(m)\n" +
                "}\n";
            File.WriteAllText(mainPath, mainCode);

            var manager = new DocumentManager();
            var workspace = new WorkspaceIndex(manager);
            workspace.SetRoots([root]);
            manager.AttachWorkspaceIndex(workspace);

            var mainUri = DocumentUri.FromFileSystemPath(mainPath).ToString();
            var doc = manager.UpdateDocument(mainUri, mainCode);

            var errors = doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            // 跨文件已导入/工作区声明的 doubleIt 和 Point 不应报未定义
            Assert.DoesNotContain(errors, d => d.Message.Contains("未定义的符号 'doubleIt'"));
            Assert.DoesNotContain(errors, d => d.Message.Contains("未定义的符号 'Point'"));
            // 缺失导出符号、缺失本地模块、跨文件参数类型不匹配、跨文件类成员不存在均应检出
            Assert.Contains(errors, d => d.Message.Contains("不存在导出符号 'missingSym'"));
            Assert.Contains(errors, d => d.Message.Contains("无法解析导入的模块 './ghost.old8'"));
            Assert.Contains(errors, d => d.Message.Contains("参数类型不匹配") && d.Message.Contains("'n'"));
            Assert.Contains(errors, d => d.Message.Contains("类型 'Point' 不存在成员 'nope'"));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }
}
