using Old8Lang.AST;
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
        // ClassMemberId 是带访问修饰符的成员ID
        // 在字节码层面，它和普通的 LangId 类似
        // 加载成员的值
        string varName = node.IdName;

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.LoadLocal, localIndex);
        }
        else
        {
            // 全局变量或类成员
            Emit(OpCode.LoadGlobal, varName);
        }

        return null;
    }

    public Instruction? VisitAwaitExpression(AwaitExpression node)
    {
        // Await 表达式
        // TODO: 完整的 await 支持需要：
        // 1. 异步状态机
        // 2. Task/Promise 机制
        // 3. 暂停和恢复执行
        //
        // 简化实现：暂时只生成表达式的代码，不实际等待

        // 获取 expression 属性
        var expression = node.GetType().GetProperty("Expression")?.GetValue(node) as LangExpression;
        expression?.Accept(this);

        // TODO: 添加 Await 指令支持
        // Emit(OpCode.Await);

        return null;
    }

    public Instruction? VisitAsyncStreamExpression(AsyncStreamExpression node)
    {
        // 异步流表达式
        // TODO: 完整的异步流支持需要异步迭代器机制
        // 简化实现：暂时不支持
        return null;
    }

    public Instruction? VisitSuperExpression(SuperExpression node)
    {
        // Super 表达式（调用父类方法）
        // TODO: 完整的继承支持需要类层次结构
        // 简化实现：暂时不支持
        return null;
    }
    public Instruction? VisitTernaryExpression(TernaryExpression node)
    {
        // 三元运算符: condition ? trueExpr : falseExpr
        // 生成条件表达式代码
        node.Condition.Accept(this);

        // 如果条件为false，跳转到false分支
        int jumpIfFalseIndex = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // True分支
        node.TrueExpression.Accept(this);
        int jumpToEndIndex = GetCurrentPosition();
        Emit(OpCode.Jump, -1); // 跳转到结束

        // False分支
        PatchJump(jumpIfFalseIndex, GetCurrentPosition());
        node.FalseExpression.Accept(this);

        // 结束
        PatchJump(jumpToEndIndex, GetCurrentPosition());

        return null;
    }
}
