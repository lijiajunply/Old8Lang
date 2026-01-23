using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 控制流语句
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitIfStatement(IfStatement node)
    {
        var elseLabel = new List<int>();
        var endLabel = -1;

        // 处理第一个if分支（主构造函数参数）
        var ifChild = GetPrimaryConstructorParameter<IfChild>(node, "ifChildBlock");
        if (ifChild != null)
        {
            // 手动处理IfChild（不使用Accept，因为IfChild不支持Visitor）
            var expression = GetPrimaryConstructorParameter<LangExpression>(ifChild, "expression");
            var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(ifChild, "blockStatement");

            if (expression != null && blockStatement != null)
            {
                // 生成条件表达式代码
                expression.Accept(this);

                // 如果条件为false,跳转到下一个分支
                int jumpIfFalseIndex = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1);

                // 生成if块代码
                blockStatement.Accept(this);

                // 跳转到结束
                elseLabel.Add(GetCurrentPosition());
                Emit(OpCode.Jump, -1);

                // 修补跳转目标（跳到下一个分支）
                PatchJump(jumpIfFalseIndex, GetCurrentPosition());
            }
        }

        // 处理elif分支（主构造函数参数）
        var elifBlocks = GetPrimaryConstructorParameter<List<IfChild?>>(node, "elifBlock") ?? [];
        foreach (var elif in elifBlocks.OfType<IfChild>())
        {
            // 手动处理IfChild
            var expression = GetPrimaryConstructorParameter<LangExpression>(elif, "expression");
            var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(elif, "blockStatement");

            if (expression != null && blockStatement != null)
            {
                // 生成条件表达式代码
                expression.Accept(this);

                // 如果条件为false,跳转到下一个分支
                int jumpIfFalseIndex = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1);

                // 生成elif块代码
                blockStatement.Accept(this);

                // 跳转到结束
                elseLabel.Add(GetCurrentPosition());
                Emit(OpCode.Jump, -1);

                // 修补跳转目标（跳到下一个分支）
                PatchJump(jumpIfFalseIndex, GetCurrentPosition());
            }
        }

        // 处理else分支（主构造函数参数）
        var elseBlock = GetPrimaryConstructorParameter<BlockStatement>(node, "elseBlockStatement");
        if (elseBlock != null)
        {
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
        // 获取expression和blockStatement字段（主构造函数参数）
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(node, "blockStatement");

        if (expression == null || blockStatement == null)
        {
            throw new Exception("IfChild节点缺少必要的字段");
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
        // 获取expression和blockStatement字段（主构造函数参数）
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var blockStatement = GetPrimaryConstructorParameter<OldStatement>(node, "blockStatement");

        if (expression == null || blockStatement == null)
        {
            throw new Exception("WhileStatement节点缺少必要的字段");
        }

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

        int loopStart = GetCurrentPosition();
        loopLabels.ContinueTarget = loopStart;

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
        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfFalseIndex, loopEnd);

        // 修补所有break跳转
        foreach (var breakJump in loopLabels.BreakJumps)
        {
            PatchJump(breakJump, loopEnd);
        }

        // 修补所有continue跳转
        foreach (var continueJump in loopLabels.ContinueJumps)
        {
            PatchJump(continueJump, loopLabels.ContinueTarget);
        }

        _loopLabels.Pop();

        return null;
    }


    public Instruction? VisitForStatement(ForStatement node)
    {
        // 获取ForStatement的字段（主构造函数参数）
        var setStatement = GetPrimaryConstructorParameter<SetStatement>(node, "setStatement");
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var statement = GetPrimaryConstructorParameter<OldStatement>(node, "statement");
        var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(node, "blockStatement");

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

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

            // continue跳转到这里(增量语句之前)
            int continueTarget = GetCurrentPosition();
            loopLabels.ContinueTarget = continueTarget;

            // 增量
            if (statement != null)
                statement.Accept(this);

            // 跳回循环开始
            Emit(OpCode.Jump, loopStart);

            // 修补跳出循环
            int loopEnd = GetCurrentPosition();
            PatchJump(jumpIfFalseIndex, loopEnd);

            // 修补所有break跳转
            foreach (var breakJump in loopLabels.BreakJumps)
            {
                PatchJump(breakJump, loopEnd);
            }

            // 修补所有continue跳转
            foreach (var continueJump in loopLabels.ContinueJumps)
            {
                PatchJump(continueJump, continueTarget);
            }
        }
        else
        {
            // 无条件循环
            blockStatement?.Accept(this);

            // continue跳转到这里(增量语句之前)
            int continueTarget = GetCurrentPosition();
            loopLabels.ContinueTarget = continueTarget;

            if (statement != null)
                statement.Accept(this);

            Emit(OpCode.Jump, loopStart);

            // 修补所有break跳转(无条件循环的break跳到循环后)
            int loopEnd = GetCurrentPosition();
            foreach (var breakJump in loopLabels.BreakJumps)
            {
                PatchJump(breakJump, loopEnd);
            }

            // 修补所有continue跳转
            foreach (var continueJump in loopLabels.ContinueJumps)
            {
                PatchJump(continueJump, continueTarget);
            }
        }

        _loopLabels.Pop();

        return null;
    }


    public Instruction? VisitReturnStatement(ReturnStatement node)
    {
        // 获取returnExpression字段（主构造函数参数）
        var returnExpression = GetPrimaryConstructorParameter<LangExpression>(node, "returnExpression");

        // 在返回前执行所有 defer 块
        Emit(OpCode.ExecuteDefers);

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
        // 检查是否在循环内部
        if (_loopLabels.Count == 0)
        {
            throw new Exception("break语句只能在循环内部使用");
        }

        // 记录需要修补的跳转位置
        var currentLoop = _loopLabels.Peek();
        currentLoop.BreakJumps.Add(GetCurrentPosition());

        // 发出跳转指令(目标位置稍后修补)
        Emit(OpCode.Jump, -1);

        return null;
    }


    public Instruction? VisitContinueStatement(ContinueStatement node)
    {
        // 检查是否在循环内部
        if (_loopLabels.Count == 0)
        {
            throw new Exception("continue语句只能在循环内部使用");
        }

        // 记录需要修补的跳转位置
        var currentLoop = _loopLabels.Peek();
        currentLoop.ContinueJumps.Add(GetCurrentPosition());

        // 发出跳转指令(目标位置稍后修补)
        Emit(OpCode.Jump, -1);

        return null;
    }


}
