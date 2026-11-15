using Old8Lang.Compiler;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Execution;

namespace Old8Lang.AsyncGeneration;

/// <summary>
/// 异步状态机生成器
/// 负责为异步函数生成状态机代码
/// </summary>
public class AsyncStateMachineGenerator : IAsyncStateMachineGenerator
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

    /// <summary>
    /// await 所在的受保护区域。
    /// </summary>
    /// <remarks>
    /// 恢复标签可能落在受保护区域内部，而 CLR 只允许从区域外部跳到区域的**首指令**。
    /// 因此每个含 await 的受保护区域（try 块、using 块）都需要自己的入口标签与派发链：
    /// 外层派发跳到内层区域入口，内层派发再跳到本区域的恢复标签。
    /// catch / finally / defer 等位置不会生成可恢复的 IL，区内的 await 直接报错。
    /// </remarks>
    private sealed class AwaitRegion
    {
        public List<int> DirectAwaitStates { get; } = [];
        public List<object> ChildRegions { get; } = [];
    }

    /// <summary>
    /// await 不允许出现的位置描述；null 表示允许。
    /// </summary>
    private static readonly object UnsupportedCatchFinally = "catch / finally 块";
    private static readonly object UnsupportedDefer = "defer 块";
    private static readonly object UnsupportedYield = "yield 表达式";

    private readonly AwaitRegion _rootRegion = new();
    private readonly Dictionary<object, AwaitRegion> _regions = [];
    private readonly Dictionary<object, Label> _regionEntryLabels = [];
    private bool _analysisComplete;

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

        // 初始化：识别 await 位置，并按所在受保护区域分组
        AnalyzeStatement(blockStatement, _rootRegion, unsupportedContext: null);
        _analysisComplete = true;
    }

    /// <summary>
    /// 递归扫描语句，登记 await 状态索引与所在区域。
    /// </summary>
    /// <summary>
    /// 递归扫描语句，登记 await 状态索引与所在区域。
    /// </summary>
    /// <param name="unsupportedContext">
    /// 非 null 表示当前位置不会生成可恢复的 IL（catch/finally/defer/yield），
    /// 其中出现 await 直接报编译错误。
    /// </param>
    private void AnalyzeStatement(OldStatement? statement, AwaitRegion region, object? unsupportedContext)
    {
        switch (statement)
        {
            case null:
                return;

            case BlockStatement block:
                for (int i = 0; i < block.Count; i++)
                {
                    AnalyzeStatement(block[i], region, unsupportedContext);
                }

                return;

            case SetStatement { Value: not null } setStmt:
                AnalyzeExpression(setStmt.Value, region, unsupportedContext);
                return;

            case FuncRunStatement { Expression: not null } funcRunStmt:
                AnalyzeExpression(funcRunStmt.Expression, region, unsupportedContext);
                return;

            case ReturnStatement returnStmt:
                if (returnStmt.Expression != null)
                {
                    AnalyzeExpression(returnStmt.Expression, region, unsupportedContext);
                }

                return;

            case ThrowStatement throwStmt:
                AnalyzeExpression(throwStmt.Expression, region, unsupportedContext);
                return;

            case YieldStatement yieldStmt:
                // yield 在 IL 后端没有生成任何指令，其中的 await 也无从恢复。
                AnalyzeExpression(yieldStmt.YieldExpression, region,
                    unsupportedContext ?? UnsupportedYield);
                return;

            case DeferStatement deferStmt:
                // defer 只在同步函数体里生成 IL，异步状态机中不生成，其中的 await 无法恢复。
                AnalyzeStatement(deferStmt.Statement, region,
                    unsupportedContext ?? UnsupportedDefer);
                return;

            case IfStatement ifStmt:
                foreach (var child in ifStmt.Children)
                {
                    AnalyzeExpression(child.Condition, region, unsupportedContext);
                    AnalyzeStatement(child.Block, region, unsupportedContext);
                }

                if (ifStmt.ElseBlock != null)
                {
                    AnalyzeStatement(ifStmt.ElseBlock, region, unsupportedContext);
                }

                return;

            case WhileStatement whileStmt:
                AnalyzeExpression(whileStmt.Condition, region, unsupportedContext);
                AnalyzeStatement(whileStmt.Block, region, unsupportedContext);
                return;

            case ForStatement forStmt:
                if (forStmt.Init != null) AnalyzeStatement(forStmt.Init, region, unsupportedContext);
                if (forStmt.Condition != null) AnalyzeExpression(forStmt.Condition, region, unsupportedContext);
                if (forStmt.Operation != null) AnalyzeStatement(forStmt.Operation, region, unsupportedContext);
                AnalyzeStatement(forStmt.Block, region, unsupportedContext);
                return;

            case ForInStatement forInStmt:
                AnalyzeExpression(forInStmt.Expression, region, unsupportedContext);
                AnalyzeStatement(forInStmt.Body, region, unsupportedContext);
                return;

            case AsyncForInStatement asyncForInStmt:
                AnalyzeExpression(asyncForInStmt.Expression, region, unsupportedContext);
                AnalyzeStatement(asyncForInStmt.Body, region, unsupportedContext);
                return;

            case SwitchStatement switchStmt:
                AnalyzeExpression(switchStmt.SwitchExpression, region, unsupportedContext);
                foreach (var caseStmt in switchStmt.SwitchCaseList)
                {
                    AnalyzeExpression(caseStmt.Expression, region, unsupportedContext);
                    AnalyzeStatement(caseStmt.BlockStatement, region, unsupportedContext);
                }

                if (switchStmt.DefaultBlockStatement != null)
                {
                    AnalyzeStatement(switchStmt.DefaultBlockStatement, region, unsupportedContext);
                }

                return;

            case SelectStatement selectStmt:
                foreach (var selectCase in selectStmt.Cases)
                {
                    AnalyzeStatement(selectCase.BlockStatement, region, unsupportedContext);
                }

                if (selectStmt.DefaultCase != null)
                {
                    AnalyzeStatement(selectStmt.DefaultCase, region, unsupportedContext);
                }

                return;

            case UsingStatement usingStmt:
                {
                    // using 体被包在 try/finally（Dispose）里，同样是可恢复的受保护区域。
                    var usingRegion = GetOrCreateRegion(usingStmt);
                    region.ChildRegions.Add(usingStmt);
                    AnalyzeExpression(usingStmt.ResourceExpression, region, unsupportedContext);
                    AnalyzeStatement(usingStmt.BlockStatement, usingRegion, unsupportedContext);
                    return;
                }

            case TryStatement tryStmt:
                {
                    // try 块是一个可恢复区域：记录父子关系，await 归属到 try 块。
                    var tryRegion = GetOrCreateRegion(tryStmt);
                    region.ChildRegions.Add(tryStmt);
                    AnalyzeStatement(tryStmt.TryBlock, tryRegion, unsupportedContext);

                    // catch / finally（含 catch 过滤器）只能由异常机制进入，
                    // 恢复标签无法到达；出现 await 时给出明确的编译错误。
                    foreach (var (_, _, filter, catchBlock) in tryStmt.CatchBlocks)
                    {
                        AnalyzeExpression(filter, region,
                            unsupportedContext ?? UnsupportedCatchFinally);
                        AnalyzeStatement(catchBlock, region,
                            unsupportedContext ?? UnsupportedCatchFinally);
                    }

                    if (tryStmt.FinallyBlock != null)
                    {
                        AnalyzeStatement(tryStmt.FinallyBlock, region,
                            unsupportedContext ?? UnsupportedCatchFinally);
                    }

                    return;
                }

            default:
                // 未显式识别的语句：按 AST 的子项兜底遍历，避免漏掉 await。
                for (int i = 0; i < statement.Count; i++)
                {
                    AnalyzeStatement(statement[i], region, unsupportedContext);
                }

                return;
        }
    }

    private AwaitRegion GetOrCreateRegion(object regionNode)
    {
        if (_regions.TryGetValue(regionNode, out var region)) return region;
        region = new AwaitRegion();
        _regions[regionNode] = region;
        return region;
    }

    /// <summary>
    /// 识别表达式中的 await，并登记到所在区域。
    /// </summary>
    /// <summary>
    /// 识别表达式中的 await，并登记到所在区域。
    /// </summary>
    /// <param name="pendingValues">
    /// 求值该表达式时求值栈上是否已经存在待用值。挂起用 <c>leave</c> 离开受保护区域，
    /// 要求求值栈为空；因此只有栈空位置的 await 才是合法的，其余位置直接报错，
    /// 避免生成损坏的表达式值或无效 IL。
    /// </param>
    private void AnalyzeExpression(LangExpression? expression, AwaitRegion region,
        object? unsupportedContext, bool pendingValues = false)
    {
        if (expression == null) return;

        // 直接检查是否为 await 表达式
        if (expression is AwaitExpression awaitExpr)
        {
            if (unsupportedContext is not null)
            {
                throw new CompilerException(
                    $"IL 模式暂不支持 {unsupportedContext} 中的 await：该位置无法从 MoveNext 入口恢复",
                    awaitExpr.Position);
            }

            if (pendingValues)
            {
                throw new CompilerException(
                    "IL 模式暂不支持表达式内部的 await（挂起时求值栈非空）：请先把 await 的结果赋给变量再参与运算",
                    awaitExpr.Position);
            }

            int stateIndex = GetStateIndex(awaitExpr);
            if (!region.DirectAwaitStates.Contains(stateIndex))
            {
                region.DirectAwaitStates.Add(stateIndex);
            }

            // awaitable 表达式本身在挂起前求值完成；外层若有待用值，会一直存在到挂起。
            AnalyzeExpression(awaitExpr.Expression, region, unsupportedContext, pendingValues: false);
            return;
        }

        // 递归检查操作符表达式：左侧先求值，右侧求值时左侧结果已在栈上
        if (expression is Operation op)
        {
            if (op.Left is not null) AnalyzeExpression(op.Left, region, unsupportedContext, pendingValues);
            if (op.Right is not null) AnalyzeExpression(op.Right, region, unsupportedContext, pendingValues: true);
            return;
        }

        // 全局函数 / 方法调用：实参求值前通常会先构造实参数组或加载目标，栈上已有待用值
        if (expression is CallExpression { Arguments: not null } call)
        {
            foreach (var argument in call.Arguments)
            {
                AnalyzeExpression(argument, region, unsupportedContext, pendingValues: true);
            }

            if (call.NamedArguments is not null)
            {
                foreach (var namedArgument in call.NamedArguments)
                {
                    AnalyzeExpression(namedArgument.Value, region, unsupportedContext, pendingValues: true);
                }
            }

            return;
        }

        // 委托 / 函数值调用：被调用表达式先入栈，实参求值时已有待用值
        if (expression is FunctionCallExpression { Arguments: not null } funcCall)
        {
            AnalyzeExpression(funcCall.FunctionExpression, region, unsupportedContext, pendingValues);
            foreach (var param in funcCall.Arguments)
            {
                AnalyzeExpression(param, region, unsupportedContext, pendingValues: true);
            }

            if (funcCall.NamedArguments is not null)
            {
                foreach (var namedArgument in funcCall.NamedArguments)
                {
                    AnalyzeExpression(namedArgument.Value, region, unsupportedContext, pendingValues: true);
                }
            }

            return;
        }

        // 三元表达式：条件被分支指令消耗，两个分支沿用外层的栈状态
        if (expression is AST.Expression.TernaryExpression ternary)
        {
            AnalyzeExpression(ternary.Condition, region, unsupportedContext, pendingValues);
            AnalyzeExpression(ternary.TrueExpression, region, unsupportedContext, pendingValues);
            AnalyzeExpression(ternary.FalseExpression, region, unsupportedContext, pendingValues);
            return;
        }

        // match 表达式：被匹配值与各分支结果之间可能留有中间值，按有待用值处理
        if (expression is AST.Expression.Intermediates.MatchExpression match)
        {
            AnalyzeExpression(match.MatchValue, region, unsupportedContext, pendingValues);
            foreach (var matchCase in match.Cases)
            {
                AnalyzeExpression(matchCase.Pattern, region, unsupportedContext, pendingValues: true);
                AnalyzeExpression(matchCase.GuardCondition, region, unsupportedContext, pendingValues: true);
                AnalyzeExpression(matchCase.ResultExpression, region, unsupportedContext, pendingValues: true);
            }

            return;
        }

        // 字符串模板：各插值片段依次拼接，除第一段外都有待用值
        if (expression is AST.Expression.Intermediates.StringTemplateValue template)
        {
            for (int i = 0; i < template.ExpressionList.Count; i++)
            {
                AnalyzeExpression(template.ExpressionList[i], region, unsupportedContext,
                    pendingValues: pendingValues || i > 0);
            }

            return;
        }

        if (expression is AST.Expression.NestedIndexExpression nestedIndexSyntax)
        {
            AnalyzeExpression(nestedIndexSyntax.Target, region, unsupportedContext, pendingValues);
            AnalyzeExpression(nestedIndexSyntax.Index, region, unsupportedContext, pendingValues: true);
            return;
        }

        if (expression is AST.Expression.NestedSliceExpression nestedSliceSyntax)
        {
            AnalyzeExpression(nestedSliceSyntax.Target, region, unsupportedContext, pendingValues);
            AnalyzeExpression(nestedSliceSyntax.Start, region, unsupportedContext, pendingValues: true);
            if (nestedSliceSyntax.End is not null)
                AnalyzeExpression(nestedSliceSyntax.End, region, unsupportedContext, pendingValues: true);
            if (nestedSliceSyntax.Step is not null)
                AnalyzeExpression(nestedSliceSyntax.Step, region, unsupportedContext, pendingValues: true);
            return;
        }

        if (expression is AST.Expression.IndexExpression indexSyntax)
        {
            AnalyzeExpression(indexSyntax.Key, region, unsupportedContext, pendingValues);
            return;
        }

        if (expression is AST.Expression.SliceExpression sliceSyntax)
        {
            if (sliceSyntax.Start is not null)
                AnalyzeExpression(sliceSyntax.Start, region, unsupportedContext, pendingValues);
            if (sliceSyntax.End is not null)
                AnalyzeExpression(sliceSyntax.End, region, unsupportedContext, pendingValues: true);
            if (sliceSyntax.Step is not null)
                AnalyzeExpression(sliceSyntax.Step, region, unsupportedContext, pendingValues: true);
            return;
        }

        if (expression is AST.Expression.RangeLiteralExpression rangeSyntax)
        {
            if (rangeSyntax.Start is not null)
                AnalyzeExpression(rangeSyntax.Start, region, unsupportedContext, pendingValues);
            if (rangeSyntax.End is not null)
                AnalyzeExpression(rangeSyntax.End, region, unsupportedContext, pendingValues: true);
            return;
        }

        if (expression is AST.Expression.TupleLiteralExpression tupleSyntax)
        {
            for (int i = 0; i < tupleSyntax.Elements.Count; i++)
            {
                AnalyzeExpression(tupleSyntax.Elements[i], region, unsupportedContext,
                    pendingValues: pendingValues || i > 0);
            }

            return;
        }

        if (expression is AST.Expression.DictionaryLiteralExpression dictionarySyntax)
        {
            for (int i = 0; i < dictionarySyntax.Entries.Count; i++)
            {
                var entry = dictionarySyntax.Entries[i];
                AnalyzeExpression(entry.Key, region, unsupportedContext, pendingValues || i > 0);
                AnalyzeExpression(entry.Value, region, unsupportedContext, pendingValues: true);
            }

            return;
        }

        if (expression is AST.Expression.ListLiteralExpression listSyntax)
        {
            for (int i = 0; i < listSyntax.Elements.Count; i++)
            {
                AnalyzeExpression(listSyntax.Elements[i], region, unsupportedContext,
                    pendingValues: pendingValues || i > 0);
            }

            return;
        }

        if (expression is AST.Expression.ArrayLiteralExpression arraySyntax)
        {
            for (int i = 0; i < arraySyntax.Elements.Count; i++)
            {
                AnalyzeExpression(arraySyntax.Elements[i], region, unsupportedContext,
                    pendingValues: pendingValues || i > 0);
            }

            return;
        }

        if (expression is AST.Expression.Value.ListLangValue { Values: not null } listVal)
        {
            for (int i = 0; i < listVal.Values.Count; i++)
            {
                AnalyzeExpression(listVal.Values[i], region, unsupportedContext,
                    pendingValues: pendingValues || i > 0);
            }

            return;
        }

        if (expression is AST.Expression.Value.ArrayLangValue { Values: not null } arrayVal)
        {
            for (int i = 0; i < arrayVal.Values.Count; i++)
            {
                AnalyzeExpression(arrayVal.Values[i], region, unsupportedContext,
                    pendingValues: pendingValues || i > 0);
            }

            return;
        }
    }

    /// <summary>
    /// 在区域入口发射状态派发链：命中某状态就跳到该状态对应的恢复标签或子区域入口。
    /// </summary>
    private void EmitRegionDispatch(ILGenerator il, AwaitRegion region)
    {
        // 子区域：跳到它的首指令（合法），由子区域自己的派发链继续定位。
        foreach (var childRegionNode in region.ChildRegions)
        {
            if (!_regions.TryGetValue(childRegionNode, out var childRegion)) continue;
            foreach (var stateIndex in CollectAwaitStates(childRegion))
            {
                EmitStateCheck(il, stateIndex, GetOrCreateRegionEntryLabel(childRegionNode));
            }
        }

        // 本区域直属的 await：直接跳到恢复标签（同一受保护区域内）。
        foreach (var stateIndex in region.DirectAwaitStates)
        {
            EmitStateCheck(il, stateIndex, GetStateLabel(stateIndex));
        }
    }

    private IEnumerable<int> CollectAwaitStates(AwaitRegion region)
    {
        foreach (var stateIndex in region.DirectAwaitStates) yield return stateIndex;

        foreach (var childRegionNode in region.ChildRegions)
        {
            if (!_regions.TryGetValue(childRegionNode, out var childRegion)) continue;
            foreach (var stateIndex in CollectAwaitStates(childRegion)) yield return stateIndex;
        }
    }

    private void EmitStateCheck(ILGenerator il, int stateIndex, Label target)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, StateField!);
        il.Emit(OpCodes.Ldc_I4, stateIndex);
        il.Emit(OpCodes.Beq, target);
    }

    private Label GetOrCreateRegionEntryLabel(object regionNode)
    {
        if (_regionEntryLabels.TryGetValue(regionNode, out var label)) return label;
        if (MoveNextIl == null) throw new InvalidOperationException("MoveNext ILGenerator not initialized");
        label = MoveNextIl.DefineLabel();
        _regionEntryLabels[regionNode] = label;
        return label;
    }

    /// <summary>
    /// 进入受保护区域（try 块 / using 块）：标记区域首指令并发射该区域的派发链。
    /// </summary>
    public void EnterProtectedRegion(ILGenerator il, object regionNode)
    {
        il.MarkLabel(GetOrCreateRegionEntryLabel(regionNode));
        if (_regions.TryGetValue(regionNode, out var region))
        {
            EmitRegionDispatch(il, region);
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

        // --- 生成函数体 ---
        // 状态派发放在外层 try 区域内部：直接跳到方法体里的恢复标签，
        // 若恢复标签在用户 try 里则先跳到该 try 的首指令，由其入口派发链接手。
        // 这样任何分支都不会从 try 外部跳进区域中间（JIT 会判为无效 IL）。
        EmitRegionDispatch(il, _rootRegion);

        LocalManager.AsyncStateMachineGenerator = this;
        ExecutionModeDispatcher.GenerateIl(BlockStatement, il, LocalManager);
        
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

        // 扫描阶段结束后不应再出现新的 await：那说明分析端漏掉了某个表达式节点，
        // 该状态不会出现在任何派发链里，恢复时会从函数体开头重跑（表现为重复求值或挂死）。
        if (_analysisComplete)
        {
            throw new CompilerException(
                "await 位于异步状态机无法恢复的表达式位置（分析阶段未登记该节点）",
                expr.Position);
        }

        int newIndex = AwaitStateMap.Count;
        AwaitStateMap[expr] = newIndex;
        return newIndex;
    }

    
    /// <summary>
    /// 在用户 finally 体之前发射「挂起中不执行用户 finally」的守卫。
    /// </summary>
    /// <remarks>
    /// 负状态（-1 未开始 / -2 已完成）表示这次离开受保护区域是正常完成或异常传播，
    /// 应当执行用户 finally；非负状态表示是在某个 await 处挂起返回，
    /// 此时 CLR 也会因为 <c>leave</c> 触发 finally，必须跳过。
    /// </remarks>
    public void EmitSuspendedFinallyGuard(ILGenerator il, Label skipLabel)
    {
        if (StateField == null) throw new InvalidOperationException("State field not initialized");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, StateField);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Bge, skipLabel);
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
