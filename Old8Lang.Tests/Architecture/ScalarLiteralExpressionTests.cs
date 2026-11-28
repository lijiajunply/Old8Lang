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
        var assignment = Assert.IsType<AST.Statement.SetStatement>(block[0]);
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
        var legacy = new AST.Expression.Intermediates.RangeLangValue(first, last,
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
    public void TupleSyntax_PreservesNamedFieldsAndReturnsFreshInstancePerEvaluation()
    {
        var value = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 7, default));
        var syntax = new TupleLiteralExpression([value, value], ["first", "second"]);
        var manager = new VariateManager();
        var visitor = new InterpreterVisitor(manager);
        var result = Assert.IsType<TupleLangValue>(syntax.Accept(visitor));

        Assert.Equal(7, Assert.IsType<IntLangValue>(result.Dot(new LangId("first"), manager)).Value);
        result.Set(0, new IntLangValue(99));
        var secondEval = Assert.IsType<TupleLangValue>(syntax.Accept(visitor));
        Assert.NotSame(result, secondEval);
        Assert.Equal(99, Assert.IsType<IntLangValue>(result.Get(0)).Value);
        Assert.Equal(7, Assert.IsType<IntLangValue>(secondEval.Get(0)).Value);
    }

    [Fact]
    public void DictionarySyntax_ReturnsFreshInstancePerEvaluationWithOrderedEntries()
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
        var result = Assert.IsType<DictionaryLangValue>(syntax.Accept(visitor));

        result.Set(new StringLangValue("key"), new IntLangValue(99));
        var secondEval = Assert.IsType<DictionaryLangValue>(syntax.Accept(visitor));
        Assert.NotSame(result, secondEval);
        Assert.Equal(99, Assert.IsType<IntLangValue>(result.Get(new StringLangValue("key"))).Value);
        Assert.Equal(7, Assert.IsType<IntLangValue>(secondEval.Get(new StringLangValue("key"))).Value);
        Assert.Equal(new[] { "key", "next" }, secondEval.Value.Select(entry => ((StringLangValue)entry.Key).Value));
    }

    [Fact]
    public void ListSyntax_ReturnsFreshInstancePerEvaluation()
    {
        var element = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 7, default));
        var syntax = new ListLiteralExpression([element]);
        var visitor = new InterpreterVisitor(new VariateManager());
        var first = Assert.IsType<ListLangValue>(syntax.Accept(visitor));

        first.ClearInternal();
        var second = Assert.IsType<ListLangValue>(syntax.Accept(visitor));
        Assert.NotSame(first, second);
        Assert.Empty(first.Values);
        Assert.Single(second.Values);
        Assert.Equal("7", second.Values[0].ToString());
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
        var block = new AST.Statement.BlockStatement([
            new AST.Statement.SetStatement(new LangId("items"),
                new ListLiteralExpression([listAwait, new ArrayLiteralExpression([arrayAwait]), dictionary,
                    new TupleLiteralExpression([tupleAwait]), new RangeLiteralExpression(rangeAwait, null),
                    new SliceExpression(new LangId("items"), step: sliceAwait),
                    new IndexExpression(new LangId("items"), indexAwait),
                    new NestedSliceExpression(new NestedIndexExpression(new LangId("items"), nestedIndexAwait),
                        nestedSliceAwait)]))
        ]);
        var method = new System.Reflection.Emit.DynamicMethod("test", typeof(void), Type.EmptyTypes);
        var generator = new AsyncGeneration.AsyncStateMachineGenerator(
            method.GetILGenerator(), new Old8Lang.Compiler.CodeGeneration.LocalManager(), block);

        Assert.Throws<Old8Lang.Error.CompilerException>(() =>
            generator.GetStateIndex(new AwaitExpression(new LangId("unseen"))));
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
    public void ArraySyntax_ReturnsFreshInstancePerEvaluation()
    {
        var literal = new ScalarLiteralExpression(new LiteralSyntax(LiteralKind.Int, 7, default));
        var syntax = new ArrayLiteralExpression([literal]);
        var visitor = new InterpreterVisitor(new VariateManager());
        var first = Assert.IsType<ArrayLangValue>(syntax.Accept(visitor));

        first.Set(new IntLangValue(0), new IntLangValue(99));
        var second = Assert.IsType<ArrayLangValue>(syntax.Accept(visitor));
        Assert.NotSame(first, second);
        Assert.Equal(99, Assert.IsType<IntLangValue>(first.Get(new IntLangValue(0))).Value);
        Assert.Equal(7, Assert.IsType<IntLangValue>(second.Get(new IntLangValue(0))).Value);
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
        Assert.Same(assembly, typeof(AST.Statement.ThrowStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ReturnStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.YieldStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.BreakStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ContinueStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.BlockStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.IfChild).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.IfStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.DeferStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.WhileStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.UsingStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.SetStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ForStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.SelectCase).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ImportItem).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ClassFieldSetStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Intermediates.MatchCase).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Intermediates.TuplePattern).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Intermediates.RangePattern).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.FuncRunStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.SwitchStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.CaseStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.TryStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.EnumInit).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.SelectStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ForInStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.AsyncForInStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Intermediates.ListComprehension).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Intermediates.MatchExpression).Assembly);
        Assert.Same(assembly, typeof(GenericInstanceExpression).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Intermediates.StringTemplateValue).Assembly);
        Assert.Same(assembly, typeof(LambdaExpression).Assembly);
        Assert.Same(assembly, typeof(FunctionDecorator).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Linq.LinqClause).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Linq.FromClause).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.FunctionSyntax).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.FuncInit).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.AsyncFuncInit).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ClassSyntax).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ClassInit).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ClassFuncInitStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ExtensionDeclaration).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.NativeStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ExternStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ExternFunctionDeclaration).Assembly);
        Assert.Same(assembly, typeof(CallExpression).Assembly);
        Assert.Same(assembly, typeof(AsyncStreamExpression).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.ImportStatement).Assembly);
        Assert.Same(assembly, typeof(AST.Expression.Linq.LinqExpression).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.IBlockDeclaration).Assembly);
        Assert.Same(assembly, typeof(AST.Statement.IBlockImportDeclaration).Assembly);
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
        Assert.Equal("left+right", new Operation(new LangId("left"), LangParser.LangTokenType.Plus, new LangId("right")).Accept(visitor));
        Assert.Equal("call name", new FunctionCallExpression(new LangId("name"), []).Accept(visitor));
        Assert.Equal("await name", new AwaitExpression(new LangId("name")).Accept(visitor));
        Assert.Equal("throw name", new AST.Statement.ThrowStatement(new LangId("name")).Accept(visitor));
        Assert.Equal("return name", new AST.Statement.ReturnStatement(new LangId("name")).Accept(visitor));
        Assert.Equal("yield name", new AST.Statement.YieldStatement(new LangId("name")).Accept(visitor));
        Assert.Equal("break", new AST.Statement.BreakStatement().Accept(visitor));
        Assert.Equal("continue", new AST.Statement.ContinueStatement().Accept(visitor));
        Assert.Equal("", new AST.Statement.BlockStatement([]).Accept(visitor));
        Assert.Equal("if", new AST.Statement.IfStatement(
            new AST.Statement.IfChild(literal, new AST.Statement.BlockStatement([])), [], null)
            .Accept(visitor));
        Assert.Equal("defer", new AST.Statement.DeferStatement(new AST.Statement.BlockStatement([]))
            .Accept(visitor));
        Assert.Equal("while", new AST.Statement.WhileStatement(literal,
            new AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("using", new AST.Statement.UsingStatement(null, literal,
            new AST.Statement.BlockStatement([])).Accept(visitor));
        var init = new AST.Statement.SetStatement(new LangId("i"), literal);
        Assert.Equal("assign", init.Accept(visitor));
        Assert.Equal("for", new AST.Statement.ForStatement(init, literal,
            new AST.Statement.SetStatement(new LangId("i"), literal),
            new AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("run", new AST.Statement.FuncRunStatement(literal).Accept(visitor));
        Assert.Equal("case", new AST.Statement.CaseStatement(literal,
            new AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("switch", new AST.Statement.SwitchStatement(literal,
            [new AST.Statement.CaseStatement(literal, new AST.Statement.BlockStatement([]))])
            .Accept(visitor));
        Assert.Equal("try", new AST.Statement.TryStatement(
            new AST.Statement.BlockStatement([]), []).Accept(visitor));
        Assert.Equal("enum", new AST.Statement.EnumInit("E", []).Accept(visitor));
        Assert.Equal("select", new AST.Statement.SelectStatement([], null).Accept(visitor));
        Assert.Equal("forin", new AST.Statement.ForInStatement(new LangId("x"), literal,
            new AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("asyncforin", new AST.Statement.AsyncForInStatement(new LangId("x"), literal,
            new AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("comp", new AST.Expression.Intermediates.ListComprehension(
            literal, new LangId("x"), literal).Accept(visitor));
        Assert.Equal("match", new AST.Expression.Intermediates.MatchExpression(literal, []).Accept(visitor));
        Assert.Equal("generic", new GenericInstanceExpression(
            new LangId("Box"), ["int"]).Accept(visitor));
        Assert.Equal("template", new AST.Expression.Intermediates.StringTemplateValue([literal])
            .Accept(visitor));
        Assert.Equal("lambda", new LambdaExpression(
            null, [], new AST.Statement.BlockStatement([]), false).Accept(visitor));
        var functionSyntax = new AST.Statement.FunctionSyntax(
            new LangId("f"), [], new AST.Statement.BlockStatement([]), null, null, null, false);
        Assert.Equal("func", new AST.Statement.FuncInit(functionSyntax).Accept(visitor));
        Assert.Equal("asyncfunc", new AST.Statement.AsyncFuncInit(
            new AST.Statement.FunctionSyntax(
                new LangId("g"), [], new AST.Statement.BlockStatement([]), null, null, null, true))
            .Accept(visitor));
        Assert.Equal("class", new AST.Statement.ClassInit(
            new AST.Statement.ClassSyntax("C", new AST.Statement.BlockStatement([])))
            .Accept(visitor));
        Assert.Equal("extension", new AST.Statement.ExtensionDeclaration("string", null, [])
            .Accept(visitor));
        Assert.Equal("native", new AST.Statement.NativeStatement("d", "C").Accept(visitor));
        Assert.Equal("extern", new AST.Statement.ExternStatement("d", []).Accept(visitor));
        Assert.Equal("call", new CallExpression(new LangId("f"), []).Accept(visitor));
        Assert.Equal("stream", new AsyncStreamExpression(
            new AST.Statement.BlockStatement([])).Accept(visitor));
        Assert.Equal("import", new AST.Statement.ImportStatement("module").Accept(visitor));
        Assert.Equal("linq", new AST.Expression.Linq.LinqExpression(
            new AST.Expression.Linq.FromClause("x", literal),
            [],
            new AST.Expression.Linq.SelectClause(literal)).Accept(visitor));
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
        public string VisitThrowStatement(AST.Statement.ThrowStatement node) => $"throw {node.Expression.Accept(this)}";
        public string VisitReturnStatement(AST.Statement.ReturnStatement node) => $"return {node.Expression.Accept(this)}";
        public string VisitYieldStatement(AST.Statement.YieldStatement node) => $"yield {node.YieldExpression.Accept(this)}";
        public string VisitBreakStatement(AST.Statement.BreakStatement node) => "break";
        public string VisitContinueStatement(AST.Statement.ContinueStatement node) => "continue";
        public string VisitBlockStatement(AST.Statement.BlockStatement node) => node.ToString();
        public string VisitIfStatement(AST.Statement.IfStatement node) => "if";
        public string VisitDeferStatement(AST.Statement.DeferStatement node) => "defer";
        public string VisitWhileStatement(AST.Statement.WhileStatement node) => "while";
        public string VisitUsingStatement(AST.Statement.UsingStatement node) => "using";
        public string VisitSetStatement(AST.Statement.SetStatement node) => "assign";
        public string VisitForStatement(AST.Statement.ForStatement node) => "for";
        public string VisitFuncRunStatement(AST.Statement.FuncRunStatement node) => "run";
        public string VisitSwitchStatement(AST.Statement.SwitchStatement node) => "switch";
        public string VisitCaseStatement(AST.Statement.CaseStatement node) => "case";
        public string VisitTryStatement(AST.Statement.TryStatement node) => "try";
        public string VisitEnumInit(AST.Statement.EnumInit node) => "enum";
        public string VisitSelectStatement(AST.Statement.SelectStatement node) => "select";
        public string VisitForInStatement(AST.Statement.ForInStatement node) => "forin";
        public string VisitAsyncForInStatement(AST.Statement.AsyncForInStatement node) => "asyncforin";
        public string VisitListComprehension(AST.Expression.Intermediates.ListComprehension node) => "comp";
        public string VisitMatchExpression(AST.Expression.Intermediates.MatchExpression node) => "match";
        public string VisitGenericInstanceExpression(GenericInstanceExpression node) => "generic";
        public string VisitStringTemplateValue(AST.Expression.Intermediates.StringTemplateValue node) => "template";
        public string VisitLambdaExpression(LambdaExpression node) => "lambda";
        public string VisitFuncInit(AST.Statement.FuncInit node) => "func";
        public string VisitAsyncFuncInit(AST.Statement.AsyncFuncInit node) => "asyncfunc";
        public string VisitClassInit(AST.Statement.ClassInit node) => "class";
        public string VisitExtensionDeclaration(AST.Statement.ExtensionDeclaration node) => "extension";
        public string VisitNativeStatement(AST.Statement.NativeStatement node) => "native";
        public string VisitExternStatement(AST.Statement.ExternStatement node) => "extern";
        public string VisitCallExpression(CallExpression node) => "call";
        public string VisitAsyncStreamExpression(AsyncStreamExpression node) => "stream";
        public string VisitImportStatement(AST.Statement.ImportStatement node) => "import";
        public string VisitLinqExpression(AST.Expression.Linq.LinqExpression node) => "linq";
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
        var assignment = Assert.IsType<AST.Statement.SetStatement>(block[0]);
        var literal = Assert.IsType<ScalarLiteralExpression>(assignment.Value);

        Assert.Equal(LiteralKind.Int, literal.Kind);
        Assert.Equal(42, literal.LiteralValue);
        Assert.IsNotAssignableFrom<LangValueType>(literal);
    }
}
