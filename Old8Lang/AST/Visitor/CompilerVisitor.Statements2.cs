using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using System.Reflection.Emit;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - 额外的 Statement 节点实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 AsyncForInStatement 节点（编译器）
    /// </summary>
    public object? VisitAsyncForInStatement(AsyncForInStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 AsyncFuncInit 节点（编译器）
    /// </summary>
    public object? VisitAsyncFuncInit(AsyncFuncInit node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 SwitchStatement 节点（编译器）
    /// </summary>
    public object? VisitSwitchStatement(SwitchStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 TryStatement 节点（编译器）
    /// </summary>
    public object? VisitTryStatement(TryStatement node)
    {
        // 检查是否有finally块且try/catch中包含return
        // 注意：在异步状态机模式下，return 语句会编译为 Leave 指令，这是合法的，所以跳过检查
        if (node.FinallyBlock is not null && local.AsyncStateMachineGenerator == null && local.ReturnLabel == null)
        {
            if (ContainsReturnStatement(node.TryBlock))
            {
                throw new CompilerException("当有finally块时，try块中不能包含return语句", node.Position);
            }

            foreach (var (_, _, _, catchBlock) in node.CatchBlocks)
            {
                if (ContainsReturnStatement(catchBlock))
                {
                    throw new CompilerException("当有finally块时，catch块中不能包含return语句", node.Position);
                }
            }
        }

        // 定义结束标签
        var endOfTryCatchLabel = ilGenerator.DefineLabel();

        // 开始异常处理块
        ilGenerator.BeginExceptionBlock();

        // 生成try块的IL代码
        node.TryBlock.Accept(this);
        
        // Try 块结束，跳出（如果不是 return）
        // 但 IL 自动处理 try 块结束到 finally/end 的流转吗？
        // 通常 try 块结束后会跳过 catch 块。System.Reflection.Emit 会处理这个吗？
        // BeginCatchBlock 会发出 leave 吗？不，它只是标记。
        // 如果 try 块执行完，它应该跳过 catch。
        // ILGenerator.BeginCatchBlock 会自动发出 leave 指令从 try 块跳出吗？
        // 文档说：BeginCatchBlock ... Emits the instruction to leave the protected block.
        // 所以不需要我们在 try 块末尾手动 emit leave。

        // 生成catch块
        // 我们只生成一个通用的 catch(Exception)，然后在内部进行类型分发
        if (node.CatchBlocks.Count > 0)
        {
            ilGenerator.BeginCatchBlock(typeof(Exception));
            
            // 声明本地变量存储异常
            var exceptionLocal = ilGenerator.DeclareLocal(typeof(Exception));
            ilGenerator.Emit(OpCodes.Stloc, exceptionLocal);
            
            // 遍历每个catch块进行检查
            foreach (var (exceptionType, exceptionVar, filter, catchBlock) in node.CatchBlocks)
            {
                var nextCatchLabel = ilGenerator.DefineLabel();
                
                // 1. 检查类型匹配
                if (!string.IsNullOrEmpty(exceptionType) && exceptionType != "Exception" && exceptionType != "Old8Exception")
                {
                    ilGenerator.Emit(OpCodes.Ldloc, exceptionLocal);
                    ilGenerator.Emit(OpCodes.Ldstr, exceptionType);
                    ilGenerator.Emit(OpCodes.Call, typeof(ExceptionHelper).GetMethod("IsMatch")!);
                    ilGenerator.Emit(OpCodes.Brfalse, nextCatchLabel);
                }
                
                // 2. 如果有过滤器 (filter)，这里也可以尝试支持 (需要先设置变量)
                if (filter != null)
                {
                    // 设置变量供过滤器使用
                    if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
                    {
                         ilGenerator.Emit(OpCodes.Ldloc, exceptionLocal);
                         ilGenerator.Emit(OpCodes.Newobj, typeof(Old8Lang.Compiler.ExceptionWrapper).GetConstructor([typeof(Exception)])!);
                         var wrapperLocal = ilGenerator.DeclareLocal(typeof(Old8Lang.Compiler.ExceptionWrapper));
                         ilGenerator.Emit(OpCodes.Stloc, wrapperLocal);
                         local.AddLocalVar(exceptionVar.IdName, wrapperLocal);
                    }
                    else
                    {
                        // 即使没有指定变量名，过滤器可能依赖隐含的异常对象（如果有这种语法）
                        // 但 Old8Lang 语法 catch(e) where e... 必须有变量名
                    }
                    
                    // 生成过滤器代码
                    filter.Accept(this);
                    
                    // 过滤器结果应该在栈顶
                    // 检查类型，如果是值类型需要装箱，因为 ToBool 接受 object
                    var filterType = filter.OutputType(local);
                    if (filterType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, filterType);
                    }
                    
                    // 转换为 bool
                    ilGenerator.Emit(OpCodes.Call, typeof(TypeConversion).GetMethod("ToBool")!);
                    
                    // 如果为 false，跳到下一个 catch
                    ilGenerator.Emit(OpCodes.Brfalse, nextCatchLabel);
                    
                    // 如果为 true，继续执行
                    // 注意：这里我们已经设置了变量，如果过滤器通过，进入 block 执行，变量应该保留。
                    // 编译顺序是线性的，所以变量在 local 中保留是正确的。
                    // 但是，如果我们在上面添加了变量，然后在编译完这个 catch 块后，我们需要移除它。
                    // 下面的 catchBlock.Accept(this) 会使用这个变量。
                    // 之后我们会执行 local.RemoveLocalVar，这会清理掉。
                    
                    // 唯一的问题是：如果在编译期添加了变量，但运行时跳转了（Brfalse），
                    // 下一个 catch 块的代码生成不应该受影响，因为我们是在循环中处理每个 catch 块。
                    // 下一个 catch 块编译时，上一个块的变量已经被移除了。
                    // 运行时跳转到 nextCatchLabel，对应的栈应该是空的（除了 exceptionLocal 还在吗？不，exceptionLocal 是局部变量，一直在）。
                    // 栈平衡：ToBool 返回 bool，Brfalse 消耗 bool。栈平衡。
                }
                
                // 3. 进入 catch 块
                // 设置异常变量 (如果有) - 如果有过滤器，上面已经设置过了。如果没有过滤器，这里设置。
                // 注意：如果过滤器设置了变量，我们不需要再次设置。
                // 我们可以检查 local 是否已经有这个变量。
                // 或者我们可以简单地：如果 filter == null，则设置变量。
                
                if (filter == null && exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
                {
                     ilGenerator.Emit(OpCodes.Ldloc, exceptionLocal);
                     // 包装异常
                     ilGenerator.Emit(OpCodes.Newobj, typeof(Old8Lang.Compiler.ExceptionWrapper).GetConstructor([typeof(Exception)])!);
                     var wrapperLocal = ilGenerator.DeclareLocal(typeof(Old8Lang.Compiler.ExceptionWrapper));
                     ilGenerator.Emit(OpCodes.Stloc, wrapperLocal);
                     local.AddLocalVar(exceptionVar.IdName, wrapperLocal);
                }
                
                // 执行块
                catchBlock.Accept(this);
                
                // 清理变量
                if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
                {
                    local.RemoveLocalVar(exceptionVar.IdName);
                }
                
                // 跳出 catch 逻辑 (处理完毕)
                ilGenerator.Emit(OpCodes.Leave, endOfTryCatchLabel);
                
                // 下一个 catch 块的标签
                ilGenerator.MarkLabel(nextCatchLabel);
            }
            
            // 如果所有 catch 块都不匹配，重新抛出
            ilGenerator.Emit(OpCodes.Rethrow);
        }

        // 生成finally块
        if (node.FinallyBlock is not null)
        {
            ilGenerator.BeginFinallyBlock();
            local.IsInFinallyBlock = true;
            node.FinallyBlock.Accept(this);
            local.IsInFinallyBlock = false;
        }

        ilGenerator.EndExceptionBlock();
        
        // 标记结束标签
        ilGenerator.MarkLabel(endOfTryCatchLabel);
        
        return null;
    }
    
    /// <summary>
    /// 检查语句是否包含 Return 语句
    /// </summary>
    private bool ContainsReturnStatement(OldStatement statement)
    {
        if (statement is ReturnStatement) return true;
        for (int i = 0; i < statement.Count; i++)
        {
            var child = statement[i];
            if (child != null && ContainsReturnStatement(child)) return true;
        }
        return false;
    }

    /// <summary>
    /// 访问 YieldStatement 节点（编译器）
    /// </summary>
    public object? VisitYieldStatement(YieldStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 CaseStatement 节点（编译器）
    /// </summary>
    public object? VisitCaseStatement(CaseStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 ClassInit 节点（编译器）
    /// </summary>
    public object? VisitClassInit(ClassInit node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 FuncInit 节点（编译器）
    /// </summary>
    public object? VisitFuncInit(FuncInit node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 ImportStatement 节点（编译器）
    /// </summary>
    public object? VisitImportStatement(ImportStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 NativeStatement 节点（编译器）
    /// </summary>
    public object? VisitNativeStatement(NativeStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }
}
