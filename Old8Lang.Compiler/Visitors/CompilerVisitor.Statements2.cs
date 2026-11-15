using Old8Lang.AST.Statement;
using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler.Helpers;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Compiler.Verification;
using Old8Lang.Concurrency;
using Old8Lang.ExternProviders;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Symbols;
using Old8Lang.ProjectManagement;
using Old8Lang.Runtime;
using Old8Lang.TypeSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

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

        var ty = node.Expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);

        // 对于字典类型，使用特殊处理
        if (ty == typeof(Dictionary<object, object>))
        {
            node.GenerateDictionaryIl(ilGenerator, local);
            return null;
        }

        // 检查是否是异步枚举类型
        if (ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            node.GenerateAsyncEnumerableIl(ilGenerator, local, ty);
            return null;
        }

        // 非字典类型，使用普通的IEnumerator处理
        var enumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        var current = ilGenerator.DeclareLocal(typeof(object));

        // 获取枚举器
        var getEnumeratorMethod = typeof(IEnumerable).GetMethod("GetEnumerator")!;
        node.Expression.Accept(this);
        ilGenerator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, enumerator);

        // 定义循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;

        // 循环开始
        ilGenerator.MarkLabel(loopStart);

        // 调用MoveNext
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素
        var currentProperty = typeof(IEnumerator).GetProperty("Current")!;
        var getCurrentMethod = currentProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);
        ilGenerator.Emit(OpCodes.Stloc, current);

        // 处理标识符赋值
        if (node.Ids.Count == 1)
        {
            // 单个标识符，直接赋值
            // 在异步状态机中，需要定义变量并存储
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, node.Ids[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, node.Ids[0].IdName, node.Position);
            }
            else
            {
                local.AddLocalVar(node.Ids[0].IdName, current);
            }
        }
        else
        {
            // 多个标识符，只赋值给第一个
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, node.Ids[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, node.Ids[0].IdName, node.Position);
            }
            else
            {
                local.AddLocalVar(node.Ids[0].IdName, current);
            }
        }

        // 生成循环体
        node.Body.Accept(this);

        // 继续标签
        ilGenerator.MarkLabel(continueLabel);

        // 跳回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    
    
        return null;
    }

    /// <summary>
    /// 访问 AsyncFuncInit 节点（编译器）
    /// </summary>
    public object? VisitAsyncFuncInit(AsyncFuncInit node)
    {

        // 获取方法的名称
        var methodName = node.GetAsyncFuncValue().Id!.IdName;

        // 使用参数的类型注解来确定参数类型
        var parameterTypes = node.GetAsyncFuncValue().Ids!.Select(item => item.Accept(new TypeInferenceVisitor(local))).ToArray();

        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        // 但需要保留DelegateVar、ClassVar等全局信息，以便函数内部可以调用其他函数
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

        foreach (var (key, value) in local.DelegateVar)
        {
            funcLocal.DelegateVar[key] = value;
        }
        foreach (var (key, value) in local.ClassVar)
        {
            funcLocal.ClassVar[key] = value;
        }
        foreach (var (key, value) in local.GlobalStaticClasses)
        {
            funcLocal.GlobalStaticClasses[key] = value;
        }
        foreach (var (key, value) in local.FuncParameters)
        {
            funcLocal.FuncParameters[key] = value;
        }
        foreach (var (key, value) in local.GenericFunctions)
        {
            funcLocal.GenericFunctions[key] = value;
        }

        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < node.GetAsyncFuncValue().Ids!.Count; i++)
        {
            var id = node.GetAsyncFuncValue().Ids[i];
            var paramType = parameterTypes[i];
            // 存储参数类型，用于后续推断返回类型
            funcLocal.LocalVarTypes[id.IdName] = paramType!;
        }

        // 异步函数返回Task<object>
        var returnType = typeof(Task<object>);

        // 定义新的动态方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            parameterTypes!,
            true
        );

        // 获取动态方法的IL生成器
        var methodIl = dynamicMethod.GetILGenerator();

        // 清空funcLocal，重新添加参数（这次使用真正的LocalBuilder）
        funcLocal.LocalVar.Clear();

        // 处理参数
        for (var i = 0; i < node.GetAsyncFuncValue().Ids!.Count; i++)
        {
            var id = node.GetAsyncFuncValue().Ids[i];
            // 使用实际的参数类型声明局部变量
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType!);
            funcLocal.AddLocalVar(id.IdName, localVar);
            // 加载参数并存储到局部变量
            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        // 生成异步方法体
        node.GetAsyncFuncValue().GenerateMethodBody(methodIl, funcLocal);

        // 将方法添加到本地变量管理器
        // 使用函数名+参数类型作为键，支持更准确的函数重载
        var paramTypeNames = string.Join("_", parameterTypes!.Select(t => t!.Name));
        var delegateKey = $"{methodName}${paramTypeNames}";
        local.DelegateVar.TryAdd(delegateKey, dynamicMethod);

        funcLocal.DelegateVar.TryAdd(delegateKey, dynamicMethod);
        if (Old8Lang.Compiler.Compiler.EnableAsyncStateMachineAwait)
        {
            CompiledDelegateRegistry.Register(delegateKey, dynamicMethod);
        }

        // 同时存储函数的参数列表信息
        if (node.GetAsyncFuncValue().Ids is not null)
        {
            local.FuncParameters.TryAdd(delegateKey, node.GetAsyncFuncValue().Ids);
            funcLocal.FuncParameters.TryAdd(delegateKey, node.GetAsyncFuncValue().Ids);
        }

        return null;
    
    }

    /// <summary>
    /// 访问 SwitchStatement 节点（编译器）
    /// </summary>
    public object? VisitSwitchStatement(SwitchStatement node)
    {

        var labelEnd = ilGenerator.DefineLabel();
        var defaultLabel = node.DefaultBlockStatement is not null ? ilGenerator.DefineLabel() : labelEnd;

        // 保存switch表达式的值到局部变量
        node.SwitchExpression.Accept(this);
        var switchValueType = node.SwitchExpression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        var switchValueLocal = ilGenerator.DeclareLocal(switchValueType);
        ilGenerator.Emit(OpCodes.Stloc, switchValueLocal);

        // 为每个case创建标签
        var caseLabels = node.SwitchCaseList.Select(_ => ilGenerator.DefineLabel()).ToList();

        // 尝试使用 Switch 指令优化（针对整数）
        bool optimized = false;
        if (switchValueType == typeof(int))
        {
            optimized = node.TryGenerateIntSwitch(ilGenerator, switchValueLocal, defaultLabel, caseLabels);
        }

        if (!optimized)
        {
            // 生成case匹配逻辑（if-else链）
            for (int i = 0; i < node.SwitchCaseList.Count; i++)
            {
                var oldCase = node.SwitchCaseList[i];
                var caseLabel = caseLabels[i];

                // 检查是否是范围匹配
                if (oldCase.Expression is RangeLiteralExpression or RangeLangValue)
                {
                    // 范围匹配：检查 switchValue 是否在范围数组中
                    // 加载case值（数组）
                    oldCase.Expression.Accept(this);

                    // 加载switch值
                    ilGenerator.Emit(OpCodes.Ldloc, switchValueLocal);

                    // 装箱 switch 值（因为 Array.IndexOf 需要 object 参数）
                    if (switchValueType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, switchValueType);
                    }

                    // 调用 Array.IndexOf 方法
                    var indexOfMethod = typeof(Array).GetMethod("IndexOf", [typeof(Array), typeof(object)])!;
                    ilGenerator.Emit(OpCodes.Call, indexOfMethod);

                    // 检查返回值是否 >= 0（表示找到了）
                    // IndexOf >= 0 等价于 IndexOf > -1
                    ilGenerator.Emit(OpCodes.Ldc_I4_M1);
                    ilGenerator.Emit(OpCodes.Cgt);

                    // 如果找到，跳转到对应的case标签
                }
                else
                {
                    // 普通匹配
                    if (switchValueType == typeof(int) || switchValueType == typeof(bool) ||
                        switchValueType == typeof(char) || switchValueType == typeof(double))
                    {
                        // 值类型：重新加载 switch 值后直接 Ceq
                        ilGenerator.Emit(OpCodes.Ldloc, switchValueLocal);
                        oldCase.Expression.Accept(this);
                        ilGenerator.Emit(OpCodes.Ceq);
                    }
                    else if (switchValueType == typeof(string))
                    {
                        // 字符串比较需要调用 string.Equals(string, string)
                        ilGenerator.Emit(OpCodes.Ldloc, switchValueLocal);
                        oldCase.Expression.Accept(this);
                        var equalsMethod = typeof(string).GetMethod("Equals", [typeof(string), typeof(string)])!;
                        ilGenerator.Emit(OpCodes.Call, equalsMethod);
                    }
                    else
                    {
                        // 其他类型（含静态类型 object，例如 for-in 循环变量）：
                        // 逐个操作数装箱后调用 object.Equals(object, object)。
                        // 之前按「实例方法 object.Equals(object)」调用，只压了实参、且
                        // 实参未装箱，生成无效 IL（for-in 里的 switch 整段无法验证）。
                        ilGenerator.Emit(OpCodes.Ldloc, switchValueLocal);
                        if (switchValueType.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, switchValueType);
                        }

                        oldCase.Expression.Accept(this);
                        var caseType = oldCase.Expression.Accept(new TypeInferenceVisitor(local));
                        if (caseType is { IsValueType: true })
                        {
                            ilGenerator.Emit(OpCodes.Box, caseType);
                        }

                        var equalsMethod = typeof(object).GetMethod("Equals", [typeof(object), typeof(object)])!;
                        ilGenerator.Emit(OpCodes.Call, equalsMethod);
                    }

                    // 如果相等，跳转到对应的case标签
                }

                ilGenerator.Emit(OpCodes.Brtrue, caseLabel);
            }

            // 所有case都不匹配，跳转到default或结束
            ilGenerator.Emit(OpCodes.Br, defaultLabel);
        }

        // 生成各个case块
        for (var i = 0; i < node.SwitchCaseList.Count; i++)
        {
            var oldCase = node.SwitchCaseList[i];
            var caseLabel = caseLabels[i];

            // 标记case标签
            ilGenerator.MarkLabel(caseLabel);

            // 生成case块的IL代码
            oldCase.BlockStatement.Accept(this);

            // 检查case块是否以return语句结尾
            var lastStatement = oldCase.BlockStatement.Count > 0
                ? oldCase.BlockStatement[^1]
                : null;

            // 如果不是return语句，跳转到结束标签
            if (lastStatement is not ReturnStatement)
            {
                ilGenerator.Emit(OpCodes.Br, labelEnd);
            }
        }

        // 生成default块
        if (node.DefaultBlockStatement is not null)
        {
            ilGenerator.MarkLabel(defaultLabel);
            node.DefaultBlockStatement.Accept(this);
        }

        // 结束标签
        ilGenerator.MarkLabel(labelEnd);

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

        // 异步状态机：try 区域的首指令处发射本区域的恢复派发链
        if (local.AsyncStateMachineGenerator is not null)
        {
            local.AsyncStateMachineGenerator.EnterProtectedRegion(ilGenerator, node);
        }

        // 生成try块的IL代码（记录受保护区域：内部 break/continue 必须用 leave）
        var wasInProtectedRegion = local.IsInProtectedRegion;
        local.IsInProtectedRegion = true;
        node.TryBlock.Accept(this);
        local.IsInProtectedRegion = wasInProtectedRegion;
        
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
                         ilGenerator.Emit(OpCodes.Call, typeof(ExceptionWrapper).GetMethod(nameof(ExceptionWrapper.Bind))!);

                         // 在异步状态机中，需要定义变量并存储
                         if (local.AsyncStateMachineGenerator != null)
                         {
                             local.DefineVariable(ilGenerator, exceptionVar.IdName, typeof(object));
                             local.StoreVariable(ilGenerator, exceptionVar.IdName, node.Position);
                         }
                         else
                         {
                             var wrapperLocal = ilGenerator.DeclareLocal(typeof(object));
                             ilGenerator.Emit(OpCodes.Stloc, wrapperLocal);
                             local.AddLocalVar(exceptionVar.IdName, wrapperLocal);
                         }
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
                    var filterType = filter.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
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
                     // 解包成 catch 变量应持有的值（语言层 throw 的原始值，或异常包装）
                     ilGenerator.Emit(OpCodes.Call, typeof(ExceptionWrapper).GetMethod(nameof(ExceptionWrapper.Bind))!);

                     // 在异步状态机中，需要定义变量并存储
                     if (local.AsyncStateMachineGenerator != null)
                     {
                         local.DefineVariable(ilGenerator, exceptionVar.IdName, typeof(object));
                         local.StoreVariable(ilGenerator, exceptionVar.IdName, node.Position);
                     }
                     else
                     {
                         var wrapperLocal = ilGenerator.DeclareLocal(typeof(object));
                         ilGenerator.Emit(OpCodes.Stloc, wrapperLocal);
                         local.AddLocalVar(exceptionVar.IdName, wrapperLocal);
                     }
                }
                
                // 执行块（catch 块同样是受保护区域）
                var catchWasInProtectedRegion = local.IsInProtectedRegion;
                local.IsInProtectedRegion = true;
                catchBlock.Accept(this);
                local.IsInProtectedRegion = catchWasInProtectedRegion;
                
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

            // 异步状态机在 await 处挂起时用 leave 离开受保护区域，CLR 会顺带执行 finally。
            // 挂起不是函数退出，用户 finally 只能执行一次；与 Roslyn 一致，
            // 在 finally 体前按 state < 0 守卫，挂起返回时跳过用户代码。
            Label? suspendedSkipLabel = null;
            if (local.AsyncStateMachineGenerator is not null)
            {
                suspendedSkipLabel = ilGenerator.DefineLabel();
                local.AsyncStateMachineGenerator.EmitSuspendedFinallyGuard(
                    ilGenerator, suspendedSkipLabel.Value);
            }

            local.IsInFinallyBlock = true;
            node.FinallyBlock.Accept(this);
            local.IsInFinallyBlock = false;

            if (suspendedSkipLabel is not null)
            {
                ilGenerator.MarkLabel(suspendedSkipLabel.Value);
            }
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
        // 同步生成器在 IL 后端没有实现：`yield` 不会发射任何指令，
        // 而调用方仍按「函数返回生成器对象」使用，整个方法会被 JIT 判为无效 IL。
        // 这里给出明确错误，避免把无效 IL 留到运行期。
        if (local.AsyncStateMachineGenerator is null)
        {
            throw new CompilerException(
                "IL 模式暂不支持同步生成器（yield）：该语句没有 IL 实现，请改用解释器或虚拟机模式执行",
                node.Position);
        }

        // 异步生成器的 yield 目前由异步状态机路径处理（语义仍不完整）。
        return null;
    }

    /// <summary>
    /// 访问 CaseStatement 节点（编译器）
    /// </summary>
    public object? VisitCaseStatement(CaseStatement node)
    {
        // CaseStatement 由 SwitchStatement 的 IL 生成逻辑直接处理；独立访问时按原实现生成。
        var labelCase = ilGenerator.DefineLabel();
        node.Expression.Accept(this);
        ilGenerator.Emit(OpCodes.Br, labelCase);
        ilGenerator.MarkLabel(labelCase);
        node.BlockStatement.Accept(this);
        return null;
    }

    /// <summary>
    /// 访问 ClassInit 节点（编译器）
    /// </summary>
    public object? VisitClassInit(ClassInit node)
    {

        // 0. 如果是泛型类，注册到泛型类缓存中，不立即生成类型
        if (node.GetTypeTemplate().IsGeneric)
        {
            local.GenericClasses.TryAdd(node.GetTypeTemplate().ClassName, node.GetTypeTemplate());
            return null;
        }

        // Mixin：解释器/VM 在构建方法表时会合并 mixin 的字段与方法，
        // IL 直接按类模板生成类型，需要先把 mixin 成员并入类模板（类自身成员优先）。
        MergeMixinMembers(node, local);

        // 1. 检查类或接口是否已经存在
        if (local.ClassVar.ContainsKey(node.GetTypeTemplate().ClassName))
        {
            // 类或接口已经存在，跳过生成
            return null;
        }

        // 2. 检查是否已经有动态程序集和模块
        // 如果没有，创建一个全局的动态程序集和模块
        ModuleBuilder moduleBuilder;
        
        if (local.DynamicAssembly is null || local.DynamicModule is null)
        {
            var assemblyName = new AssemblyName("Old8LangDynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("Old8LangDynamicModule");
            
            // 保存到local中，以便其他类和接口使用
            local.DynamicAssembly = assemblyBuilder;
            local.DynamicModule = moduleBuilder;
        }
        else
        {
            // 使用现有的动态程序集和模块
            moduleBuilder = local.DynamicModule;
        }

        // 3. 定义基类或接口的基础类型
        Type? baseType = null;
        TypeAttributes typeAttributes;
        
        if (node.GetTypeTemplate().IsInterface)
        {
            // 接口的处理
            typeAttributes = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract;
        }
        else
        {
            // 类的处理
            typeAttributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;

            // 定义基类
            if (node.GetTypeTemplate().ParentClassName is not null &&
                local.ClassVar.TryGetValue(node.GetTypeTemplate().ParentClassName, out var parentType))
            {
                baseType = parentType;
            }
            else
            {
                // 如果没有指定父类，使用 LangObject 作为基类
                // 这样所有 Old8Lang 自定义类都有统一的基类，方便运算符重载
                baseType = typeof(LangObject);
            }
        }

        // 4. 定义类型（类或接口）
        var typeBuilder = moduleBuilder.DefineType(
            node.GetTypeTemplate().ClassName,
            typeAttributes,
            baseType);

        // 5. 创建一个新的LocalManager实例，用于生成当前类或接口的IL代码
        // 这样可以避免不同类之间的方法混淆
        var classLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder,
            Globals = local.Globals
        };

        // 类体中的方法、字段初始化器同样需要可见此前已创建的类（例如
        // Derived 的静态方法引用 Base.count）。同时登记正在创建的类型，
        // 使类自身的静态成员可在声明体内解析。
        foreach (var (name, knownType) in local.ClassVar)
        {
            classLocal.ClassVar[name] = knownType;
        }

        classLocal.ClassVar[node.GetTypeTemplate().ClassName] = typeBuilder;

        // 6. 定义类或接口的成员
        if (node.GetTypeTemplate().IsInterface)
        {
            // 接口只能有方法，不能有字段
            node.DefineInterfaceMembers(typeBuilder, classLocal);
        }
        else
        {
            // 类可以有字段和方法
            // 6.1 定义字段
            var fieldBuilders = node.DefineFields(typeBuilder, classLocal);

            // 6.2 定义类的构造函数 (先于方法定义，以便方法中可以创建类的实例)
            node.DefineConstructor(typeBuilder, baseType!, fieldBuilders, classLocal);

            // 6.3 定义方法
            node.DefineMethods(typeBuilder, classLocal);

            // 6.4 静态字段初值：实例字段的初值由构造函数写入，静态字段必须在
            // .cctor 里初始化，否则 IL 模式下读到的是 CLR 默认值。
            node.DefineStaticFieldInitializer(typeBuilder, classLocal);
        }

        // 8. 创建类型
        var createdType = typeBuilder.CreateType();

        // 9. 将类型添加到原始LocalManager中，以便其他类可以访问
        local.ClassVar[node.GetTypeTemplate().ClassName] = createdType;
    
    
        return null;
    }

    /// <summary>
    /// 访问 FuncInit 节点（编译器）
    /// </summary>
    public object? VisitFuncInit(FuncInit node)
    {

        // 验证 params 参数的合法性
        node.ValidateParamsParameter();

        // 应用装饰器（如果有）
        // 在 IL 模式下，我们需要在编译时应用装饰器
        // 策略：使用解释器执行装饰器函数，获取包装后的函数，然后编译包装后的函数
        var funcToCompile = node.GetFuncValue();
        if (node.GetFuncValue().Decorators is not null && node.GetFuncValue().Decorators.Count > 0)
        {
            // 需要一个 VariateManager 来执行装饰器
            // 从 LocalManager 的 Interpreter 获取 VariateManager
            if (local.Interpreter?.Manager is not null)
            {
                funcToCompile = FuncInitRuntimeExtensions.ApplyDecoratorsForCompiler(node.GetFuncValue(), local);
            }
            else
            {
                throw new CompilerException("IL 模式下应用装饰器需要解释器上下文", node.Position);
            }
        }

        // 尝试使用类型推断（如果启用）
        if (TypeInferenceConfig.Instance.EnableTypeInference)
        {
            var inferenceEngine = new TypeInferenceEngine(local);
            if (inferenceEngine.NeedsTypeInference(node))
            {
                // 执行类型推断
                if (inferenceEngine.InferFunctionTypes(node))
                {
                    if (TypeInferenceConfig.Instance.DebugOutput)
                    {
                        Console.WriteLine($"✓ 函数 {funcToCompile.Id?.IdName} 类型推断成功");
                    }
                }
            }
        }

        // 验证函数类型注解完整性（IL 模式要求）
        node.ValidateTypeAnnotations(local);

        // 获取方法的名称
        var methodName = funcToCompile.Id!.IdName;

        // 对于泛型函数，只注册定义，不生成IL
        // 泛型函数的代码生成会在实例化时由 GenericMethodSpecializer 处理
        if (funcToCompile.IsGeneric)
        {
            local.GenericFunctions[methodName] = funcToCompile;
            // 同时也需要注册到 DelegateVar，以便能够被识别为函数（虽然不能直接调用）
            // 但为了避免被当作普通函数调用（没有泛型参数），我们只在 GenericFunctions 中注册
            // FunctionCallExpression 会检查 GenericFunctions
            return null;
        }

        if (funcToCompile.Method is not null)
        {
            local.DelegateVar.Add(methodName, funcToCompile.Method);
            return null;
        }

        // 使用参数的类型注解来确定参数类型
        // 对于 params 参数，类型应该是数组类型（已经是 array<T>）
        var parameterTypes = funcToCompile.Ids!.Select(item => item.Accept(new TypeInferenceVisitor(local))).ToArray();

        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        // 这样可以避免函数内部的局部变量与外部的局部变量冲突
        // 但需要保留DelegateVar、ClassVar等全局信息，以便函数内部可以调用其他函数
        var funcLocal = new LocalManager()
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            Globals = local.Globals
        };

        // 复制全局信息：委托、类、全局静态类、泛型函数
        foreach (var (key, value) in local.DelegateVar)
        {
            funcLocal.DelegateVar[key] = value;
        }

        foreach (var (key, value) in local.ClassVar)
        {
            funcLocal.ClassVar[key] = value;
        }

        foreach (var (key, value) in local.GlobalStaticClasses)
        {
            funcLocal.GlobalStaticClasses[key] = value;
        }

        foreach (var (key, value) in local.FuncParameters)
        {
            funcLocal.FuncParameters[key] = value;
        }

        foreach (var (key, value) in local.GenericFunctions)
        {
            funcLocal.GenericFunctions[key] = value;
        }

        foreach (var (key, value) in local.GenericClasses)
        {
            funcLocal.GenericClasses[key] = value;
        }

        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < funcToCompile.Ids!.Count; i++)
        {
            var id = funcToCompile.Ids[i];
            var paramType = parameterTypes[i];
            // 创建一个临时的LocalBuilder来表示参数
            // 注意：这里我们不能使用真正的LocalBuilder，因为还没有创建ILGenerator
            // 所以我们使用一个占位符，稍后会替换
            funcLocal.LocalVarTypes[id.IdName] = paramType!;
        }

        // 优先使用显式声明的返回类型
        // 如果类型注解存在但OutputType返回null/object，则仍尝试推断（用于兼容性）
        var returnType = funcToCompile.Id?.Accept(new TypeInferenceVisitor(local));
        if (returnType is null || returnType == typeof(object))
        {
            // 如果OutputType无法解析，尝试从函数体推断
            returnType = FuncLangValue.GetItemType(funcToCompile.BlockStatement, funcLocal);
        }

        // int? / bool? 等可空值类型返回没有 CLR 载体，改用 object 承载 null，
        // 否则 return null 会在返回路径对空引用 unbox.any 而抛 NRE。
        returnType = IlReturnTypeHelper.Resolve(funcToCompile.Id, returnType);

        // 装饰器应用后的函数体是解释器里构造的 wrapper 闭包（自由变量不在当前
        // IL 作用域），直接编译会解析不到被装饰函数。这里改为生成转发委托：
        // 运行期按名字调用解释器管理器里已装饰好的函数。
        if (node.GetFuncValue().Decorators is { Count: > 0 })
        {
            var forwarder = new DynamicMethod(
                methodName,
                returnType,
                parameterTypes!,
                true);

            var forwarderIl = forwarder.GetILGenerator();
            forwarderIl.Emit(OpCodes.Ldstr, methodName);
            forwarderIl.Emit(OpCodes.Ldc_I4, parameterTypes!.Length);
            forwarderIl.Emit(OpCodes.Newarr, typeof(object));
            for (var index = 0; index < parameterTypes.Length; index++)
            {
                forwarderIl.Emit(OpCodes.Dup);
                forwarderIl.Emit(OpCodes.Ldc_I4, index);
                forwarderIl.Emit(OpCodes.Ldarg, index);
                if (parameterTypes[index].IsValueType)
                {
                    forwarderIl.Emit(OpCodes.Box, parameterTypes[index]);
                }

                forwarderIl.Emit(OpCodes.Stelem_Ref);
            }

            forwarderIl.Emit(OpCodes.Call,
                typeof(IlRuntimeFunctions).GetMethod(nameof(IlRuntimeFunctions.InvokeByName))!);

            if (returnType == typeof(void))
            {
                forwarderIl.Emit(OpCodes.Pop);
            }
            else if (returnType.IsValueType)
            {
                forwarderIl.Emit(OpCodes.Unbox_Any, returnType);
            }
            else if (returnType != typeof(object))
            {
                forwarderIl.Emit(OpCodes.Castclass, returnType);
            }

            forwarderIl.Emit(OpCodes.Ret);

            var forwarderKey = methodName;
            if (funcToCompile.Ids is not null)
            {
                forwarderKey = $"{methodName}${string.Join("_", parameterTypes.Select(t => t.Name))}";
            }

            local.DelegateVar.TryAdd(forwarderKey, forwarder);
            if (funcToCompile.Ids is not null)
            {
                local.FuncParameters.TryAdd(forwarderKey, funcToCompile.Ids);
            }

            return null;
        }

        // 定义新的方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            parameterTypes!,
            true
        );

        // 创建方法的 IL 发射器
        var methodIl = dynamicMethod.GetILGenerator();

        // 【修复递归调用】在编译函数体之前，先将函数注册到 DelegateVar
        // 这样递归调用时就能找到自己的方法引用
        var delegateKey = methodName;
        if (funcToCompile.Ids is not null)
        {
            var paramTypeNames = string.Join("_", parameterTypes!.Select(t => t!.Name));
            delegateKey = $"{methodName}${paramTypeNames}";
        }

        // 先注册到 local.DelegateVar（外部作用域）
        local.DelegateVar.TryAdd(delegateKey, dynamicMethod);

        // 同时注册到 funcLocal.DelegateVar（函数内部作用域），支持递归调用
        funcLocal.DelegateVar.TryAdd(delegateKey, dynamicMethod);

        // 对于泛型函数，也需要注册基础版本
        if (funcToCompile.IsGeneric)
        {
            local.DelegateVar.TryAdd(methodName, dynamicMethod);
            funcLocal.DelegateVar.TryAdd(methodName, dynamicMethod);
            local.GenericFunctions[methodName] = funcToCompile;
        }

        // 清空funcLocal
        funcLocal.LocalVar.Clear();

        // 处理参数：注册到 ArgumentIndices，不生成 IL 副本
        for (var i = 0; i < funcToCompile.Ids!.Count; i++)
        {
            var id = funcToCompile.Ids[i];
            // 记录参数索引
            funcLocal.ArgumentIndices[id.IdName] = i;
        }

        // 为支持defer，使用try-finally包装函数体
        // 声明返回值局部变量（如果需要）
        if (returnType != typeof(void))
        {
            funcLocal.ReturnValueLocal = methodIl.DeclareLocal(returnType);
        }

        // 创建函数结束标签
        var endLabel = methodIl.DefineLabel();
        funcLocal.ReturnLabel = endLabel;

        // 开始 try-finally 块
        methodIl.BeginExceptionBlock();

        // 生成方法体的 IL 代码
        funcLocal.CatchAssignedVariables.UnionWith(CatchAssignmentScanner.Collect(funcToCompile.BlockStatement));
        funcToCompile.BlockStatement.Accept(new CompilerVisitor(methodIl, funcLocal));

        // 检查函数体的最后一个语句是否是 ReturnStatement
        var lastStatement = funcToCompile.BlockStatement.Count > 0
            ? funcToCompile.BlockStatement[^1]
            : null;

        // 如果最后一个语句不是 ReturnStatement，提供默认返回值
        if (lastStatement is not ReturnStatement)
        {
            if (returnType != typeof(void))
            {
                // 为有返回值的函数提供默认值并存储到ReturnValueLocal
                if (returnType.IsValueType)
                {
                    if (returnType == typeof(int))
                    {
                        methodIl.Emit(OpCodes.Ldc_I4_0);
                    }
                    else if (returnType == typeof(double))
                    {
                        methodIl.Emit(OpCodes.Ldc_R8, 0.0);
                    }
                    else if (returnType == typeof(bool))
                    {
                        methodIl.Emit(OpCodes.Ldc_I4_0);
                    }
                    else
                    {
                        var defaultLocal = methodIl.DeclareLocal(returnType);
                        methodIl.Emit(OpCodes.Initobj, returnType);
                        methodIl.Emit(OpCodes.Ldloc, defaultLocal);
                    }
                }
                else
                {
                    methodIl.Emit(OpCodes.Ldnull);
                }

                methodIl.Emit(OpCodes.Stloc, funcLocal.ReturnValueLocal!);
            }
        }

        // Finally 块：执行 defer 语句
        methodIl.BeginFinallyBlock();
        funcLocal.IsInFinallyBlock = true;
        funcLocal.GenerateDeferIL(methodIl);
        funcLocal.IsInFinallyBlock = false;
        methodIl.EndExceptionBlock();

        // 标记函数结束位置
        methodIl.MarkLabel(endLabel);

        // 加载返回值并返回
        if (returnType != typeof(void))
        {
            methodIl.Emit(OpCodes.Ldloc, funcLocal.ReturnValueLocal!);
        }

        methodIl.Emit(OpCodes.Ret);

        // 注意：函数已经在编译函数体之前注册到 DelegateVar（第160-181行）
        // 这里只需要存储函数的参数列表信息，用于支持默认参数
        if (funcToCompile.Ids is not null)
        {
            local.FuncParameters.TryAdd(delegateKey, funcToCompile.Ids);

            // 对于泛型函数，也存储基础版本的参数信息
            if (funcToCompile.IsGeneric)
            {
                local.FuncParameters.TryAdd(methodName, funcToCompile.Ids);
            }
        }
    
    
        return null;
    }


    /// <summary>
    /// 把 `class C with M` / `implements I` 的 mixin/接口默认成员并入类模板，
    /// 供 IL 生成字段/方法。类自身成员优先；mixin 的父 mixin 递归合并。
    /// </summary>
    private static void MergeMixinMembers(ClassInit node, LocalManager local)
    {
        var template = node.GetTypeTemplate();
        if (template.IsMixin || local.Interpreter?.Manager is not { } manager)
        {
            return;
        }

        var mergedNames = new HashSet<string>(StringComparer.Ordinal);

        void MergeTemplate(TypeTemplate source, bool includeFields)
        {
            if (!mergedNames.Add(source.ClassName))
            {
                return;
            }

            if (source.ParentClassName is not null &&
                manager.GetAny(new LangId(source.ParentClassName)) is TypeTemplate parentTemplate)
            {
                MergeTemplate(parentTemplate, includeFields);
            }

            foreach (var (memberId, memberValue) in source.Variates)
            {
                if (!includeFields && memberValue is not FuncLangValue && memberValue is not MethodOverloadList)
                {
                    continue;
                }

                if (template.Variates.Keys.Any(existing => existing.IdName == memberId.IdName))
                {
                    continue;
                }

                template.Variates[memberId] = memberValue;
            }

            foreach (var (memberId, memberValue) in source.StaticVariates)
            {
                if (template.StaticVariates.Keys.Any(existing => existing.IdName == memberId.IdName))
                {
                    continue;
                }

                template.StaticVariates[memberId] = memberValue;
            }
        }

        foreach (var mixinName in template.MixinNames)
        {
            if (manager.GetAny(new LangId(mixinName)) is TypeTemplate mixin)
            {
                MergeTemplate(mixin, includeFields: true);
            }
        }

        foreach (var interfaceName in template.ImplementsNames)
        {
            if (manager.GetAny(new LangId(interfaceName)) is TypeTemplate interfaceTemplate)
            {
                MergeTemplate(interfaceTemplate, includeFields: false);
            }
        }
    }

    /// <summary>
    /// 访问 ImportStatement 节点（编译器）
    /// </summary>
    public object? VisitImportStatement(ImportStatement node)
    {

        string moduleName = node.ImportString;
        bool isDirectory = false;

        // 优先级 1: 标准库（Old8LangLib 和 Old8Lang.NetLib）
        // 在 IL 模式下，标准库由 C# 运行时提供，但我们需要验证其是否可用
        // 编译器始终由宿主创建的解释器驱动；没有解释器时使用共享空目录，绝不读取进程默认目录。
        var standardLibraries = local.Interpreter?.Manager.StandardLibraries ?? StandardLibraryCatalog.Empty;
        if (standardLibraries.IsStandardLibrary(moduleName))
        {
            // 验证标准库是否可用（程序集和类是否存在）
            if (!StandardLibraryLoader.ValidateStandardLibrary(moduleName, out var errorMessage, standardLibraries))
            {
                throw new ImportError(node.Position, moduleName,
                    $"编译时验证失败: {errorMessage}\n\n" +
                    "请确保标准库程序集已正确部署到运行时目录。");
            }

            if (!node.FromClause && StandardLibraryLoader.TryLoadStandardLibrary(
                    moduleName, new VariateManager { StandardLibraries = standardLibraries }, out var standardModule))
                local.GlobalStaticClasses[node.ModuleAlias ?? moduleName] = standardModule;

            return null;
        }

        // 使用 ModuleResolver 进行统一的模块解析
        var resolver = new ModuleResolver(standardLibraries);
        var resolution = resolver.ResolveModule(moduleName, local.FilePath);

        // 如果解析失败，抛出详细错误
        if (!resolution.IsSuccess)
        {
            throw new ImportError(node.Position, moduleName, resolution);
        }

        // 处理不同类型的模块
        string? resolvedPath = resolution.ResolvedPath;

        // 如果是网络模块，IL 模式暂不支持
        if (resolution.ModuleType == ModuleType.NetworkModule)
        {
            throw new ImportError(node.Position, moduleName,
                "IL 模式暂不支持从网络URL导入模块。请使用解释模式 (-f) 或将模块下载到本地。");
        }

        // 对于本地文件、第三方包和子模块，继续处理
        if (string.IsNullOrEmpty(resolvedPath))
        {
            // 这种情况不应该发生，因为 IsSuccess 为 false 时已经抛出异常
            throw new ImportError(node.Position, moduleName, "模块解析失败，但未提供详细信息");
        }

        // 获取绝对路径作为缓存键
        var moduleAbsolutePath = Path.GetFullPath(resolvedPath);

        // 检查模块缓存
        if (local.Interpreter?.ModuleCache.TryGetValue(moduleAbsolutePath, out var cachedBlock) == true)
        {
            // 使用缓存的模块
            var cachedOriginalPath = local.FilePath;
            local.FilePath = moduleAbsolutePath;
            cachedBlock.GenerateImportIl(ilGenerator, local, includeBody: true);
            local.FilePath = cachedOriginalPath;
            return null;
        }

        // 执行导入并缓存
        var code = isDirectory ? Apis.FromDirectory(moduleAbsolutePath) : Apis.FromFile(moduleAbsolutePath);
        var importOriginalPath = local.FilePath;
        local.FilePath = moduleAbsolutePath;
        var block = local.Interpreter?.Build(code: code);

        // 缓存模块
        if (block is not null && local.Interpreter is not null)
        {
            local.Interpreter.ModuleCache[moduleAbsolutePath] = block;
        }

        block?.GenerateImportIl(ilGenerator, local, includeBody: true);
        local.FilePath = importOriginalPath;
    
    
        return null;
    }

    /// <summary>
    /// 访问 NativeStatement 节点（编译器）
    /// </summary>
    public object? VisitNativeStatement(NativeStatement node)
    {

        // 使用 DllPathResolver 查找 DLL 路径
        string path;
        try
        {
            path = DllPathResolver.ResolveDllPath(
                node.DllName,
                null, // 不再使用 importPath
                local.FilePath,
                local.Interpreter?.Manager.StandardLibraries ?? StandardLibraryCatalog.Empty);
        }
        catch (FileNotFoundException ex)
        {
            throw new ImportError(new SourcePosition(0, 0), $"IL 模式：导入原生库失败：\n{ex.Message}");
        }

        // 加载程序集并获取类型
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFile(path);
        }
        catch (Exception ex)
        {
            throw new ImportError(new SourcePosition(0, 0), $"IL 模式：加载 DLL 文件失败：{path}\n错误：{ex.Message}");
        }

        var type = assembly.GetType($"{node.DllName}.{node.ClassName}");
        if (!string.IsNullOrEmpty(node.Name))
        {
            type = assembly.GetType($"{node.Name}.{node.ClassName}");
            if (type is null)
            {
                type = Type.GetType($"{node.Name}.{node.ClassName}");
                if (type is null)
                    throw new TypeError(node, $"找不到类型 {node.Name}.{node.ClassName}");
            }

            local.ClassVar.Add(node.ClassName, type);
            return null;
        }

        if (!string.IsNullOrEmpty(node.MethodName))
        {
            var methodInfo = type?.GetMethod(node.MethodName);
            if (methodInfo is null) throw new InvalidOperationError(node, $"找不到方法 {node.MethodName} 在 {node.ClassName} 类中");
            if (string.IsNullOrEmpty(node.NativeName))
                node.NativeName = node.MethodName;
            local.DelegateVar.Add(node.NativeName, methodInfo);
            return null;
        }

        // 处理批量导入所有方法：native "DllName" ClassName *
        if (node.ImportAll)
        {
            var methods = type?.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if (methods is null || methods.Length == 0)
            {
                throw new InvalidOperationError(node, $"类 {node.ClassName} 中没有找到公共静态方法");
            }

            foreach (var method in methods)
            {
                // 过滤掉 Object 基类的方法
                if (method.DeclaringType == typeof(object))
                    continue;

                local.DelegateVar.Add(method.Name, method);
            }

            return null;
        }

        // 处理选择性导入多个方法：native "DllName" ClassName { Method1, Method2 }
        if (node.MethodList is { Count: > 0 })
        {
            foreach (var methodName in node.MethodList)
            {
                var methodInfo = type?.GetMethod(methodName);
                if (methodInfo is null)
                {
                    throw new InvalidOperationError(node, $"找不到方法 {methodName} 在 {node.ClassName} 类中");
                }

                local.DelegateVar.Add(methodName, methodInfo);
            }
        }
    
    
        return null;
    }
}
