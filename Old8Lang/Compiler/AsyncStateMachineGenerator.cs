using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Old8Lang.AST.Statement;

namespace Old8Lang.Compiler;

/// <summary>
/// 异步状态机生成器
/// 负责为异步函数生成状态机代码
/// </summary>
public class AsyncStateMachineGenerator
{
    private readonly ILGenerator _ilGenerator;
    private readonly LocalManager _localManager;
    private readonly BlockStatement _blockStatement;
    
    // 状态机字段
    private FieldBuilder? _stateField;
    private FieldBuilder? _builderField;
    private FieldBuilder? _awaiterField;
    
    // 状态常量
    private const int StateNotStarted = -1;
    private const int StateCompleted = -2;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="localManager">局部变量管理器</param>
    /// <param name="blockStatement">异步函数体</param>
    public AsyncStateMachineGenerator(ILGenerator ilGenerator, LocalManager localManager, BlockStatement blockStatement)
    {
        _ilGenerator = ilGenerator;
        _localManager = localManager;
        _blockStatement = blockStatement;
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
        _stateField = typeBuilder.DefineField(
            "<state>",
            typeof(int),
            System.Reflection.FieldAttributes.Private);
        
        // 异步方法构建器字段
        _builderField = typeBuilder.DefineField(
            "<builder>",
            typeof(AsyncTaskMethodBuilder<object>),
            System.Reflection.FieldAttributes.Private);
        
        // 等待器字段：用于存储当前等待的任务等待器
        _awaiterField = typeBuilder.DefineField(
            "<awaiter>",
            typeof(TaskAwaiter<object>),
            System.Reflection.FieldAttributes.Private);
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
        
        // 生成try-catch块来处理异常
        var tryLabel = moveNextIl.BeginExceptionBlock();
        
        // 加载状态字段
        moveNextIl.Emit(OpCodes.Ldarg_0);
        moveNextIl.Emit(OpCodes.Ldfld, _stateField!);
        
        // 定义状态标签
        var stateLabels = new Dictionary<int, Label>();
        for (int i = 0; i < 10; i++) // 预定义10个状态标签
        {
            stateLabels[i] = moveNextIl.DefineLabel();
        }
        
        var stateCompletedLabel = moveNextIl.DefineLabel();
        
        // 状态跳转表
        moveNextIl.Emit(OpCodes.Switch, stateLabels.Values.ToArray());
        
        // 初始状态（StateNotStarted）
        GenerateInitialStateCode(moveNextIl, stateLabels);
        
        // 处理其他状态
        for (int i = 0; i < 10; i++)
        {
            moveNextIl.MarkLabel(stateLabels[i]);
            GenerateStateCode(moveNextIl, i, stateLabels);
        }
        
        // 完成状态
        moveNextIl.MarkLabel(stateCompletedLabel);
        GenerateCompletedStateCode(moveNextIl);
        
        // 结束try块
        moveNextIl.EndExceptionBlock();
        
        // 异常处理
        var catchLabel = moveNextIl.DefineLabel();
        moveNextIl.BeginCatchBlock(typeof(Exception));
        GenerateExceptionHandlingCode(moveNextIl);
        moveNextIl.EndExceptionBlock();
    }
    
    /// <summary>
    /// 生成初始状态代码
    /// </summary>
    private void GenerateInitialStateCode(ILGenerator il, Dictionary<int, Label> stateLabels)
    {
        // 初始化异步方法构建器
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("Create")!);
        il.Emit(OpCodes.Stfld, _builderField!);
        
        // 设置初始状态为0
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stfld, _stateField!);
        
        // 跳转到第一个状态
        il.Emit(OpCodes.Br, stateLabels[0]);
    }
    
    /// <summary>
    /// 生成指定状态的代码
    /// </summary>
    private void GenerateStateCode(ILGenerator il, int state, Dictionary<int, Label> stateLabels)
    {
        // 这里根据具体状态生成代码
        // 遍历异步函数体中的语句，处理await表达式
        
        // 简化实现：直接处理函数体中的语句
        // 实际实现需要根据状态来决定从哪个语句继续执行
        
        // 检查是否有更多语句需要处理
        if (state < _blockStatement.Count)
        {
            // 获取当前状态对应的语句
            var statement = _blockStatement[state];
            
            // 生成语句的IL代码
            statement.GenerateIl(il, _localManager);
            
            // 设置下一个状态
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, state + 1);
            il.Emit(OpCodes.Stfld, _stateField!);
            
            // 跳转到下一个状态
            il.Emit(OpCodes.Br, stateLabels[state + 1]);
        }
        else
        {
            // 所有语句处理完成，跳转到完成状态
            il.Emit(OpCodes.Br, stateLabels[StateCompleted]);
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
        il.Emit(OpCodes.Ldfld, _builderField!);
        il.Emit(OpCodes.Ldnull); // 假设返回值为null，实际实现需要返回函数的实际返回值
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetResult")!);
        
        // 2. 设置状态为已完成
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, StateCompleted);
        il.Emit(OpCodes.Stfld, _stateField!);
        
        // 3. 返回
        il.Emit(OpCodes.Ret);
    }
    
    /// <summary>
    /// 生成异常处理代码
    /// </summary>
    private void GenerateExceptionHandlingCode(ILGenerator il)
    {
        // 将异常传递给构建器
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _builderField!);
        il.Emit(OpCodes.Ldarg_1); // 异常对象
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetException")!);
        
        // 设置状态为已完成
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, StateCompleted);
        il.Emit(OpCodes.Stfld, _stateField!);
        
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
            new[] { typeof(object) });
        
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
        il.Emit(OpCodes.Stfld, _stateField!);
        
        // 3. 调用状态机的MoveNext方法
        il.Emit(OpCodes.Callvirt, methodBuilder.DeclaringType.GetMethod("MoveNext")!);
        
        // 4. 返回状态机的结果
        il.Emit(OpCodes.Ldfld, _builderField!);
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetProperty("Task")!.GetGetMethod()!);
        
        // 5. 生成返回指令
        il.Emit(OpCodes.Ret);
    }
}