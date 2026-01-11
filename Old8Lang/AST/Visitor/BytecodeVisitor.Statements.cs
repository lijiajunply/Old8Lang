using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;

namespace Old8Lang.Bytecode;

/// <summary>
/// BytecodeVisitor - Statement节点的实现
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitBlockStatement(BlockStatement node)
    {
        // 遍历所有语句
        for (int i = 0; i < node.Count; i++)
        {
            var statement = node[i];
            statement.Accept(this);
        }

        return null;
    }

    public Instruction? VisitSetStatement(SetStatement node)
    {
        string varName = node.Id.IdName;

        // 生成右侧表达式的代码
        node.Value.Accept(this);

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }
        else
        {
            // 声明为全局变量
            _compiler.DeclareGlobalVariable(varName);
            Emit(OpCode.StoreGlobal, varName);
        }

        return null;
    }

    public Instruction? VisitIfStatement(IfStatement node)
    {
        var elseLabel = new List<int>();
        var endLabel = -1;

        // 处理第一个if分支
        var ifChild = node.GetType().GetProperty("ifChildBlock")?.GetValue(node) as IfChild;
        if (ifChild != null)
        {
            ifChild.Accept(this);
            elseLabel.Add(GetCurrentPosition());
            Emit(OpCode.Jump, -1); // 跳转到结束,稍后修补
        }

        // 处理elif分支
        var elifBlocks = node.GetType().GetProperty("elifBlock")?.GetValue(node) as List<IfChild?> ?? new List<IfChild?>();
        foreach (var elif in elifBlocks.OfType<IfChild>())
        {
            // 修补上一个分支的跳转目标
            if (elseLabel.Count > 0)
            {
                int lastJump = elseLabel[elseLabel.Count - 1];
                PatchJump(lastJump, GetCurrentPosition());
            }

            elif.Accept(this);
            elseLabel.Add(GetCurrentPosition());
            Emit(OpCode.Jump, -1); // 跳转到结束
        }

        // 处理else分支
        var elseBlock = node.GetType().GetProperty("elseBlockStatement")?.GetValue(node) as BlockStatement;
        if (elseBlock != null)
        {
            // 修补最后一个分支的跳转
            if (elseLabel.Count > 0)
            {
                int lastJump = elseLabel[elseLabel.Count - 1];
                PatchJump(lastJump, GetCurrentPosition());
                elseLabel.RemoveAt(elseLabel.Count - 1);
            }

            elseBlock.Accept(this);
        }

        // 修补所有跳转到结束的指令
        endLabel = GetCurrentPosition();
        foreach (var jumpIndex in elseLabel)
        {
            PatchJump(jumpIndex, endLabel);
        }

        return null;
    }

    public Instruction? VisitIfChild(IfChild node)
    {
        // 获取expression和blockStatement属性
        var expression = node.GetType().GetProperty("expression")?.GetValue(node) as LangExpression;
        var blockStatement = node.GetType().GetProperty("blockStatement")?.GetValue(node) as BlockStatement;

        if (expression == null || blockStatement == null)
        {
            throw new Exception("IfChild节点缺少必要的属性");
        }

        // 生成条件表达式代码
        expression.Accept(this);

        // 如果条件为false,跳转到下一个分支
        int jumpIfFalseIndex = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 生成if块代码
        blockStatement.Accept(this);

        // 修补跳转目标
        PatchJump(jumpIfFalseIndex, GetCurrentPosition());

        return null;
    }

    public Instruction? VisitWhileStatement(WhileStatement node)
    {
        // 获取expression和blockStatement属性
        var expression = node.GetType().GetProperty("expression")?.GetValue(node) as LangExpression;
        var blockStatement = node.GetType().GetProperty("blockStatement")?.GetValue(node) as OldStatement;

        if (expression == null || blockStatement == null)
        {
            throw new Exception("WhileStatement节点缺少必要的属性");
        }

        int loopStart = GetCurrentPosition();

        // 生成条件代码
        expression.Accept(this);

        // 如果条件为false,跳出循环
        int jumpIfFalseIndex = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 生成循环体代码
        blockStatement.Accept(this);

        // 跳回循环开始
        Emit(OpCode.Jump, loopStart);

        // 修补跳出循环的跳转
        PatchJump(jumpIfFalseIndex, GetCurrentPosition());

        return null;
    }

    public Instruction? VisitForStatement(ForStatement node)
    {
        // 获取ForStatement的属性(主构造函数参数)
        var setStatement = node.GetType().GetProperty("setStatement")?.GetValue(node) as SetStatement;
        var expression = node.GetType().GetProperty("expression")?.GetValue(node) as LangExpression;
        var statement = node.GetType().GetProperty("statement")?.GetValue(node) as OldStatement;
        var blockStatement = node.GetType().GetProperty("blockStatement")?.GetValue(node) as BlockStatement;

        // 初始化
        if (setStatement != null)
            setStatement.Accept(this);

        int loopStart = GetCurrentPosition();

        // 条件
        if (expression != null)
        {
            expression.Accept(this);

            int jumpIfFalseIndex = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);

            // 生成循环体代码
            blockStatement?.Accept(this);

            // 增量
            if (statement != null)
                statement.Accept(this);

            // 跳回循环开始
            Emit(OpCode.Jump, loopStart);

            // 修补跳出循环
            PatchJump(jumpIfFalseIndex, GetCurrentPosition());
        }
        else
        {
            // 无条件循环
            blockStatement?.Accept(this);

            if (statement != null)
                statement.Accept(this);

            Emit(OpCode.Jump, loopStart);
        }

        return null;
    }

    public Instruction? VisitReturnStatement(ReturnStatement node)
    {
        // 获取returnExpression属性(主构造函数参数)
        var returnExpression = node.GetType().GetProperty("returnExpression")?.GetValue(node) as LangExpression;

        if (returnExpression != null)
        {
            returnExpression.Accept(this);
            Emit(OpCode.Return);
        }
        else
        {
            Emit(OpCode.ReturnVoid);
        }

        return null;
    }

    public Instruction? VisitBreakStatement(BreakStatement node)
    {
        // TODO: 实现break跳转
        Emit(OpCode.Nop);
        return null;
    }

    public Instruction? VisitContinueStatement(ContinueStatement node)
    {
        // TODO: 实现continue跳转
        Emit(OpCode.Nop);
        return null;
    }

    public Instruction? VisitFuncInit(FuncInit node)
    {
        // 编译函数定义
        var funcValue = node.FuncLangValue;
        var funcName = funcValue.Id?.IdName ?? "<lambda>";
        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? new List<string>();

        _compiler.CompileFunction(funcName, paramNames, funcValue.BlockStatement);
        return null;
    }

    public Instruction? VisitAsyncFuncInit(AsyncFuncInit node)
    {
        // TODO: 实现异步函数编译
        return null;
    }

    public Instruction? VisitClassInit(ClassInit node)
    {
        // TODO: 实现类定义编译
        return null;
    }

    // ===== 其他语句 - 默认实现 =====

    public Instruction? VisitForInStatement(ForInStatement node) => null;
    public Instruction? VisitAsyncForInStatement(AsyncForInStatement node) => null;
    public Instruction? VisitSwitchStatement(SwitchStatement node) => null;
    public Instruction? VisitCaseStatement(CaseStatement node) => null;
    public Instruction? VisitImportStatement(ImportStatement node) => null;
    public Instruction? VisitNativeStatement(NativeStatement node) => null;
    public Instruction? VisitTryStatement(TryStatement node) => null;
    public Instruction? VisitThrowStatement(ThrowStatement node) => null;
    public Instruction? VisitYieldStatement(YieldStatement node) => null;
    public Instruction? VisitUsingStatement(UsingStatement node) => null;
    public Instruction? VisitSelectStatement(SelectStatement node) => null;
}
