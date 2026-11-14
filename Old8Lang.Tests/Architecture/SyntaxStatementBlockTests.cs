using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Old8Lang.Syntax;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 块语句迁移到语法程序集后的分类与边界测试。
/// </summary>
public class SyntaxStatementBlockTests
{
    [Fact]
    public void BlockStatement_IsDefinedInSyntaxAssembly()
    {
        var assembly = typeof(LiteralSyntax).Assembly;
        Assert.Same(assembly, typeof(BlockStatement).Assembly);
        Assert.Same(assembly, typeof(IBlockDeclaration).Assembly);
        Assert.Same(assembly, typeof(IBlockImportDeclaration).Assembly);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name == "Old8Lang.Runtime");
    }

    [Fact]
    public void BlockStatement_ClassifiesDeclarationsAndKeepsReturnLast()
    {
        const string code = "func f() { }\nreturn 1\nx <- 2";
        var block = new Old8Lang.LangParser.LangParser(LangInterpreter.Tokenize(code), code).ParseProgram();

        var declaration = Assert.Single(block.ImportStatements);
        Assert.IsType<FuncInit>(declaration);
        Assert.IsAssignableFrom<IBlockDeclaration>(declaration);

        // 既有语义：遇到 return 后停止收集，后续语句被丢弃。
        var statement = Assert.Single(block.OtherStatements);
        Assert.IsType<ReturnStatement>(statement);
    }

    [Fact]
    public void BlockStatement_KeepsDynamicImportInNormalStatementFlow()
    {
        var dynamicImport = new ImportStatement(
            "module",
            isDynamic: true,
            dynamicModuleExpression: new ScalarLiteralExpression(LiteralSyntax.String("module", default)));

        var block = new BlockStatement([dynamicImport]);

        Assert.Empty(block.ImportStatements);
        Assert.Same(dynamicImport, Assert.Single(block.OtherStatements));
    }

    [Fact]
    public void BlockStatement_ClassifiesEveryDeclarationKind()
    {
        var func = new FuncInit(new FunctionSyntax(new LangId("f"), [], new BlockStatement([]), null, null, null, isAsync: false));
        var asyncFunc = new AsyncFuncInit(new FunctionSyntax(new LangId("g"), [], new BlockStatement([]), null, null, null, isAsync: true));
        var classInit = new ClassInit(new ClassSyntax("C", new BlockStatement([])));
        var native = new NativeStatement("native.dll", "NativeClass", "Method", "method");
        var externStatement = new ExternStatement("extern.dll", []);

        var block = new BlockStatement([func, asyncFunc, classInit, native, externStatement]);

        Assert.Equal([func, asyncFunc, classInit, native, externStatement], block.ImportStatements);
        Assert.Empty(block.OtherStatements);
    }

    [Fact]
    public void BlockStatement_CollectsClassMembersThroughRuntimeHelpers()
    {
        const string code = """
            class Box {
                x <- 1
                public static PI <- 3.14
                func get() { return this.x }
            }
            """;
        var program = new Old8Lang.LangParser.LangParser(LangInterpreter.Tokenize(code), code).ParseProgram();
        var classInit = Assert.IsType<ClassInit>(Assert.Single(program.ImportStatements));

        var template = classInit.GetTypeTemplate();
        Assert.Contains(template.Variates.Keys, id => id.IdName == "x");
        Assert.Contains(template.Variates.Keys, id => id.IdName == "get");
        Assert.Contains(template.StaticVariates.Keys, id => id.IdName == "PI");
    }

    [Fact]
    public void BlockStatement_MovesStaticImportToDeclarationList()
    {
        var import = new ImportStatement("module");

        var block = new BlockStatement([import]);

        Assert.Same(import, Assert.Single(block.ImportStatements));
        Assert.Empty(block.OtherStatements);
        Assert.IsAssignableFrom<IBlockImportDeclaration>(import);
    }
}
