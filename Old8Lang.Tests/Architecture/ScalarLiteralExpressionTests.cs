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
    public void ScalarSyntaxAndVisitor_AreUsableFromSyntaxAssembly()
    {
        var assembly = typeof(LiteralSyntax).Assembly;
        Assert.Same(assembly, typeof(ScalarLiteralExpression).Assembly);
        Assert.Same(assembly, typeof(LangExpression).Assembly);
        Assert.Same(assembly, typeof(OldStatement).Assembly);
        Assert.Same(assembly, typeof(IOldLangTree).Assembly);
        Assert.Same(assembly, typeof(IVisitor<>).Assembly);
        Assert.Same(assembly, typeof(FunctionCallExpression).Assembly);
        Assert.Same(assembly, typeof(AwaitExpression).Assembly);
        Assert.Same(assembly, typeof(Old8Lang.AST.Statement.ThrowStatement).Assembly);
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
        Assert.Equal("call name", new FunctionCallExpression(new LangId("name"), []).Accept(visitor));
        Assert.Equal("await name", new AwaitExpression(new LangId("name")).Accept(visitor));
        Assert.Equal("throw name", new Old8Lang.AST.Statement.ThrowStatement(new LangId("name")).Accept(visitor));
        Assert.Equal("field", new ClassMemberId("field").Accept(visitor));
        Assert.Equal("Bool:True?this:super", new TernaryExpression(literal, new ThisExpression(), new SuperExpression()).Accept(visitor));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name == "Old8Lang.Runtime");
    }

    private sealed class SyntaxOnlyVisitor : IVisitor<string>
    {
        public string VisitScalarLiteralExpression(ScalarLiteralExpression node) => $"{node.Kind}:{node.LiteralValue}";
        public string VisitAwaitExpression(AwaitExpression node) => $"await {node.Expression.Accept(this)}";
        public string VisitThrowStatement(Old8Lang.AST.Statement.ThrowStatement node) => $"throw {node.Expression.Accept(this)}";
        public string VisitFunctionCallExpression(FunctionCallExpression node) => $"call {node.FunctionExpression.Accept(this)}";
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
