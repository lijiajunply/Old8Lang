using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;

namespace Old8Lang.Generators;

/// <summary>
/// 异步状态机生成器
/// 负责为异步函数生成状态机代码
/// </summary>
public class AsyncStateMachineGenerator
{
    private readonly ILGenerator IlGenerator;
    private readonly LocalManager LocalManager;
    private readonly BlockStatement BlockStatement;

    // 状态机字段
    private FieldBuilder? StateField;
    private FieldBuilder? BuilderField;
    private FieldBuilder? AwaiterField;
    private List<FieldBuilder> LocalVariableFields = [];

    // 状态常量
    private const int StateNotStarted = -1;
    private const int StateCompleted = -2;

    // await表达式信息列表，用于生成状态转换
    private readonly List<AwaitInfo> AwaitInfos = [];

    /// <summary>
    /// await 表达式信息
    /// </summary>
    private class AwaitInfo
    {
        public int StateIndex { get; set; }
        public AwaitExpression AwaitExpression { get; set; } = null!;
        public OldStatement ContainingStatement { get; set; } = null!;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="localManager">局部变量管理器</param>
    /// <param name="blockStatement">异步函数体</param>
    public AsyncStateMachineGenerator(ILGenerator ilGenerator, LocalManager localManager, BlockStatement blockStatement)
    {
        IlGenerator = ilGenerator;
        LocalManager = localManager;
        BlockStatement = blockStatement;

        // 初始化：识别await表达式位置
        IdentifyAwaitExpressions(blockStatement);
    }

    /// <summary>
    /// 识别异步函数体中的await表达式位置（简化实现）
    /// 注意：这是一个简化版本，只处理最常见的情况
    /// 完整实现需要遍历所有语句类型和表达式类型
    /// </summary>
    /// <param name="statement">当前语句</param>
    private void IdentifyAwaitExpressions(OldStatement statement)
    {
        // 递归处理块语句
        if (statement is BlockStatement block)
        {
            for (int i = 0; i < block.Count; i++)
            {
                IdentifyAwaitExpressions(block[i]);
            }
            return;
        }

        // 处理赋值语句（最常见的包含 await 的情况）
        if (statement is SetStatement setStmt && setStmt.Value is not null)
        {
            IdentifyAwaitInExpression(setStmt.Value, setStmt);
            return;
        }

        // 处理返回语句
        if (statement is ReturnStatement returnStmt)
        {
            // ReturnStatement 使用主构造函数参数 returnExpression
            // 我们需要通过反射或其他方式访问它
            // 简化处理：暂时跳过返回语句中的 await 识别
            // TODO: 添加对返回语句中 await 的支持
            return;
        }

        // TODO: 添加对其他语句类型的支持（if、for、while 等）
        // 当前简化实现只处理最常见的情况
    }

    /// <summary>
    /// 识别表达式中的await表达式（简化实现）
    /// </summary>
    /// <param name="expression">当前表达式</param>
    /// <param name="containingStatement">包含该表达式的语句</param>
    private void IdentifyAwaitInExpression(LangExpression expression, OldStatement containingStatement)
    {
        // 直接检查是否为 await 表达式
        if (expression is AwaitExpression awaitExpr)
        {
            // 发现await表达式，记录信息
            AwaitInfos.Add(new AwaitInfo
            {
                StateIndex = AwaitInfos.Count,
                AwaitExpression = awaitExpr,
                ContainingStatement = containingStatement
            });
            // 继续递归检查 await 的内部表达式
            IdentifyAwaitInExpression(awaitExpr.Expression, containingStatement);
            return;
        }

        // 递归检查操作符表达式
        if (expression is Operation op)
        {
            if (op.Left is not null)
                IdentifyAwaitInExpression(op.Left, containingStatement);
            if (op.Right is not null)
                IdentifyAwaitInExpression(op.Right, containingStatement);
            return;
        }

        // 递归检查函数调用的参数
        if (expression is FunctionCallExpression funcCall && funcCall.Arguments is not null)
        {
            foreach (var param in funcCall.Arguments)
            {
                IdentifyAwaitInExpression(param, containingStatement);
            }
            return;
        }

        // TODO: 添加对其他表达式类型的支持
        // 当前简化实现只处理最常见的情况
    }

    /// <summary>
    /// 生成异步状态机类
    /// </summary>
    /// <param name="typeBuilder">类型生成器</param>
    public void GenerateStateMachine(TypeBuilder typeBuilder)
    {
        // 生成状态机字段
        GenerateStateMachineFields(typeBuilder);

        // 生成MoveNext方法
        GenerateMoveNextMethod(typeBuilder);

        // 生成SetStateMachine方法
        GenerateSetStateMachineMethod(typeBuilder);
    }

    /// <summary>
    /// 生成状态机字段
    /// </summary>
    private void GenerateStateMachineFields(TypeBuilder typeBuilder)
    {
        // 状态字段：当前状态
        StateField = typeBuilder.DefineField(
            "<state>",
            typeof(int),
            System.Reflection.FieldAttributes.Private);

        // 异步方法构建器字段
        BuilderField = typeBuilder.DefineField(
            "<builder>",
            typeof(AsyncTaskMethodBuilder<object>),
            System.Reflection.FieldAttributes.Private);

        // 等待器字段：用于存储当前等待的任务等待器
        AwaiterField = typeBuilder.DefineField(
            "<awaiter>",
            typeof(TaskAwaiter<object>),
            System.Reflection.FieldAttributes.Private);

        // 简化实现：暂时不生成本地变量字段
        // 后续可以添加更复杂的本地变量处理逻辑
    }

    /// <summary>
    /// 生成MoveNext方法
    /// 这是状态机的核心方法，处理状态转换和异步操作
    /// </summary>
    private void GenerateMoveNextMethod(TypeBuilder typeBuilder)
    {
        // 定义MoveNext方法：public void MoveNext()
        var moveNextMethod = typeBuilder.DefineMethod(
            "MoveNext",
            System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig,
            null,
            Type.EmptyTypes);

        var moveNextIl = moveNextMethod.GetILGenerator();

        // 定义状态标签
        var stateLabels = new Dictionary<int, Label>();
        // 为每个await表达式生成状态标签
        int maxStates = Math.Max(AwaitInfos.Count + 1, 1); // +1 是因为还有初始状态
        for (int i = 0; i < maxStates; i++)
        {
            stateLabels[i] = moveNextIl.DefineLabel();
        }

        var stateCompletedLabel = moveNextIl.DefineLabel();
        var initialStateLabel = moveNextIl.DefineLabel();

        // 生成状态机逻辑，不使用异常块（简化实现）
        // 加载状态字段
        moveNextIl.Emit(OpCodes.Ldarg_0);
        moveNextIl.Emit(OpCodes.Ldfld, StateField!);

        // 状态跳转逻辑
        // 注意：状态值从 0 开始，Switch 指令基于 0 的索引
        // 如果状态值 < 0 或 >= stateLabels.Count，会跳转到 Switch 之后的指令
        if (stateLabels.Count > 0)
        {
            // 使用 Switch 指令处理多个状态
            moveNextIl.Emit(OpCodes.Switch, stateLabels.Values.ToArray());
        }

        // Switch 的默认分支（状态值无效时）
        // 跳转到初始状态
        moveNextIl.Emit(OpCodes.Br, stateLabels[0]);

        // 处理其他状态
        for (int i = 0; i < maxStates; i++)
        {
            moveNextIl.MarkLabel(stateLabels[i]);
            GenerateStateCode(moveNextIl, i, stateLabels, stateCompletedLabel);
        }

        // 完成状态
        moveNextIl.MarkLabel(stateCompletedLabel);
        GenerateCompletedStateCode(moveNextIl);
    }

    /// <summary>
    /// 生成初始状态代码
    /// </summary>
    private void GenerateInitialStateCode(ILGenerator il, Dictionary<int, Label> stateLabels)
    {
        // 初始化异步方法构建器
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("Create")!);
        il.Emit(OpCodes.Stfld, BuilderField!);

        // 设置初始状态为0
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stfld, StateField!);

        // 跳转到第一个状态
        il.Emit(OpCodes.Br, stateLabels[0]);
    }

    /// <summary>
    /// 生成指定状态的代码
    /// </summary>
    private void GenerateStateCode(ILGenerator il, int state, Dictionary<int, Label> stateLabels,
        Label stateCompletedLabel)
    {
        // 状态 0 是初始状态，需要初始化 Builder
        if (state == 0)
        {
            // 初始化异步方法构建器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("Create")!);
            il.Emit(OpCodes.Stfld, BuilderField!);
        }

        // 检查是否有更多语句需要处理
        if (state < BlockStatement.Count)
        {
            // 获取当前状态对应的语句
            var statement = BlockStatement[state];

            // 检查当前语句是否包含await表达式
            bool hasAwait = AwaitInfos.Any(info => info.StateIndex == state);

            if (hasAwait)
            {
                // 处理包含await表达式的语句
                GenerateAwaitStateCode(il, state, stateLabels);
            }
            else
            {
                // 处理普通语句
                // 生成语句的IL代码
                statement.GenerateIl(il, LocalManager);

                // 设置下一个状态
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, state + 1);
                il.Emit(OpCodes.Stfld, StateField!);

                // 跳转到下一个状态
                il.Emit(OpCodes.Br, stateLabels[Math.Min(state + 1, stateLabels.Count - 1)]);
            }
        }
        else
        {
            // 所有语句处理完成，跳转到完成状态
            il.Emit(OpCodes.Br, stateCompletedLabel);
        }
    }

    /// <summary>
    /// 生成包含await表达式的状态代码
    /// 注意：这是一个简化实现，暂时生成同步等待代码
    /// TODO: 实现真正的异步状态机切换
    /// </summary>
    private void GenerateAwaitStateCode(ILGenerator il, int state, Dictionary<int, Label> stateLabels)
    {
        // 获取当前状态对应的语句
        var statement = BlockStatement[state];

        // 简化实现：直接生成语句的IL代码
        // 这会导致 await 表达式使用同步等待（GetResult()）
        // 但至少可以让代码编译和运行
        statement.GenerateIl(il, LocalManager);

        // 设置下一个状态
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, state + 1);
        il.Emit(OpCodes.Stfld, StateField!);

        // 跳转到下一个状态
        if (state + 1 < stateLabels.Count)
        {
            il.Emit(OpCodes.Br, stateLabels[state + 1]);
        }
        else
        {
            // 所有语句处理完成，跳转到完成状态
            il.Emit(OpCodes.Br, stateLabels[stateLabels.Count - 1]);
        }
    }

    /// <summary>
    /// 生成完成状态代码
    /// </summary>
    private void GenerateCompletedStateCode(ILGenerator il)
    {
        // 1. 标记任务完成，返回null（简化实现）
        // 实际实现中，应该返回函数的实际返回值
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, BuilderField!);
        il.Emit(OpCodes.Ldnull); // 假设返回值为null，实际实现需要返回函数的实际返回值
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetResult")!);

        // 2. 设置状态为已完成
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, StateCompleted);
        il.Emit(OpCodes.Stfld, StateField!);

        // 3. 返回
        il.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 生成异常处理代码
    /// </summary>
    private void GenerateExceptionHandlingCode(ILGenerator il)
    {
        // 检查异常类型，特殊处理OperationCanceledException
        var catchEndLabel = il.DefineLabel();
        var canceledLabel = il.DefineLabel();

        // 检查是否为OperationCanceledException
        il.Emit(OpCodes.Ldarg_1); // 加载异常对象
        il.Emit(OpCodes.Isinst, typeof(OperationCanceledException));
        il.Emit(OpCodes.Brtrue, canceledLabel);

        // 常规异常处理
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, BuilderField!);
        il.Emit(OpCodes.Ldarg_1); // 异常对象
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetException")!);
        il.Emit(OpCodes.Br, catchEndLabel);

        // 取消操作处理
        il.MarkLabel(canceledLabel);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, BuilderField!);
        il.Emit(OpCodes.Ldarg_1); // 异常对象
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetException")!);

        // 处理完成
        il.MarkLabel(catchEndLabel);

        // 设置状态为已完成
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, StateCompleted);
        il.Emit(OpCodes.Stfld, StateField!);

        // 返回
        il.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 生成SetStateMachine方法
    /// 用于设置状态机实例
    /// </summary>
    private void GenerateSetStateMachineMethod(TypeBuilder typeBuilder)
    {
        // 定义SetStateMachine方法：public void SetStateMachine(object stateMachine)
        // 简化实现，使用object类型代替IAsyncStateMachine
        var setStateMachineMethod = typeBuilder.DefineMethod(
            "SetStateMachine",
            System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig,
            null,
            [typeof(object)]);

        var setStateMachineIl = setStateMachineMethod.GetILGenerator();

        // 简单实现，不做任何操作
        setStateMachineIl.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 生成异步函数的IL代码
    /// 实现真正的异步方法，创建并启动状态机
    /// </summary>
    /// <param name="methodBuilder">方法生成器</param>
    public void GenerateAsyncMethod(MethodBuilder methodBuilder)
    {
        var il = methodBuilder.GetILGenerator();

        // 1. 创建状态机实例
        il.Emit(OpCodes.Newobj, methodBuilder.DeclaringType!.GetConstructor(Type.EmptyTypes)!);

        // 2. 初始化状态机
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4, StateNotStarted);
        il.Emit(OpCodes.Stfld, StateField!);

        // 3. 调用状态机的MoveNext方法
        il.Emit(OpCodes.Callvirt, methodBuilder.DeclaringType.GetMethod("MoveNext")!);

        // 4. 返回状态机的结果
        il.Emit(OpCodes.Ldfld, BuilderField!);
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetProperty("Task")!.GetGetMethod()!);

        // 5. 生成返回指令
        il.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 生成异步函数的IL代码（使用DynamicMethod）
    /// 实现真正的异步方法，创建并启动状态机
    /// </summary>
    /// <param name="dynamicMethod">动态方法</param>
    public void GenerateAsyncMethod(DynamicMethod dynamicMethod)
    {
        var il = dynamicMethod.GetILGenerator();

        // 简化实现：返回一个已完成的Task<object>
        // 完整实现需要创建状态机类和相关方法
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Call, typeof(Task)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true })
            .MakeGenericMethod(typeof(object)));
        il.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 生成异步函数体的IL代码
    /// 直接在现有方法中生成异步函数体的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    public void GenerateAsyncMethodBody(ILGenerator ilGenerator)
    {
        // 遍历异步函数体中的语句，生成IL代码
        // 对于await表达式，生成状态转换逻辑

        // 简化实现：直接生成函数体的IL代码
        // 完整实现需要生成状态机代码
        BlockStatement.GenerateIl(ilGenerator, LocalManager);
    }
}