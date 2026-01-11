using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.LangParser;

namespace Old8Lang.Bytecode;

/// <summary>
/// BytecodeVisitor - Expression节点的实现
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitLangId(LangId node)
    {
        string varName = node.IdName;

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.LoadLocal, localIndex);
        }
        else
        {
            // 全局变量
            Emit(OpCode.LoadGlobal, varName);
        }

        return null;
    }

    public Instruction? VisitOperation(Operation node)
    {
        // 生成左操作数代码
        if (node.Left != null)
            node.Left.Accept(this);

        // 生成右操作数代码
        if (node.Right != null)
            node.Right.Accept(this);

        // 生成运算符指令
        switch (node.Opera)
        {
            case LangTokenType.Plus:
                Emit(OpCode.Add);
                break;
            case LangTokenType.Minus:
                Emit(OpCode.Sub);
                break;
            case LangTokenType.Star:
                Emit(OpCode.Mul);
                break;
            case LangTokenType.Slash:
                Emit(OpCode.Div);
                break;
            case LangTokenType.Percent:
                Emit(OpCode.Mod);
                break;
            case LangTokenType.Caret:  // ^ 幂运算
                Emit(OpCode.Pow);
                break;
            case LangTokenType.Equals:  // ==
                Emit(OpCode.Equal);
                break;
            case LangTokenType.NotEquals:  // !=
                Emit(OpCode.NotEqual);
                break;
            case LangTokenType.GreaterThan:  // >
                Emit(OpCode.Greater);
                break;
            case LangTokenType.LessThan:  // <
                Emit(OpCode.Less);
                break;
            case LangTokenType.GreaterThanEquals:  // >=
                Emit(OpCode.GreaterEqual);
                break;
            case LangTokenType.LessThanEquals:  // <=
                Emit(OpCode.LessEqual);
                break;
            case LangTokenType.And:  // &&
                Emit(OpCode.And);
                break;
            case LangTokenType.Or:  // ||
                Emit(OpCode.Or);
                break;
            case LangTokenType.Exclamation:  // !
                Emit(OpCode.Not);
                break;
            default:
                Emit(OpCode.Nop); // 未支持的运算符
                break;
        }

        return null;
    }

    public Instruction? VisitFunctionCallExpression(FunctionCallExpression node)
    {
        // 函数表达式必须是简单的标识符
        if (node.FunctionExpression is not LangId funcId)
        {
            throw new Exception("字节码模式暂不支持复杂的函数调用表达式");
        }

        string funcName = funcId.IdName;

        // 生成参数代码
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }

        // 检查是否是原生函数
        if (_compiler.IsNativeFunction(funcName))
        {
            Emit(OpCode.CallNative, new object[] { node.Arguments.Count, funcName });
        }
        else
        {
            Emit(OpCode.Call, new object[] { node.Arguments.Count, funcName });
        }

        return null;
    }

    public Instruction? VisitClassMemberId(ClassMemberId node)
    {
        // TODO: 实现成员访问
        Emit(OpCode.Nop);
        return null;
    }

    public Instruction? VisitAwaitExpression(AwaitExpression node)
    {
        // TODO: 实现await表达式
        node.Expression.Accept(this);
        Emit(OpCode.Await);
        return null;
    }

    public Instruction? VisitAsyncStreamExpression(AsyncStreamExpression node) => null;
    public Instruction? VisitSuperExpression(SuperExpression node) => null;
    public Instruction? VisitTernaryExpression(TernaryExpression node)
    {
        // TODO: 实现三元运算符
        Emit(OpCode.Nop);
        return null;
    }
}
