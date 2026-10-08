using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Classes;

/// <summary>
/// 省略 func 关键字与 -&gt; 返回类型注解组合的解析测试
/// </summary>
/// <remarks>
/// 文档 §5.6.1 把「使用 func 关键字」「省略 func 关键字」「使用 -&gt; 指定返回类型」
/// 列为可自由组合的几种写法，但此前省略 func 时箭头后的返回类型不会被解析，
/// 于是 "public greet() -&gt; string { ... }" 会报 SYNTAX_ERROR: 无法识别的主表达式类型 'Arrow'。
/// 这里覆盖四种写法的全部组合。
/// </remarks>
[Collection("Sequential")]
public class FuncLessMethodReturnTypeTests
{
    private static void AssertParses(string code)
    {
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Theory]
    // 方式2：省略 func 关键字，不写返回类型
    [InlineData("class A { greet() { return 1 } }")]
    // 方式3：省略 func 关键字 + -> 返回类型
    [InlineData("class A { greet() -> int { return 1 } }")]
    // 方式1/3：func 关键字 + -> 返回类型
    [InlineData("class A { func greet() -> int { return 1 } }")]
    // 带访问修饰符 + 省略 func + -> 返回类型
    [InlineData("class A { public greet() -> int { return 1 } }")]
    [InlineData("class A { private getAge() -> int { return 1 } }")]
    [InlineData("class A { public func greet() -> int { return 1 } }")]
    // 类外的普通函数声明
    [InlineData("add(a, b) -> int { return a + b }")]
    [InlineData("func add(a, b) -> int { return a + b }")]
    public void Declaration_OptionalFuncKeywordAndReturnType_Parses(string code)
    {
        AssertParses(code);
    }

    [Theory]
    // 复杂返回类型同样应该被当作类型注解消费掉
    [InlineData("class A { public items() -> list<int> { return {1} } }")]
    [InlineData("class A { public name() -> string { return \"x\" } }")]
    [InlineData("class A { public init(a:int) -> void { } }")]
    public void Declaration_WithComplexReturnType_Parses(string code)
    {
        AssertParses(code);
    }

    [Fact]
    public void Declaration_ArrowWithoutFuncKeyword_ReturnTypeIsRecorded()
    {
        // 仅解析成功还不够：必须确认箭头后拿到的是「返回类型」而不是被顺手跳过
        const string code = "add(a, b) -> int { return a + b }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var func = FindFunction(program, "add");

        Assert.Equal("int", func.FuncValue.Id.AssumptionType);
    }

    [Fact]
    public void Declaration_ArrowWithFuncKeyword_ReturnTypeIsRecorded()
    {
        const string code = "func add(a, b) -> int { return a + b }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);
        var program = parser.ParseProgram();

        var func = FindFunction(program, "add");

        Assert.Equal("int", func.FuncValue.Id.AssumptionType);
    }

    private static Old8Lang.AST.Statement.FuncInit FindFunction(
        Old8Lang.AST.Statement.BlockStatement program, string name)
    {
        // 函数声明会被 BlockStatement 归入 ImportStatements
        return program.ImportStatements
            .Concat(program.OtherStatements)
            .OfType<Old8Lang.AST.Statement.FuncInit>()
            .Single(func => func.FuncValue.Id.IdName == name);
    }
}
