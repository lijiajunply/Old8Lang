using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 异常处理
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitTryStatement(TryStatement node)
    {
        // Try-Catch-Finally 异常处理
        // 完整实现：生成异常表和相应的字节码指令

        // 记录 try 块的起始位置
        int tryStart = _instructions.Count;

        // 生成 try 块的字节码
        node.TryBlock.Accept(this);

        // 记录 try 块的结束位置
        int tryEnd = _instructions.Count;

        // 生成跳转到 finally 或结束的指令 (跳过 catch 块)
        int jumpToFinallyIndex = GetCurrentPosition();
        Emit(OpCode.Jump, -1);

        // 处理 catch 块 (实现手动分发逻辑)
        int catchStart = -1;
        int catchEnd = -1;
        string? exceptionVariable = "<exception_dispatch>";
        int exceptionVariableIndex = _compiler.AllocateLocal(exceptionVariable);

        if (node.CatchBlocks.Count > 0)
        {
            catchStart = _instructions.Count;

            // 1. 保存异常对象到临时变量
            Emit(OpCode.StoreLocal, exceptionVariableIndex);

            // 2. 遍历所有 catch 块，生成检查链
            List<int> jumpsToNextBlock = [];
            List<int> jumpsToFinally = [];

            for (int i = 0; i < node.CatchBlocks.Count; i++)
            {
                var (catchExceptionType, catchExceptionVar, filter, catchBlock) = node.CatchBlocks[i];
                
                // 修补上一个块失败后的跳转（跳到当前块的开始）
                foreach (var jump in jumpsToNextBlock)
                {
                    PatchJump(jump, GetCurrentPosition());
                }
                jumpsToNextBlock.Clear();

                // --- 类型检查 ---
                if (!string.IsNullOrEmpty(catchExceptionType) && catchExceptionType != "Exception")
                {
                    // 检查类型
                    Emit(OpCode.LoadLocal, exceptionVariableIndex);
                    Emit(OpCode.IsType, catchExceptionType);
                    
                    // 如果类型不匹配，跳到下一个 catch 块
                    jumpsToNextBlock.Add(GetCurrentPosition());
                    Emit(OpCode.JumpIfFalse, -1);
                }
                
                // --- 过滤器检查 ---
                if (filter != null)
                {
                    // 绑定变量 (供过滤器使用)
                    if (catchExceptionVar != null && !string.IsNullOrEmpty(catchExceptionVar.IdName))
                    {
                        int varIndex = _compiler.AllocateLocal(catchExceptionVar.IdName);
                        Emit(OpCode.LoadLocal, exceptionVariableIndex);
                        Emit(OpCode.StoreLocal, varIndex);
                    }
                    
                    // 执行过滤器
                    filter.Accept(this);
                    
                    // 如果过滤器为 false，跳到下一个 catch 块
                    jumpsToNextBlock.Add(GetCurrentPosition());
                    Emit(OpCode.JumpIfFalse, -1);
                }

                // --- 执行 Catch 块 ---
                // 绑定变量 (供 catch 块使用)
                // 简单起见，重新绑定 (覆盖)。
                
                if (catchExceptionVar != null && !string.IsNullOrEmpty(catchExceptionVar.IdName))
                {
                    // 实际上，我们应该在 catch 块开始时声明变量
                    int varIndex = _compiler.DeclareLocalVariable(catchExceptionVar.IdName);
                    Emit(OpCode.LoadLocal, exceptionVariableIndex);
                    Emit(OpCode.StoreLocal, varIndex);
                }

                catchBlock.Accept(this);

                // 执行完 catch 块后，跳到 finally
                jumpsToFinally.Add(GetCurrentPosition());
                Emit(OpCode.Jump, -1);
            }

            // --- 所有 Catch 块都不匹配 ---
            // 重新抛出异常
            // 如果最后一个 catch 是 catch-all，则不会到达这里
            foreach (var jump in jumpsToNextBlock)
            {
                PatchJump(jump, GetCurrentPosition());
            }
            
            Emit(OpCode.LoadLocal, exceptionVariableIndex);
            Emit(OpCode.Throw);

            catchEnd = _instructions.Count;
            
            // 修补所有跳到 finally 的指令
            // 我们将在 finally 块生成后修补
            
            // 为了让 jumpToFinallyList 在 finally 块后可用，我们需要存储它？
            // 不，我们在 finally 块生成后可以手动添加它们到 elseLabel (如果 finallyBlock 为空)
            // 或者我们可以立即修补到 finallyStart (如果 finallyBlock 存在)
            
            // 但是 finallyStart 还不知道。
            // 我们可以将 jumpToFinallyIndex 也加入到 jumpsToFinally 中
            jumpsToFinally.Add(jumpToFinallyIndex);
            
            // 稍后修补 jumpsToFinally
            
            // 处理 finally 块
            int finallyStart = -1;
            int finallyEnd = -1;

            if (node.FinallyBlock != null)
            {
                finallyStart = _instructions.Count;

                // 生成 finally 块的字节码
                node.FinallyBlock.Accept(this);

                finallyEnd = _instructions.Count;
            }
            
            int endPos = GetCurrentPosition();
            int target = finallyStart != -1 ? finallyStart : endPos;
            
            foreach (var jump in jumpsToFinally)
            {
                PatchJump(jump, target);
            }

            // 创建异常表条目 (单个入口，匹配所有异常)
            var exceptionEntry = new ExceptionTableEntry
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                CatchStart = catchStart,
                CatchEnd = catchEnd,
                FinallyStart = finallyStart,
                FinallyEnd = finallyEnd,
                ExceptionType = null, // 匹配所有异常
                ExceptionVariable = null, // 手动处理变量
                ExceptionVariableIndex = -1
            };

            // 将异常表条目添加到当前函数的异常表
            _compiler.AddExceptionTableEntry(exceptionEntry);
        }
        else
        {
            // 没有 catch 块，只有 finally 块
            // 修补跳过 catch 的指令 (直接跳到 finally)
            // 此时 catchStart = -1
            
            int finallyStart = -1;
            int finallyEnd = -1;

            if (node.FinallyBlock != null)
            {
                finallyStart = _instructions.Count;
                node.FinallyBlock.Accept(this);
                finallyEnd = _instructions.Count;
            }
            
            int endPos = GetCurrentPosition();
            int target = finallyStart != -1 ? finallyStart : endPos;
            PatchJump(jumpToFinallyIndex, target);
            
            if (node.FinallyBlock != null)
            {
                var exceptionEntry = new ExceptionTableEntry
                {
                    TryStart = tryStart,
                    TryEnd = tryEnd,
                    CatchStart = -1,
                    CatchEnd = -1,
                    FinallyStart = finallyStart,
                    FinallyEnd = finallyEnd,
                    ExceptionType = null,
                    ExceptionVariable = null,
                    ExceptionVariableIndex = -1
                };
                _compiler.AddExceptionTableEntry(exceptionEntry);
            }
        }

        return null;
    }


    public Instruction? VisitThrowStatement(ThrowStatement node)
    {
        // 获取 expression 字段（主构造函数参数）
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");

        if (expression != null)
        {
            // 计算异常表达式的值
            expression.Accept(this);

            // 抛出异常
            Emit(OpCode.Throw);
        }

        return null;
    }

}
