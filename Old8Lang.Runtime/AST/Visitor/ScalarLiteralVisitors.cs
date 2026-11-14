using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Core;
using Old8Lang.Runtime;
using Old8Lang.Syntax;

namespace Old8Lang.AST.Visitor;

public partial class InterpreterVisitor
{
    // 旧 AST 在解析时创建值，同一节点重复求值返回同一实例；弱引用缓存保留这一语义。
    private static readonly ConditionalWeakTable<ScalarLiteralExpression, LangValueType> LiteralValues = new();

    /// <summary>求值标量字面量。</summary>
    public LangValueType VisitScalarLiteralExpression(ScalarLiteralExpression node) =>
        LiteralValues.GetValue(node, static n => ScalarLiteralRuntimeValueFactory.Create(n));
}

public partial class CompilerVisitor
{
    /// <summary>生成标量字面量的 IL。</summary>
    public object? VisitScalarLiteralExpression(ScalarLiteralExpression node) =>
        ScalarLiteralRuntimeValueFactory.Create(node).Accept(this);
}

public partial class BytecodeVisitor
{
    /// <summary>生成标量字面量的字节码。</summary>
    public Instruction? VisitScalarLiteralExpression(ScalarLiteralExpression node) =>
        ScalarLiteralRuntimeValueFactory.Create(node).Accept(this);
}

public partial class TypeInferenceVisitor
{
    /// <summary>推断标量字面量的类型。</summary>
    public Type? VisitScalarLiteralExpression(ScalarLiteralExpression node) => node.Kind switch
    {
        LiteralKind.Int => typeof(int),
        LiteralKind.Double => typeof(double),
        LiteralKind.String => typeof(string),
        LiteralKind.Bool => typeof(bool),
        LiteralKind.Char => typeof(char),
        LiteralKind.Null => typeof(object),
        LiteralKind.Void => typeof(void),
        _ => throw new ArgumentOutOfRangeException(nameof(node))
    };
}
