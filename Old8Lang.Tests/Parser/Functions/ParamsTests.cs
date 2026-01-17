using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Functions;

/// <summary>
/// 测试 params 可变参数语法解析
/// </summary>
public class ParamsTests
{
    [Fact]
    public void TestBasicParamsSyntax()
    {
        // 测试基础 params 语法解析
        var code = @"
func sum(params args:array<int>) -> int {
    return 0
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        var ast = parser.ParseProgram();

        Assert.NotNull(ast);
        Assert.Equal(1, ast.Total);

        var funcInit = Assert.IsType<FuncInit>(ast.GetImportStatement(0));
        Assert.NotNull(funcInit.FuncValue.Ids);
        Assert.Single(funcInit.FuncValue.Ids);

        var paramsParam = funcInit.FuncValue.Ids[0];
        Assert.Equal("args", paramsParam.IdName);
        Assert.True(paramsParam.IsParams);
        Assert.Equal("array<int>", paramsParam.AssumptionType);
    }

    [Fact]
    public void TestParamsWithRegularParameters()
    {
        // 测试 params 与普通参数混用
        var code = @"
func format(fmt:string, params args:array<object>) -> string {
    return fmt
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        var ast = parser.ParseProgram();

        Assert.NotNull(ast);
        Assert.Equal(1, ast.Total);

        var funcInit = Assert.IsType<FuncInit>(ast.GetImportStatement(0));
        Assert.NotNull(funcInit.FuncValue.Ids);
        Assert.Equal(2, funcInit.FuncValue.Ids.Count);

        // 第一个参数是普通参数
        var fmtParam = funcInit.FuncValue.Ids[0];
        Assert.Equal("fmt", fmtParam.IdName);
        Assert.False(fmtParam.IsParams);
        Assert.Equal("string", fmtParam.AssumptionType);

        // 第二个参数是 params 参数
        var argsParam = funcInit.FuncValue.Ids[1];
        Assert.Equal("args", argsParam.IdName);
        Assert.True(argsParam.IsParams);
        Assert.Equal("array<object>", argsParam.AssumptionType);
    }

    [Fact]
    public void TestParamsWithDifferentArrayTypes()
    {
        // 测试不同数组类型的 params 参数
        var code = @"
func printStrings(params items:array<string>) -> void {
    return
}

func printDoubles(params values:array<double>) -> void {
    return
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        var ast = parser.ParseProgram();

        Assert.NotNull(ast);
        Assert.Equal(2, ast.Total);

        var funcInit1 = Assert.IsType<FuncInit>(ast.GetImportStatement(0));
        var paramsParam1 = funcInit1.FuncValue.Ids![0];
        Assert.True(paramsParam1.IsParams);
        Assert.Equal("array<string>", paramsParam1.AssumptionType);

        var funcInit2 = Assert.IsType<FuncInit>(ast.GetImportStatement(1));
        var paramsParam2 = funcInit2.FuncValue.Ids![0];
        Assert.True(paramsParam2.IsParams);
        Assert.Equal("array<double>", paramsParam2.AssumptionType);
    }
}
