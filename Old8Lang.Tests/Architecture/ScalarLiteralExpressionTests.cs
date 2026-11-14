using Old8Lang.AST.Expression;
using Old8Lang.AST;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Interpreter;
using Old8Lang.Syntax;

namespace Old8Lang.Tests.Architecture;

public class ScalarLiteralExpressionTests
{
    [Fact]
    public void SliceSyntax_ParserProducesIndependentSyntaxNode()
    {
        const string code = "result <- items[1:4:2]";
        var block = new LangParser.LangParser(LangInterpreter.Tokenize(code), code).ParseProgram();
        var assignment = Assert.IsType<Old8Lang.AST.Statement.SetStatement>(block[0]);
        var slice = Assert.IsType<SliceExpression>(assignment.Value);
        Assert.Equal("items", slice.Id.IdName);
        Assert.NotNull(slice.Start);
        Assert.NotNull(slice.End);
        Assert.NotNull(slice.Step);
        Assert.Same(typeof(LiteralSyntax).Assembly, typeof(SliceExpression).Assembly);
    }

    [Theory]
    [InlineData(1, 4, true, true)]
    [InlineData(4, 1, false, true)]
    [InlineData(1, 1, false, false)]
    public void RangeSyntax_MatchesLegacyEndpointsAndReturnsFreshArrays(
        int start, int end, bool includeStart, bool includeEnd)
    {
        var first = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, start, default));
        var last = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, end, default));
        var syntax = new RangeLiteralExpression(first, last, includeStart: includeStart, includeEnd: includeEnd);
        var legacy = new Old8Lang.AST.Expression.Intermediates.RangeLangValue(first, last,
            includeStart: includeStart, includeEnd: includeEnd);
        var visitor = new InterpreterVisitor(new VariateManager());
        var expected = Assert.IsType<ArrayLangValue>(legacy.Accept(visitor));
        var actual = Assert.IsType<ArrayLangValue>(syntax.Accept(visitor));

        Assert.Equal(expected.GetItems().Cast<IntLangValue>().Select(value => value.Value),
            actual.GetItems().Cast<IntLangValue>().Select(value => value.Value));
        Assert.NotSame(actual, syntax.Accept(visitor));
    }

    [Fact]
    public void RangeSyntax_PreservesInvalidEndpointErrorPosition()
    {
        var position = new SourcePosition(3, 7);
        var invalid = new ScalarLiteralExpression(LiteralSyntax.String("not an integer", position));
        var valid = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 2, position));
        var visitor = new InterpreterVisitor(new VariateManager());

        var error = Assert.Throws<Old8Lang.Error.TypeError>(() =>
            new RangeLiteralExpression(invalid, valid, position).Accept(visitor));
        Assert.Equal(position, error.Position);
    }

    [Fact]
    public void TupleSyntax_PreservesNamedFieldsIdentityAndReevaluation()
    {
        var value = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 7, default));
        var syntax = new TupleLiteralExpression([value, value], ["first", "second"]);
        var manager = new VariateManager();
        var visitor = new InterpreterVisitor(manager);
        var result = Assert.IsType<TupleLangValue>(syntax.Accept(visitor));

        Assert.Equal(7, Assert.IsType<IntLangValue>(result.Dot(new LangId("first"), manager)).Value);
        result.Set(0, new IntLangValue(99));
        Assert.Same(result, syntax.Accept(visitor));
        Assert.Equal(7, Assert.IsType<IntLangValue>(result.Get(0)).Value);
        Assert.NotSame(result, new TupleLiteralExpression([value, value]).Accept(visitor));
    }

    [Fact]
    public void DictionarySyntax_ReusesIdentityAndReevaluatesOrderedEntries()
    {
        var key = new ScalarLiteralExpression(LiteralSyntax.String("key", default));
        var value = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 7, default));
        var entries = new[]
        {
            new KeyValuePair<LangExpression, LangExpression>(key, value),
            new KeyValuePair<LangExpression, LangExpression>(
                new ScalarLiteralExpression(LiteralSyntax.String("next", default)), value)
        };
        var syntax = new DictionaryLiteralExpression(entries);
        var visitor = new InterpreterVisitor(new VariateManager());
        var legacy = new DictionaryLangValue(entries.Select(entry => new TupleLangValue(entry.Key, entry.Value)).ToList());
        var expected = Assert.IsType<DictionaryLangValue>(legacy.Accept(visitor));
        var result = Assert.IsType<DictionaryLangValue>(syntax.Accept(visitor));

        result.Set(new StringLangValue("key"), new IntLangValue(99));
        expected.Set(new StringLangValue("key"), new IntLangValue(99));
        Assert.Same(result, syntax.Accept(visitor));
        Assert.Same(expected, legacy.Accept(visitor));
        Assert.Equal(Assert.IsType<IntLangValue>(expected.Get(new StringLangValue("key"))).Value,
            Assert.IsType<IntLangValue>(result.Get(new StringLangValue("key"))).Value);
        Assert.Equal(new[] { "key", "next" }, result.Value.Select(entry => ((StringLangValue)entry.Key).Value));
        Assert.NotSame(result, new DictionaryLiteralExpression(entries).Accept(visitor));
    }

    [Fact]
    public void ListSyntax_MatchesLegacyIdentityAndClearReevaluation()
    {
        var element = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 7, default));
        var syntax = new ListLiteralExpression([element]);
        var legacy = new ListLangValue(new List<LangExpression> { element });
        var visitor = new InterpreterVisitor(new VariateManager());
        var actual = Assert.IsType<ListLangValue>(syntax.Accept(visitor));
        var expected = Assert.IsType<ListLangValue>(legacy.Accept(visitor));

        actual.ClearInternal();
        expected.ClearInternal();
        Assert.Same(actual, syntax.Accept(visitor));
        Assert.Same(expected, legacy.Accept(visitor));
        Assert.Equal(expected.Values.Select(value => value.ToString()), actual.Values.Select(value => value.ToString()));
        Assert.NotSame(actual, new ListLiteralExpression([element]).Accept(visitor));
    }

    [Fact]
    public void AsyncStateMachine_FindsAwaitInsideSyntaxCollections()
    {
        var listAwait = new AwaitExpression(new LangId("first"));
        var arrayAwait = new AwaitExpression(new LangId("second"));
        var keyAwait = new AwaitExpression(new LangId("key"));
        var valueAwait = new AwaitExpression(new LangId("value"));
        var tupleAwait = new AwaitExpression(new LangId("tuple"));
        var rangeAwait = new AwaitExpression(new LangId("range"));
        var sliceAwait = new AwaitExpression(new LangId("slice"));
        var indexAwait = new AwaitExpression(new LangId("index"));
        var nestedIndexAwait = new AwaitExpression(new LangId("nestedIndex"));
        var nestedSliceAwait = new AwaitExpression(new LangId("nestedSlice"));
        var dictionary = new DictionaryLiteralExpression([
            new KeyValuePair<LangExpression, LangExpression>(keyAwait, valueAwait)
        ]);
        var block = new Old8Lang.AST.Statement.BlockStatement([
            new Old8Lang.AST.Statement.SetStatement(new LangId("items"),
                new ListLiteralExpression([listAwait, new ArrayLiteralExpression([arrayAwait]), dictionary,
                    new TupleLiteralExpression([tupleAwait]), new RangeLiteralExpression(rangeAwait, null),
                    new SliceExpression(new LangId("items"), step: sliceAwait),
                    new IndexExpression(new LangId("items"), indexAwait),
                    new NestedSliceExpression(new NestedIndexExpression(new LangId("items"), nestedIndexAwait),
                        nestedSliceAwait)]))
        ]);
        var method = new System.Reflection.Emit.DynamicMethod("test", typeof(void), Type.EmptyTypes);
        var generator = new Old8Lang.AsyncGeneration.AsyncStateMachineGenerator(
            method.GetILGenerator(), new Old8Lang.Compiler.CodeGeneration.LocalManager(), block);

        Assert.Equal(10, generator.GetStateIndex(new AwaitExpression(new LangId("unseen"))));
        Assert.Equal(0, generator.GetStateIndex(listAwait));
        Assert.Equal(1, generator.GetStateIndex(arrayAwait));
        Assert.Equal(2, generator.GetStateIndex(keyAwait));
        Assert.Equal(3, generator.GetStateIndex(valueAwait));
        Assert.Equal(4, generator.GetStateIndex(tupleAwait));
        Assert.Equal(5, generator.GetStateIndex(rangeAwait));
        Assert.Equal(6, generator.GetStateIndex(sliceAwait));
        Assert.Equal(7, generator.GetStateIndex(indexAwait));
        Assert.Equal(8, generator.GetStateIndex(nestedIndexAwait));
        Assert.Equal(9, generator.GetStateIndex(nestedSliceAwait));
    }

    [Fact]
    public void ArraySyntax_ReusesRuntimeIdentityAndReevaluatesElements()
    {
        var literal = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 7, default));
        var syntax = new ArrayLiteralExpression([literal]);
        var other = new ArrayLiteralExpression([literal]);
        var visitor = new InterpreterVisitor(new VariateManager());
        var first = Assert.IsType<ArrayLangValue>(syntax.Accept(visitor));

        first.Set(new IntLangValue(0), new IntLangValue(99));
        Assert.Same(first, syntax.Accept(visitor));
        Assert.Equal(7, Assert.IsType<IntLangValue>(first.Get(new IntLangValue(0))).Value);
        Assert.NotSame(first, other.Accept(visitor));
    }

    [Fact]
    public void ScalarSyntaxAndVisitor_AreUsableFromSyntaxAssembly()
    {
        var assembly = typeof(LiteralSyntax).Assembly;
        Assert.Same(assembly, typeof(ScalarLiteralExpression).Assembly);
        Assert.Same(assembly, typeof(LangExpression).Assembly);
        Assert.Same(assembly, typeof(OldStatement).Assembly);
        Assert.Same(assembly, typeof(IOldLangTree).Assembly);
        Assert.Same(assembly, typeof(IVisitor<>).Assembly);
        Assert.Same(assembly, typeof(NestedIndexExpression).Assembly);
        Assert.Same(assembly, typeof(NestedSliceExpression).Assembly);
        Assert.Same(assembly, typeof(IndexExpression).Assembly);
        Assert.Same(assembly, typeof(SliceExpression).Assembly);
        Assert.Same(assembly, typeof(RangeLiteralExpression).Assembly);
        Assert.Same(assembly, typeof(TupleLiteralExpression).Assembly);
        Assert.Same(assembly, typeof(DictionaryLiteralExpression).Assembly);
        Assert.Same(assembly, typeof(ListLiteralExpression).Assembly);
        Assert.Same(assembly, typeof(ArrayLiteralExpression).Assembly);
        Assert.Same(assembly, typeof(Operation).Assembly);
        Assert.Same(assembly, typeof(FunctionCallExpression).Assembly);
        Assert.Same(assembly, typeof(AwaitExpression).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ThrowStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ReturnStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.YieldStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.BreakStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ContinueStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.BlockStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.IfChild).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.IfStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.DeferStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.WhileStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.UsingStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.SetStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ForStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.SelectCase).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ImportItem).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ClassFieldSetStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Intermediates.MatchCase).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Intermediates.TuplePattern).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Intermediates.RangePattern).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.FuncRunStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.SwitchStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.CaseStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.TryStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.EnumInit).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.SelectStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ForInStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.AsyncForInStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Intermediates.ListComprehension).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Intermediates.MatchExpression).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.GenericInstanceExpression).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Intermediates.StringTemplateValue).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.LambdaExpression).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.FunctionDecorator).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Linq.LinqClause).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Expression.Linq.FromClause).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.FunctionSyntax).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.FuncInit).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.AsyncFuncInit).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ClassSyntax).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ClassInit).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ClassFuncInitStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ExtensionDeclaration).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.NativeStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ExternStatement).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ExternFunctionDeclaration).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.IBlockDeclaration).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.IBlockImportDeclaration).Assembly);
        Assert.Same(assembly, typeof(LangId).Assembly);
        Assert.Same(assembly, typeof(ClassMemberId).Assembly);
        Assert.Same(assembly, typeof(ThisExpression).Assembly);
        Assert.Same(assembly, typeof(SuperExpression).Assembly);
        Assert.Same(assembly, typeof(TernaryExpression).Assembly);
        Assert.Same(assembly, typeof(NamedArgument).Assembly);
        var literal = new ScalarLiteralExpression(LiteralSyntax.Bool(true, default));
        var visitor = new SyntaxOnlyVisitor();
        Assert.Equal("Bool:True", literal.Accept(visitor));
        Assert.Equal("name", new LangId("name").Accept(visitor));
        Assert.Equal("left+right", new Operation(new LangId("left"), Old8Lang.LangParser.LangTokenType.Plus, new LangId("right")).Accept(visitor));
        Assert.Equal("call name", new FunctionCallExpression(new LangId("name"), []).Accept(visitor));
        Assert.Equal("await name", new AwaitExpression(new LangId("name")).Accept(visitor));
        Assert.Equal("throw name", new Old8Lang.AST.Statement.ThrowStatement(new LangId("name")).Accept(visitor));
        Assert.Equal("return name", new Old8Lang.AST.Statement.ReturnStatement(new LangId("name")).Accept(visitor));
        Assert.Equal("yield name", new Old8Lang.AST.Statement.YieldStatement(new LangId("name")).Accept(visitor));
        Assert.Equal("break", new Old8Lang.AST.Statement.BreakStatement().Accept(visitor));
        Assert.Equal("continue", new Old8Lang.AST.Statement.ContinueStatement().Accept(visitor));
        Assert.Equal("", new Old8Lang.AST.Statement.BlockStatement([]).Accept(visitor));
        Assert.Equal("if", new Old8Lang.AST.Statement.IfStatement(
            new Old8Lang.AST.Statement.IfChild(literal, new Old8Lang.AST.Statement.BlockStatement([])), [], null)
            .Accept(visitor));
        Assert.Equal("defer", new Old8Lang.AST.Statement.DeferStatement(new Old8Lang.AST.Statement.BlockStatement([]))
            .Accept(visitor));
        Assert.Equal("while", new Old8Lang.AST.Statement.WhileStatement(literal,
            new Old8Lang.AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("using", new Old8Lang.AST.Statement.UsingStatement(null, literal,
            new Old8Lang.AST.Statement.BlockStatement([])).Accept(visitor));
        var init = new Old8Lang.AST.Statement.SetStatement(new LangId("i"), literal);
        Assert.Equal("assign", init.Accept(visitor));
        Assert.Equal("for", new Old8Lang.AST.Statement.ForStatement(init, literal,
            new Old8Lang.AST.Statement.SetStatement(new LangId("i"), literal),
            new Old8Lang.AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("run", new Old8Lang.AST.Statement.FuncRunStatement(literal).Accept(visitor));
        Assert.Equal("case", new Old8Lang.AST.Statement.CaseStatement(literal,
            new Old8Lang.AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("switch", new Old8Lang.AST.Statement.SwitchStatement(literal,
            [new Old8Lang.AST.Statement.CaseStatement(literal, new Old8Lang.AST.Statement.BlockStatement([]))])
            .Accept(visitor));
        Assert.Equal("try", new Old8Lang.AST.Statement.TryStatement(
            new Old8Lang.AST.Statement.BlockStatement([]), []).Accept(visitor));
        Assert.Equal("enum", new Old8Lang.AST.Statement.EnumInit("E", []).Accept(visitor));
        Assert.Equal("select", new Old8Lang.AST.Statement.SelectStatement([], null).Accept(visitor));
        Assert.Equal("forin", new Old8Lang.AST.Statement.ForInStatement(new LangId("x"), literal,
            new Old8Lang.AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("asyncforin", new Old8Lang.AST.Statement.AsyncForInStatement(new LangId("x"), literal,
            new Old8Lang.AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("comp", new Old8Lang.AST.Expression.Intermediates.ListComprehension(
            literal, new LangId("x"), literal).Accept(visitor));
        Assert.Equal("match", new Old8Lang.AST.Expression.Intermediates.MatchExpression(literal, []).Accept(visitor));
        Assert.Equal("generic", new Old8Lang.AST.Expression.GenericInstanceExpression(
            new LangId("Box"), ["int"]).Accept(visitor));
        Assert.Equal("template", new Old8Lang.AST.Expression.Intermediates.StringTemplateValue([literal])
            .Accept(visitor));
        Assert.Equal("lambda", new Old8Lang.AST.Expression.LambdaExpression(
            null, [], new Old8Lang.AST.Statement.BlockStatement([]), false).Accept(visitor));
        var functionSyntax = new Old8Lang.AST.Statement.FunctionSyntax(
            new LangId("f"), [], new Old8Lang.AST.Statement.BlockStatement([]), null, null, null, false);
        Assert.Equal("func", new Old8Lang.AST.Statement.FuncInit(functionSyntax).Accept(visitor));
        Assert.Equal("asyncfunc", new Old8Lang.AST.Statement.AsyncFuncInit(
            new Old8Lang.AST.Statement.FunctionSyntax(
                new LangId("g"), [], new Old8Lang.AST.Statement.BlockStatement([]), null, null, null, true))
            .Accept(visitor));
        Assert.Equal("class", new Old8Lang.AST.Statement.ClassInit(
            new Old8Lang.AST.Statement.ClassSyntax("C", new Old8Lang.AST.Statement.BlockStatement([])))
            .Accept(visitor));
        Assert.Equal("extension", new Old8Lang.AST.Statement.ExtensionDeclaration("string", null, [])
            .Accept(visitor));
        Assert.Equal("native", new Old8Lang.AST.Statement.NativeStatement("d", "C").Accept(visitor));
        Assert.Equal("extern", new Old8Lang.AST.Statement.ExternStatement("d", []).Accept(visitor));
        Assert.Equal("field", new ClassMemberId("field").Accept(visitor));
        Assert.Equal("Bool:True?this:super", new TernaryExpression(literal, new ThisExpression(), new SuperExpression()).Accept(visitor));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name == "Old8Lang.Runtime");
    }

    private sealed class SyntaxOnlyVisitor : IVisitor<string>
    {
        public string VisitScalarLiteralExpression(ScalarLiteralExpression node) => $"{node.Kind}:{node.LiteralValue}";
        public string VisitNestedIndexExpression(NestedIndexExpression node) => node.ToString();
        public string VisitNestedSliceExpression(NestedSliceExpression node) => node.ToString();
        public string VisitIndexExpression(IndexExpression node) => node.ToString();
        public string VisitSliceExpression(SliceExpression node) => node.ToString();
        public string VisitRangeLiteralExpression(RangeLiteralExpression node) => node.ToString();
        public string VisitTupleLiteralExpression(TupleLiteralExpression node) => node.ToString();
        public string VisitDictionaryLiteralExpression(DictionaryLiteralExpression node) => node.ToString();
        public string VisitListLiteralExpression(ListLiteralExpression node) => node.ToString();
        public string VisitArrayLiteralExpression(ArrayLiteralExpression node) => node.ToString();
        public string VisitAwaitExpression(AwaitExpression node) => $"await {node.Expression.Accept(this)}";
        public string VisitThrowStatement(Old8Lang.AST.Statement.ThrowStatement node) => $"throw {node.Expression.Accept(this)}";
        public string VisitReturnStatement(Old8Lang.AST.Statement.ReturnStatement node) => $"return {node.Expression.Accept(this)}";
        public string VisitYieldStatement(Old8Lang.AST.Statement.YieldStatement node) => $"yield {node.YieldExpression.Accept(this)}";
        public string VisitBreakStatement(Old8Lang.AST.Statement.BreakStatement node) => "break";
        public string VisitContinueStatement(Old8Lang.AST.Statement.ContinueStatement node) => "continue";
        public string VisitBlockStatement(Old8Lang.AST.Statement.BlockStatement node) => node.ToString();
        public string VisitIfStatement(Old8Lang.AST.Statement.IfStatement node) => "if";
        public string VisitDeferStatement(Old8Lang.AST.Statement.DeferStatement node) => "defer";
        public string VisitWhileStatement(Old8Lang.AST.Statement.WhileStatement node) => "while";
        public string VisitUsingStatement(Old8Lang.AST.Statement.UsingStatement node) => "using";
        public string VisitSetStatement(Old8Lang.AST.Statement.SetStatement node) => "assign";
        public string VisitForStatement(Old8Lang.AST.Statement.ForStatement node) => "for";
        public string VisitFuncRunStatement(Old8Lang.AST.Statement.FuncRunStatement node) => "run";
        public string VisitSwitchStatement(Old8Lang.AST.Statement.SwitchStatement node) => "switch";
        public string VisitCaseStatement(Old8Lang.AST.Statement.CaseStatement node) => "case";
        public string VisitTryStatement(Old8Lang.AST.Statement.TryStatement node) => "try";
        public string VisitEnumInit(Old8Lang.AST.Statement.EnumInit node) => "enum";
        public string VisitSelectStatement(Old8Lang.AST.Statement.SelectStatement node) => "select";
        public string VisitForInStatement(Old8Lang.AST.Statement.ForInStatement node) => "forin";
        public string VisitAsyncForInStatement(Old8Lang.AST.Statement.AsyncForInStatement node) => "asyncforin";
        public string VisitListComprehension(Old8Lang.AST.Expression.Intermediates.ListComprehension node) => "comp";
        public string VisitMatchExpression(Old8Lang.AST.Expression.Intermediates.MatchExpression node) => "match";
        public string VisitGenericInstanceExpression(Old8Lang.AST.Expression.GenericInstanceExpression node) => "generic";
        public string VisitStringTemplateValue(Old8Lang.AST.Expression.Intermediates.StringTemplateValue node) => "template";
        public string VisitLambdaExpression(Old8Lang.AST.Expression.LambdaExpression node) => "lambda";
        public string VisitFuncInit(Old8Lang.AST.Statement.FuncInit node) => "func";
        public string VisitAsyncFuncInit(Old8Lang.AST.Statement.AsyncFuncInit node) => "asyncfunc";
        public string VisitClassInit(Old8Lang.AST.Statement.ClassInit node) => "class";
        public string VisitExtensionDeclaration(Old8Lang.AST.Statement.ExtensionDeclaration node) => "extension";
        public string VisitNativeStatement(Old8Lang.AST.Statement.NativeStatement node) => "native";
        public string VisitExternStatement(Old8Lang.AST.Statement.ExternStatement node) => "extern";
        public string VisitFunctionCallExpression(FunctionCallExpression node) => $"call {node.FunctionExpression.Accept(this)}";
        public string VisitOperation(Operation node) => node.ToString();
        public string VisitLangId(LangId node) => node.IdName;
        public string VisitThisExpression(ThisExpression node) => "this";
        public string VisitSuperExpression(SuperExpression node) => "super";
        public string VisitTernaryExpression(TernaryExpression node) => $"{node.Condition.Accept(this)}?{node.TrueExpression.Accept(this)}:{node.FalseExpression.Accept(this)}";
    }

    [Fact]
    public void Interpreter_ReusesValueForSameSyntaxNode_ButNotDifferentNodes()
    {
        var syntax = LiteralSyntax.String(@"a\nb", new SourcePosition(2, 3));
        var first = new ScalarLiteralExpression(syntax);
        var second = new ScalarLiteralExpression(syntax);
        var visitor = new InterpreterVisitor(new VariateManager());

        var value = Assert.IsType<StringLangValue>(first.Accept(visitor));
        Assert.Equal("a\nb", value.Value);
        Assert.Same(value, first.Accept(visitor));
        Assert.NotSame(value, second.Accept(visitor));
        Assert.Equal(first.Position, value.Position);
    }

    [Fact]
    public void Parser_ProducesSyntaxExpressionRatherThanRuntimeValue()
    {
        var tokens = LangInterpreter.Tokenize("answer <- 42");
        var block = new LangParser.LangParser(tokens).ParseProgram();
        var assignment = Assert.IsType<Old8Lang.AST.Statement.SetStatement>(block[0]);
        var literal = Assert.IsType<ScalarLiteralExpression>(assignment.Value);

        Assert.Equal(LiteralKind.Int, literal.Kind);
        Assert.Equal(42, literal.LiteralValue);
        Assert.IsNotAssignableFrom<LangValueType>(literal);
    }
}
