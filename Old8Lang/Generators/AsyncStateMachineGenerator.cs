using System.Reflection;
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
    public FieldBuilder? StateField { get; private set; }
    public FieldBuilder? BuilderField { get; private set; }
    
    // 每个 await 表达式对应一个专用的 awaiter 字段
    private readonly Dictionary<int, FieldBuilder> AwaiterFields = [];

    // 状态常量
    private const int StateNotStarted = -1;
    private const int StateCompleted = -2;

    // 变量提升字段映射
    public Dictionary<string, FieldBuilder> VariableFields { get; } = new Dictionary<string, FieldBuilder>();
    public TypeBuilder? TypeBuilder { get; set; }

    // Await 表达式到状态索引的映射
    private readonly Dictionary<AwaitExpression, int> AwaitStateMap = [];
    
    // 状态索引到标签的映射
    private readonly Dictionary<int, Label> StateLabels = [];

    // 当前正在生成的 MoveNext ILGenerator
    private ILGenerator? MoveNextIl;
    
    // 返回标签（用于退出 try 块）
    private Label? _retLabel;

    // 是否已经执行了 return 语句（避免重复的 leave 指令）
    private bool HasReturned { get; set; }

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
    /// 识别异步函数体中的await表达式位置
    /// </summary>
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

        // 处理赋值语句
        if (statement is SetStatement setStmt && setStmt.Value is not null)
        {
            IdentifyAwaitInExpression(setStmt.Value);
            return;
        }

        if (statement is FuncRunStatement funcRunStmt && funcRunStmt.Expression is not null)
        {
            IdentifyAwaitInExpression(funcRunStmt.Expression);
            return;
        }

        // 处理返回语句
        if (statement is ReturnStatement returnStmt)
        {
            if (returnStmt.Expression != null)
            {
                IdentifyAwaitInExpression(returnStmt.Expression);
            }
            return;
        }
        
        // 处理 If 语句
        if (statement is IfStatement ifStmt)
        {
            foreach (var child in ifStmt.Children)
            {
                IdentifyAwaitInExpression(child.Condition);
                IdentifyAwaitExpressions(child.Block);
            }
            if (ifStmt.ElseBlock != null)
            {
                IdentifyAwaitExpressions(ifStmt.ElseBlock);
            }
            return;
        }
        
        // 处理 While 语句
        if (statement is WhileStatement whileStmt)
        {
            IdentifyAwaitInExpression(whileStmt.Condition);
            IdentifyAwaitExpressions(whileStmt.Block);
            return;
        }
        
        // 处理 For 语句
        if (statement is ForStatement forStmt)
        {
            if (forStmt.Init != null) IdentifyAwaitExpressions(forStmt.Init);
            if (forStmt.Condition != null) IdentifyAwaitInExpression(forStmt.Condition);
            if (forStmt.Operation != null) IdentifyAwaitExpressions(forStmt.Operation);
            IdentifyAwaitExpressions(forStmt.Block);
            return;
        }

        // 处理 ForIn 语句 (async for)
        if (statement is ForInStatement forInStmt)
        {
            IdentifyAwaitInExpression(forInStmt.Expression);
            IdentifyAwaitExpressions(forInStmt.Body);
            return;
        }

        // 处理 Try 语句
        if (statement is TryStatement tryStmt)
        {
            IdentifyAwaitExpressions(tryStmt.TryBlock);
            foreach (var catchClause in tryStmt.CatchBlocks)
            {
                IdentifyAwaitExpressions(catchClause.catchBlock);
            }
            if (tryStmt.FinallyBlock != null)
            {
                IdentifyAwaitExpressions(tryStmt.FinallyBlock);
            }
            return;
        }
    }

    /// <summary>
    /// 识别表达式中的await表达式
    /// </summary>
    private void IdentifyAwaitInExpression(LangExpression expression)
    {
        if (expression == null) return;

        // 直接检查是否为 await 表达式
        if (expression is AwaitExpression awaitExpr)
        {
            int stateIndex = AwaitStateMap.Count;
            AwaitStateMap[awaitExpr] = stateIndex;
            IdentifyAwaitInExpression(awaitExpr.Expression);
            return;
        }

        // 递归检查操作符表达式
        if (expression is Operation op)
        {
            if (op.Left is not null) IdentifyAwaitInExpression(op.Left);
            if (op.Right is not null) IdentifyAwaitInExpression(op.Right);
            return;
        }

        // 递归检查函数调用的参数
        if (expression is FunctionCallExpression funcCall && funcCall.Arguments is not null)
        {
            foreach (var param in funcCall.Arguments)
            {
                IdentifyAwaitInExpression(param);
            }
            return;
        }
        
        // 检查 List/Array 字面量
        if (expression is AST.Expression.Value.ListLangValue listVal && listVal.Values != null)
        {
             foreach (var val in listVal.Values) IdentifyAwaitInExpression(val);
             return;
        }
        
        if (expression is AST.Expression.Value.ArrayLangValue arrayVal && arrayVal.Values != null)
        {
             foreach (var val in arrayVal.Values) IdentifyAwaitInExpression(val);
             return;
        }
    }

    /// <summary>
    /// 生成异步状态机类
    /// </summary>
    public void GenerateStateMachine(TypeBuilder typeBuilder)
    {
        TypeBuilder = typeBuilder;

        // 首先添加接口实现
        typeBuilder.AddInterfaceImplementation(typeof(IAsyncStateMachine));

        GenerateStateMachineFields(typeBuilder);
        GenerateMoveNextMethod(typeBuilder);
        GenerateSetStateMachineMethod(typeBuilder);
    }

    /// <summary>
    /// 定义提升的变量（作为字段）
    /// </summary>
    public void DefineVariable(string name, Type type)
    {
        if (VariableFields.ContainsKey(name)) return;
        if (TypeBuilder == null) throw new InvalidOperationException("TypeBuilder not initialized");
        
        var field = TypeBuilder.DefineField(name, type, FieldAttributes.Public);
        VariableFields[name] = field;
    }

    /// <summary>
    /// 加载变量（从字段）
    /// </summary>
    public void LoadVariable(ILGenerator il, string name)
    {
        if (VariableFields.TryGetValue(name, out var field))
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
        }
        else
        {
            throw new Exception($"Variable '{name}' not found in async state machine fields");
        }
    }

    /// <summary>
    /// 存储变量（到字段）
    /// </summary>
    public void StoreVariable(ILGenerator il, string name)
    {
        if (VariableFields.TryGetValue(name, out var field))
        {
            var tempLocal = il.DeclareLocal(field.FieldType);
            il.Emit(OpCodes.Stloc, tempLocal);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, tempLocal);
            il.Emit(OpCodes.Stfld, field);
        }
        else
        {
            throw new Exception($"Variable '{name}' not found in async state machine fields");
        }
    }

    /// <summary>
    /// 生成状态机字段
    /// </summary>
    private void GenerateStateMachineFields(TypeBuilder typeBuilder)
    {
        StateField = typeBuilder.DefineField("<>1__state", typeof(int), FieldAttributes.Public);
        BuilderField = typeBuilder.DefineField("<>t__builder", typeof(AsyncTaskMethodBuilder<object>), FieldAttributes.Public);
    }
    
    /// <summary>
    /// 获取或创建 awaiter 字段
    /// </summary>
    private FieldBuilder GetOrCreateAwaiterField(int stateIndex, Type awaiterType)
    {
        if (TypeBuilder == null) throw new InvalidOperationException("TypeBuilder not initialized");
        
        if (AwaiterFields.TryGetValue(stateIndex, out var field))
        {
            return field;
        }
        
        // 创建新的 awaiter 字段
        field = TypeBuilder.DefineField($"<>u__{stateIndex}", awaiterType, FieldAttributes.Public);
        AwaiterFields[stateIndex] = field;
        return field;
    }

    /// <summary>
    /// 生成MoveNext方法
    /// </summary>
    private void GenerateMoveNextMethod(TypeBuilder typeBuilder)
    {
        var moveNextMethod = typeBuilder.DefineMethod(
            "MoveNext",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
            null,
            Type.EmptyTypes);
            
        typeBuilder.AddInterfaceImplementation(typeof(IAsyncStateMachine));
        typeBuilder.DefineMethodOverride(moveNextMethod, typeof(IAsyncStateMachine).GetMethod("MoveNext")!);

        var il = moveNextMethod.GetILGenerator();
        MoveNextIl = il;
        
        // 定义返回标签
        _retLabel = il.DefineLabel();
        
        var exceptionLocal = il.DeclareLocal(typeof(Exception));
        il.BeginExceptionBlock();

        // --- 状态分发 (Switch) ---
        int stateCount = Old8Lang.Compiler.Compiler.EnableAsyncStateMachineAwait ? AwaitStateMap.Count : 0;
        var labels = stateCount > 0 ? new Label[stateCount] : Array.Empty<Label>();
        if (stateCount > 0)
        {
            for (int i = 0; i < stateCount; i++)
            {
                labels[i] = il.DefineLabel();
                StateLabels[i] = labels[i];
            }
        }
        
        var startLabel = il.DefineLabel();
        
        if (stateCount > 0)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, StateField!);
            il.Emit(OpCodes.Switch, labels);
        }
        
        il.Emit(OpCodes.Br_S, startLabel);
        
        il.MarkLabel(startLabel);

        // --- 生成函数体 ---
        LocalManager.AsyncStateMachineGenerator = this;
        BlockStatement.GenerateIl(il, LocalManager);
        
        // --- 完成处理 ---
        // 只有当函数体中没有执行 return 语句时，才调用 EmitReturnInternal
        if (!HasReturned)
        {
            EmitReturnInternal(il, typeof(object));
        }
        
        // --- 异常处理 ---
        il.BeginCatchBlock(typeof(Exception));
        il.Emit(OpCodes.Stloc, exceptionLocal);
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, StateCompleted);
        il.Emit(OpCodes.Stfld, StateField!);
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, BuilderField!);
        il.Emit(OpCodes.Ldloc, exceptionLocal);
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetException")!);
        il.Emit(OpCodes.Leave, _retLabel.Value);
        
        il.EndExceptionBlock();
        
        // 标记返回标签
        il.MarkLabel(_retLabel.Value);
        il.Emit(OpCodes.Ret);
    }
    
    public int GetStateIndex(AwaitExpression expr)
    {
        if (AwaitStateMap.TryGetValue(expr, out int index)) return index;
        int newIndex = AwaitStateMap.Count;
        AwaitStateMap[expr] = newIndex;
        return newIndex;
    }
    
    public Label GetStateLabel(int stateIndex)
    {
        if (StateLabels.TryGetValue(stateIndex, out Label label)) return label;
        if (MoveNextIl == null) throw new InvalidOperationException("MoveNext ILGenerator not initialized");
        label = MoveNextIl.DefineLabel();
        StateLabels[stateIndex] = label;
        return label;
    }

    public void EmitAwaitYield(ILGenerator il, int stateIndex, LocalBuilder awaiterLocal)
    {
        // 1. 设置状态为 stateIndex
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, stateIndex);
        il.Emit(OpCodes.Stfld, StateField!);
        
        // 2. 保存 awaiter 到专用字段
        var awaiterField = GetOrCreateAwaiterField(stateIndex, awaiterLocal.LocalType);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc, awaiterLocal);
        il.Emit(OpCodes.Stfld, awaiterField);
        
        // 3. 调用 AwaitUnsafeOnCompleted
        // 签名: void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, BuilderField!);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, awaiterField);
        il.Emit(OpCodes.Ldarg_0);
        
        var awaitUnsafeOnCompletedMethod = typeof(AsyncTaskMethodBuilder<object>)
            .GetMethod("AwaitUnsafeOnCompleted", BindingFlags.Public | BindingFlags.Instance);
        
        if (awaitUnsafeOnCompletedMethod == null)
            throw new InvalidOperationException("Cannot find AwaitUnsafeOnCompleted method on AsyncTaskMethodBuilder<object>");
        
        // 创建泛型方法：AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>
        var genericMethod = awaitUnsafeOnCompletedMethod.MakeGenericMethod(awaiterLocal.LocalType, TypeBuilder!);
        il.Emit(OpCodes.Call, genericMethod);
            
        // 4. 返回 (挂起)
        il.Emit(OpCodes.Leave, _retLabel!.Value);
    }
    
    public void EmitAwaitResume(ILGenerator il, int stateIndex, LocalBuilder awaiterLocal)
    {
        // 1. 标记恢复标签
        il.MarkLabel(GetStateLabel(stateIndex));
        
        // 2. 从专用字段恢复 awaiter
        var awaiterField = GetOrCreateAwaiterField(stateIndex, awaiterLocal.LocalType);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, awaiterField);
        il.Emit(OpCodes.Stloc, awaiterLocal);
        
        // 3. 重置状态为 -1 (Running)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, StateNotStarted);
        il.Emit(OpCodes.Stfld, StateField!);
    }

    public void EmitReturn(ILGenerator il, Type returnType)
    {
        var resultLocal = il.DeclareLocal(typeof(object));
        if (returnType == typeof(void))
        {
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc, resultLocal);
        }
        else
        {
            if (returnType.IsValueType) il.Emit(OpCodes.Box, returnType);
            il.Emit(OpCodes.Stloc, resultLocal);
        }
        
        EmitReturnInternal(il, typeof(object), resultLocal);
    }
    
    private void EmitReturnInternal(ILGenerator il, Type resultType, LocalBuilder? resultLocal = null)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, BuilderField!);
        
        if (resultLocal != null) il.Emit(OpCodes.Ldloc, resultLocal);
        else il.Emit(OpCodes.Ldnull);
        
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetResult")!);
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, StateCompleted);
        il.Emit(OpCodes.Stfld, StateField!);
        
        il.Emit(OpCodes.Leave, _retLabel!.Value);

        HasReturned = true;
    }

    private void GenerateSetStateMachineMethod(TypeBuilder typeBuilder)
    {
        var setStateMachineMethod = typeBuilder.DefineMethod(
            "SetStateMachine",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
            null,
            [typeof(IAsyncStateMachine)]);

        typeBuilder.DefineMethodOverride(setStateMachineMethod, typeof(IAsyncStateMachine).GetMethod("SetStateMachine")!);

        var il = setStateMachineMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, BuilderField!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, typeof(AsyncTaskMethodBuilder<object>).GetMethod("SetStateMachine")!);
        il.Emit(OpCodes.Ret);
    }
}
