using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.AST.Visitor;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.Interpreter;
using System.Reflection;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Verification;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 异步函数值类型
/// 表示一个异步函数，可以被调用并返回 TaskLangValue
/// </summary>
public class AsyncFuncLangValue : ImportInfo
{
    public readonly LangId? Id;
    public readonly List<LangId>? Ids;
    internal readonly BlockStatement BlockStatement;

    // 闭包环境：捕获的作用域
    public VariateManager? CapturedScope { get; internal init; }

    /// <summary>
    /// 获取捕获的作用域（用于模块导入等场景）
    /// </summary>
    internal VariateManager? GetCapturedScope() => CapturedScope;

    // 默认参数值缓存
    private Dictionary<int, LangValueType>? CachedDefaultValues { get; set; }

    /// <summary>
    /// 文档注释内容
    /// 存储通过 /// 语法编写的异步函数文档注释（结构化）
    /// </summary>
    public DocCommentInfo? DocComment { get; set; }

    /// <summary>
    /// 装饰器列表
    /// 存储应用于此异步函数的装饰器（从上到下的顺序）
    /// </summary>
    public List<FunctionDecorator>? Decorators { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncFuncLangValue(
        LangId? id,
        List<LangId>? ids,
        BlockStatement blockStatement,
        SourcePosition position = default)
        : base(position)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
    }

    /// <summary>
    /// 检查函数是否是异步生成器函数（包含yield语句）
    /// </summary>
    private bool IsAsyncGenerator => ContainsYieldStatement(BlockStatement);

    /// <summary>
    /// 递归检查语句是否包含yield语句
    /// </summary>
    private bool ContainsYieldStatement(OldStatement stmt)
    {
        if (stmt is YieldStatement)
            return true;

        // 检查块语句中的子语句
        for (int i = 0; i < stmt.Count; i++)
        {
            var child = stmt[i];
            if (child is not null && ContainsYieldStatement(child))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Run 方法：返回捕获了闭包的异步函数副本或异步生成器
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        // 检查是否为异步生成器函数
        if (IsAsyncGenerator)
        {
            // 创建异步生成器，捕获当前作用域
            var generatorClosure = new AsyncFuncLangValue(Id, Ids, BlockStatement, Position)
            {
                // 对于异步生成器，使用深拷贝创建独立作用域
                // 这样生成器内部的变量修改不会影响外部作用域
                // 生成器需要保持自己的局部状态（如循环变量）
                CapturedScope = manager.Clone()
            };

            return new AsyncGeneratorLangValue(generatorClosure, Position);
        }

        // 创建新的异步函数副本，捕获当前作用域
        var closureFunc = new AsyncFuncLangValue(Id, Ids, BlockStatement, Position)
        {
            // 直接引用原始 manager，允许异步函数访问和修改外部作用域变量
            // 并发安全性通过在 RunAsync 中为每个任务创建独立的参数作用域来保证
            CapturedScope = manager
        };
        return closureFunc;
    }

    /// <summary>
    /// 调用异步函数，返回 Task
    /// </summary>
    /// <param name="variateManagerFunc">调用时的变量管理器</param>
    /// <param name="ids">参数表达式列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含异步操作的 TaskLangValue</returns>
    public TaskLangValue RunAsync(VariateManager variateManagerFunc, List<LangExpression> ids,
        CancellationToken cancellationToken = default)
    {
        // 创建 .NET Task
        var task = Task.Run(() =>
        {
            // 检查取消请求
            cancellationToken.ThrowIfCancellationRequested();

            // 参数数量检查
            if (Ids is not null && ids.Count > Ids.Count)
            {
                throw new ArgumentError(
                    Position,
                    $"异步函数 '{Id?.IdName ?? "anonymous"}' 期望最多 {Ids.Count} 个参数，但实际提供了 {ids.Count} 个参数"
                );
            }

            // 使用捕获的作用域或调用时的作用域
            var baseManager = CapturedScope ?? variateManagerFunc;

            // 克隆 baseManager 创建独立的执行作用域
            // 这确保每个异步任务有独立的参数作用域，避免并发冲突
            // 如果需要在异步函数中修改外部变量，应使用锁定变量（lock）机制
            var executionManager = baseManager.Clone();

            // 重置返回状态，确保异步函数体能够正常执行
            executionManager.IsReturn = false;

            try
            {
                // 增加递归深度
                executionManager.RecursionDepth++;

                // 入栈
                Old8Exception.PushCallStack(Id?.IdName ?? "anonymous async", Position);

                // 添加新作用域
                executionManager.AddChildren();
                executionManager.IsFunc = true;

                try
                {
                    // 处理参数
                    if (Ids is not null && Ids.Count != 0)
                    {
                        // 初始化默认参数值缓存（仅在首次调用时）
                        if (CachedDefaultValues is null && Ids.Any(id => id.DefaultValue is not null))
                        {
                            InitializeDefaultValueCache(executionManager);
                        }

                        // 评估参数
                        var paramValues = ids.Select(t => t.Run(variateManagerFunc)).ToList();

                        // 补全默认参数
                        for (var i = paramValues.Count; i < Ids.Count; i++)
                        {
                            // 检查取消请求
                            cancellationToken.ThrowIfCancellationRequested();

                            var id = Ids[i];
                            if (id.DefaultValue is not null)
                            {
                                // 优先使用缓存值（常量表达式）
                                paramValues.Add(CachedDefaultValues?.TryGetValue(i, out var cachedValue) == true
                                    ? cachedValue
                                    : id.DefaultValue.Run(executionManager));
                            }
                            else
                            {
                                throw new ArgumentError(
                                    Position,
                                    $"异步函数 '{Id?.IdName ?? "anonymous"}' 的参数 '{id.IdName}' 缺少实参且没有默认值"
                                );
                            }
                        }

                        // 设置参数到作用域
                        for (var i = 0; i < Ids.Count; i++)
                        {
                            executionManager.Set(Ids[i], paramValues[i]);
                        }
                    }

                    // 参数设置完成后，恢复非函数上下文标志
                    // 这样函数体中的赋值语句可以正常查找和修改外部作用域的变量
                    executionManager.IsFunc = false;

                    // 执行函数体
                    BlockStatement.Run(executionManager);

                    // 保存返回值（在清理之前）
                    var result = executionManager.Result;

                    return result;
                }
                finally
                {
                    // 清理资源
                    executionManager.IsReturn = false;
                    executionManager.IsFunc = false;

                    // 移除作用域
                    executionManager.RemoveChildren();
                }
            }
            finally
            {
                executionManager.RecursionDepth--;
                Old8Exception.PopCallStack();
            }
        }, cancellationToken);

        // 创建 TaskLangValue 对象并设置 ExternalManager
        var taskLangValue = new TaskLangValue(task, cancellationToken, Position)
        {
            ExternalManager = CapturedScope ?? variateManagerFunc
        };
        return taskLangValue;
    }

    /// <summary>
    /// 初始化默认参数值缓存
    /// </summary>
    private void InitializeDefaultValueCache(VariateManager manager)
    {
        CachedDefaultValues = new Dictionary<int, LangValueType>();

        for (int i = 0; i < Ids!.Count; i++)
        {
            var param = Ids[i];
            if (param.DefaultValue is not null && IsConstantExpression(param.DefaultValue))
            {
                var defaultValue = param.DefaultValue.Run(manager);
                CachedDefaultValues[i] = defaultValue;
            }
        }
    }

    /// <summary>
    /// 判断是否为常量表达式
    /// </summary>
    private static bool IsConstantExpression(LangExpression? expr)
    {
        return expr switch
        {
            IntLangValue or DoubleLangValue or StringLangValue or BoolLangValue or CharLangValue => true,
            Operation op => IsConstantExpression(op.Left) && IsConstantExpression(op.Right),
            _ => false
        };
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// 生成异步函数的委托，支持异步函数的编译
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 1. 确定参数类型和返回类型
        var parameterTypes = Ids!.Select(item => item.OutputType(local)).ToArray();
        var returnType = typeof(Task<object>);

        // 2. 创建 DynamicMethod
        var dynamicMethod = new DynamicMethod(
            Id?.IdName ?? "AnonymousAsync",
            returnType,
            parameterTypes,
            true
        );

        // 3. 生成方法体
        var methodIl = dynamicMethod.GetILGenerator();

        // 创建新的LocalManager实例，专门用于函数体的IL生成
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

        // 处理参数
        for (var i = 0; i < Ids.Count; i++)
        {
            var id = Ids[i];
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            funcLocal.LocalVarTypes[id.IdName] = paramType;

            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        GenerateMethodBody(methodIl, funcLocal);

        // 4. 创建委托并加载到栈上
        var delegateType = System.Linq.Expressions.Expression.GetDelegateType(
            parameterTypes.Concat([returnType]).ToArray());

        ilGenerator.Emit(OpCodes.Ldnull); // target (null for static method)
        ilGenerator.Emit(OpCodes.Ldftn, dynamicMethod);
        ilGenerator.Emit(OpCodes.Newobj, delegateType.GetConstructors()[0]);
    }

    /// <summary>
    /// 生成异步方法体（状态机启动代码）
    /// </summary>
    public void GenerateMethodBody(ILGenerator ilGenerator, LocalManager local)
    {
        if (!Old8Lang.Compiler.Compiler.EnableAsyncStateMachineAwait)
        {
            local.AsyncStateMachineGenerator = null;

            var returnValueLocal = ilGenerator.DeclareLocal(typeof(object));
            var returnLabel = ilGenerator.DefineLabel();
            local.ReturnValueLocal = returnValueLocal;
            local.ReturnLabel = returnLabel;

            ilGenerator.BeginExceptionBlock();
            BlockStatement.GenerateIl(ilGenerator, local);

            if (BlockStatement.Count == 0 || BlockStatement[^1] is not ReturnStatement)
            {
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Stloc, returnValueLocal);
            }

            ilGenerator.Emit(OpCodes.Leave, returnLabel);
            ilGenerator.BeginFinallyBlock();
            ilGenerator.EndExceptionBlock();

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ldloc, returnValueLocal);

            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
            ilGenerator.Emit(OpCodes.Call, fromResultMethod.MakeGenericMethod(typeof(object)));
            ilGenerator.Emit(OpCodes.Ret);
            return;
        }

        // 创建动态程序集和类型来生成状态机
        var assemblyName = new AssemblyName($"Old8LangAsync_{Id?.IdName ?? "Anonymous"}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType(
            $"AsyncStateMachine_{Id?.IdName ?? "Anonymous"}",
            TypeAttributes.Public |
            TypeAttributes.Sealed |
            TypeAttributes.AutoLayout |  // 使用 AutoLayout 而不是 SequentialLayout
            TypeAttributes.AnsiClass |
            TypeAttributes.BeforeFieldInit,
            typeof(ValueType));

        var stateMachineGenerator =
            new Old8Lang.Generators.AsyncStateMachineGenerator(ilGenerator, local, BlockStatement)
            {
                TypeBuilder = typeBuilder
            };

        local.AsyncStateMachineGenerator = stateMachineGenerator;

        // 提升参数到状态机字段
        if (Ids != null)
        {
            foreach (var id in Ids)
            {
                var argName = id.IdName;
                var argLocal = local.GetLocalVar(argName);
                if (argLocal != null)
                {
                    stateMachineGenerator.DefineVariable(argName, argLocal.LocalType);
                }
            }
        }

        stateMachineGenerator.GenerateStateMachine(typeBuilder);

        var stateMachineType = typeBuilder.CreateType()!;
        var stateField = stateMachineType.GetField(stateMachineGenerator.StateField!.Name)!;
        var builderField = stateMachineType.GetField(stateMachineGenerator.BuilderField!.Name)!;

        if (Old8Lang.Compiler.Compiler.DebugOutputEnabled)
        {
            var moveNext = stateMachineType.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);
            if (moveNext != null) Console.WriteLine(IlDisassembler.Disassemble(moveNext));
        }

        var smLocal = ilGenerator.DeclareLocal(stateMachineType);

        // 1. sm = default;
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Initobj, stateMachineType);

        // 初始化提升的参数字段
        if (Ids != null)
        {
            foreach (var argName in Ids.Select(id => id.IdName))
            {
                if (!stateMachineGenerator.VariableFields.TryGetValue(argName, out var fieldBuilder)) continue;
                var field = stateMachineType.GetField(fieldBuilder.Name)!;
                var argLocal = local.GetLocalVar(argName)!;
                ilGenerator.Emit(OpCodes.Ldloca, smLocal);
                ilGenerator.Emit(OpCodes.Ldloc, argLocal);
                ilGenerator.Emit(OpCodes.Stfld, field);
            }
        }

        // 2. sm.builder = AsyncTaskMethodBuilder<object>.Create();
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetMethod("Create")!);
        ilGenerator.Emit(OpCodes.Stfld, builderField);

        // 3. sm.state = -1;
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_M1);
        ilGenerator.Emit(OpCodes.Stfld, stateField);

        // 4. sm.builder.Start(ref sm);
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Ldflda, builderField);
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetMethod("Start")!
                .MakeGenericMethod(stateMachineType));

        // 5. return sm.builder.Task;
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Ldflda, builderField);
        ilGenerator.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetProperty("Task")!
                .GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        var parameterTypes = Ids!.Select(item => item.OutputType(local)).ToArray();
        var returnType = typeof(Task<object>);
        return System.Linq.Expressions.Expression.GetDelegateType(
            parameterTypes.Concat([returnType]).ToArray());
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("AsyncFuncLangValue 暂不支持 Visitor 模式访问");
    }
}