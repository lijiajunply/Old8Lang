using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - LINQ表达式
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitLinqExpression(LinqExpression node)
    {
        MarkPosition(node);
        // 数据源在外层作用域求值；查询范围变量在独立作用域中遮蔽同名变量。
        node.FromClause.DataSource.Accept(this);
        var sourceName = $"<linq_source_{Guid.NewGuid():N}>";
        var sourceLocal = _compiler.AllocateLocal(sourceName);
        Emit(OpCode.StoreLocal, sourceLocal);
        _compiler.EnterScope();
        try
        {
            var scoped = new LinqExpression(
                new FromClause(node.FromClause.RangeVariable, new LangId(sourceName)),
                node.BodyClauses, node.TerminationClause, node.Continuation, node.Position);
            return EmitLinqExpression(scoped);
        }
        finally
        {
            _compiler.LeaveScope();
            _compiler.FreeLocal(sourceLocal);
        }
    }

    private Instruction? EmitLinqExpression(LinqExpression node)
    {
        // 检查是否有 Join 子句
        var hasJoin = node.BodyClauses.Any(c => c is JoinClause);

        if (hasJoin)
        {
            return VisitLinqExpressionWithJoin(node);
        }

        // LINQ 查询表达式的字节码生成策略:
        // 1. 获取数据源并转换为迭代器
        // 2. 遍历数据源,对每个元素应用查询子句
        // 3. 收集结果到列表中
        // 4. 返回结果列表

        // 步骤1: 计算数据源表达式
        node.FromClause.DataSource.Accept(this);

        // 获取迭代器
        Emit(OpCode.GetIterator);
        int iteratorLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, iteratorLocal);

        // 检查终止子句类型和 OrderBy 子句
        bool isGroupBy = node.TerminationClause is GroupByClause;
        var orderByClause = node.BodyClauses.OfType<OrderByClause>().LastOrDefault();

        if (isGroupBy && orderByClause == null)
        {
            // 创建分组字典 (用于无排序的 GroupBy)
            Emit(OpCode.NewGroupDict);
        }
        else
        {
            // 创建结果列表 (用于 Select 或带 OrderBy 的排序行收集)
            Emit(OpCode.NewList, 0);
        }

        int resultListLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, resultListLocal);

        // 为范围变量分配局部变量槽
        int rangeVarLocal = _compiler.AllocateLocal(node.FromClause.RangeVariable);

        // 为 let 变量分配局部变量槽
        var letVariables = new Dictionary<string, int>();
        foreach (var clause in node.BodyClauses)
        {
            if (clause is LetClause letClause)
            {
                int letVarLocal = _compiler.AllocateLocal(letClause.Variable);
                letVariables[letClause.Variable] = letVarLocal;
            }
        }

        // 步骤2: 遍历数据源
        int loopStartPos = GetCurrentPosition();

        // 检查迭代器是否有下一个元素
        Emit(OpCode.LoadLocal, iteratorLocal);
        Emit(OpCode.IteratorMoveNext);

        // 如果没有下一个元素,跳出循环
        int loopEndJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1); // 占位,稍后修补

        // 获取当前元素
        Emit(OpCode.LoadLocal, iteratorLocal);
        Emit(OpCode.IteratorCurrent);

        // 将当前元素存储到范围变量
        Emit(OpCode.StoreLocal, rangeVarLocal);

        // 步骤3: 处理查询体子句 (where, let)
        var skipElementJumps = new List<int>();
        ProcessLinqBodyClauses(node.BodyClauses, letVariables, skipElementJumps);

        // 步骤4: 执行终止子句 (select / group)
        if (orderByClause != null)
        {
            ProcessLinqOrderedTerminationClause(node.TerminationClause, orderByClause, resultListLocal);
        }
        else
        {
            ProcessLinqTerminationClause(node.TerminationClause, resultListLocal);
        }

        // 跳回循环开始
        int continueJump = GetCurrentPosition();
        Emit(OpCode.Jump, loopStartPos);

        // 修补所有跳过元素的跳转 (where 条件不满足时跳到这里)
        foreach (var jumpIndex in skipElementJumps)
        {
            PatchJump(jumpIndex, continueJump);
        }

        // 修补循环结束跳转
        PatchJump(loopEndJump, GetCurrentPosition());

        // 步骤5: 处理 OrderBy / GroupBy 终结转换
        if (orderByClause != null)
        {
            ProcessLinqOrderBy(orderByClause, isGroupBy, resultListLocal);
        }
        else if (isGroupBy)
        {
            Emit(OpCode.LoadLocal, resultListLocal);
            Emit(OpCode.GroupDictToList);
            Emit(OpCode.StoreLocal, resultListLocal);
        }

        // 步骤7: 加载结果列表到栈
        Emit(OpCode.LoadLocal, resultListLocal);

        // 释放局部变量
        _compiler.FreeLocal(iteratorLocal);
        _compiler.FreeLocal(resultListLocal);
        _compiler.FreeLocal(rangeVarLocal);
        foreach (var letVar in letVariables.Values)
        {
            _compiler.FreeLocal(letVar);
        }

        EmitLinqContinuation(node);

        return null;
    }

    private void EmitLinqContinuation(LinqExpression node)
    {
        if (node.Continuation is not { } continuation) return;
        // 栈顶为上一阶段结果；将其作为新查询的数据源逐元素处理。
        var temporaryName = $"<linq_continuation_{Guid.NewGuid():N}>";
        var temporary = _compiler.AllocateLocal(temporaryName);
        Emit(OpCode.StoreLocal, temporary);
        var next = new LinqExpression(
            new FromClause(continuation.Variable, new LangId(temporaryName)),
            continuation.BodyClauses, continuation.TerminationClause, continuation.Continuation);
        VisitLinqExpression(next);
        _compiler.FreeLocal(temporary);
    }

    /// <summary>
    /// 处理包含 Join 子句的 LINQ 表达式（支持多层嵌套连接、组连接、where/let 顺序执行、OrderBy 与 GroupBy）
    /// </summary>
    private Instruction? VisitLinqExpressionWithJoin(LinqExpression node)
    {
        bool isGroupBy = node.TerminationClause is GroupByClause;
        var orderByClause = node.BodyClauses.OfType<OrderByClause>().LastOrDefault();

        if (isGroupBy && orderByClause == null)
        {
            Emit(OpCode.NewGroupDict);
        }
        else
        {
            Emit(OpCode.NewList, 0);
        }
        int resultListLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, resultListLocal);

        // 为外部范围变量分配局部变量槽
        int outerRangeVarLocal = _compiler.AllocateLocal(node.FromClause.RangeVariable);

        // 为 let 变量分配局部变量槽
        var letVariables = new Dictionary<string, int>();
        foreach (var clause in node.BodyClauses)
        {
            if (clause is LetClause letClause && !letVariables.ContainsKey(letClause.Variable))
            {
                letVariables[letClause.Variable] = _compiler.AllocateLocal(letClause.Variable);
            }
        }

        // 获取外部数据源的迭代器
        node.FromClause.DataSource.Accept(this);
        Emit(OpCode.GetIterator);
        int outerIteratorLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, outerIteratorLocal);

        // 外部循环开始
        int outerLoopStartPos = GetCurrentPosition();

        // 检查外部迭代器是否有下一个元素
        Emit(OpCode.LoadLocal, outerIteratorLocal);
        Emit(OpCode.IteratorMoveNext);

        // 如果没有下一个元素,跳出外部循环
        int outerLoopEndJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 获取外部当前元素
        Emit(OpCode.LoadLocal, outerIteratorLocal);
        Emit(OpCode.IteratorCurrent);
        Emit(OpCode.StoreLocal, outerRangeVarLocal);

        // 按子句声明顺序递归生成连接、过滤、let 与终止子句
        var outerSkipJumps = new List<int>();
        EmitJoinBodyClauses(node, 0, letVariables, orderByClause, resultListLocal, outerSkipJumps);

        int outerContinuePos = GetCurrentPosition();
        foreach (var skipJump in outerSkipJumps)
        {
            PatchJump(skipJump, outerContinuePos);
        }

        // 跳回外部循环开始
        Emit(OpCode.Jump, outerLoopStartPos);

        // 修补外部循环结束跳转
        PatchJump(outerLoopEndJump, GetCurrentPosition());

        // 处理 OrderBy / GroupBy 终结转换
        if (orderByClause != null)
        {
            ProcessLinqOrderBy(orderByClause, isGroupBy, resultListLocal);
        }
        else if (isGroupBy)
        {
            Emit(OpCode.LoadLocal, resultListLocal);
            Emit(OpCode.GroupDictToList);
            Emit(OpCode.StoreLocal, resultListLocal);
        }

        // 加载结果列表到栈
        Emit(OpCode.LoadLocal, resultListLocal);

        // 释放局部变量
        _compiler.FreeLocal(resultListLocal);
        _compiler.FreeLocal(outerRangeVarLocal);
        _compiler.FreeLocal(outerIteratorLocal);
        foreach (var local in letVariables.Values)
        {
            _compiler.FreeLocal(local);
        }

        EmitLinqContinuation(node);

        return null;
    }

    private void EmitJoinBodyClauses(
        LinqExpression node,
        int clauseIndex,
        Dictionary<string, int> letVariables,
        OrderByClause? orderByClause,
        int resultListLocal,
        List<int> skipJumps)
    {
        if (clauseIndex >= node.BodyClauses.Count)
        {
            if (orderByClause != null)
            {
                ProcessLinqOrderedTerminationClause(node.TerminationClause, orderByClause, resultListLocal);
            }
            else
            {
                ProcessLinqTerminationClause(node.TerminationClause, resultListLocal);
            }
            return;
        }

        var clause = node.BodyClauses[clauseIndex];
        if (clause is WhereClause whereClause)
        {
            whereClause.Condition.Accept(this);
            int skipJump = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);
            skipJumps.Add(skipJump);
            EmitJoinBodyClauses(node, clauseIndex + 1, letVariables, orderByClause, resultListLocal, skipJumps);
        }
        else if (clause is LetClause letClause)
        {
            letClause.Expression.Accept(this);
            Emit(OpCode.StoreLocal, letVariables[letClause.Variable]);
            EmitJoinBodyClauses(node, clauseIndex + 1, letVariables, orderByClause, resultListLocal, skipJumps);
        }
        else if (clause is OrderByClause)
        {
            // OrderBy 键在终止阶段按每行收集，循环结束后统一稳定排序
            EmitJoinBodyClauses(node, clauseIndex + 1, letVariables, orderByClause, resultListLocal, skipJumps);
        }
        else if (clause is JoinClause joinClause)
        {
            // 在声明内层变量之前求值数据源，允许 `join y in y` 引用外层 y。
            joinClause.InnerDataSource.Accept(this);
            Emit(OpCode.GetIterator);
            int innerIteratorLocal = _compiler.AllocateLocal();
            Emit(OpCode.StoreLocal, innerIteratorLocal);

            // 计算外部键（每个外层元素只求值一次）
            joinClause.OuterKeyExpression.Accept(this);
            int outerKeyLocal = _compiler.AllocateLocal();
            Emit(OpCode.StoreLocal, outerKeyLocal);

            int groupListLocal = -1;
            if (joinClause.IsGroupJoin && joinClause.GroupVariable != null)
            {
                Emit(OpCode.NewList, 0);
                groupListLocal = _compiler.AllocateLocal(joinClause.GroupVariable);
                Emit(OpCode.StoreLocal, groupListLocal);
            }

            int innerRangeVarLocal = _compiler.AllocateLocal(joinClause.RangeVariable);
            int innerKeyLocal = _compiler.AllocateLocal();

            int innerLoopStartPos = GetCurrentPosition();
            Emit(OpCode.LoadLocal, innerIteratorLocal);
            Emit(OpCode.IteratorMoveNext);
            int innerLoopEndJump = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);

            Emit(OpCode.LoadLocal, innerIteratorLocal);
            Emit(OpCode.IteratorCurrent);
            Emit(OpCode.StoreLocal, innerRangeVarLocal);

            joinClause.InnerKeyExpression.Accept(this);
            Emit(OpCode.StoreLocal, innerKeyLocal);

            Emit(OpCode.LoadLocal, outerKeyLocal);
            Emit(OpCode.LoadLocal, innerKeyLocal);
            Emit(OpCode.Equal);
            int skipInnerJump = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);

            if (joinClause.IsGroupJoin && joinClause.GroupVariable != null)
            {
                Emit(OpCode.LoadLocal, groupListLocal);
                Emit(OpCode.LoadLocal, innerRangeVarLocal);
                Emit(OpCode.CallMethod, new object[] { 2, "Add" });
                Emit(OpCode.Pop);

                PatchJump(skipInnerJump, innerLoopStartPos);
                Emit(OpCode.Jump, innerLoopStartPos);
                PatchJump(innerLoopEndJump, GetCurrentPosition());

                _compiler.FreeLocal(innerKeyLocal);
                _compiler.FreeLocal(innerRangeVarLocal);
                _compiler.FreeLocal(outerKeyLocal);
                _compiler.FreeLocal(innerIteratorLocal);

                // Group Join 收集完分组后继续后续子句
                EmitJoinBodyClauses(node, clauseIndex + 1, letVariables, orderByClause, resultListLocal, skipJumps);
                _compiler.FreeLocal(groupListLocal);
            }
            else
            {
                var innerSkipJumps = new List<int>();
                EmitJoinBodyClauses(node, clauseIndex + 1, letVariables, orderByClause, resultListLocal, innerSkipJumps);

                int continueInnerPos = GetCurrentPosition();
                foreach (var sj in innerSkipJumps)
                {
                    PatchJump(sj, continueInnerPos);
                }
                PatchJump(skipInnerJump, continueInnerPos);

                Emit(OpCode.Jump, innerLoopStartPos);
                PatchJump(innerLoopEndJump, GetCurrentPosition());

                _compiler.FreeLocal(innerKeyLocal);
                _compiler.FreeLocal(innerRangeVarLocal);
                _compiler.FreeLocal(outerKeyLocal);
                _compiler.FreeLocal(innerIteratorLocal);
            }
        }
        else
        {
            EmitJoinBodyClauses(node, clauseIndex + 1, letVariables, orderByClause, resultListLocal, skipJumps);
        }
    }

    // ===== 泛型处理辅助方法 =====

    /// <summary>
    /// 处理泛型类实例化
    /// </summary>
    private void HandleGenericClassInstantiation(GenericInstanceExpression node, string className)
    {
        // 获取泛型类模板
        var typeTemplate = _compiler.GenericClasses[className];

        // 构建特化类名：ClassName$Type1_Type2_...
        // 解析类型参数时，需要考虑当前的类型参数映射
        var typeArgNames = node.TypeArguments.Select(typeArg =>
        {
            var resolvedType = ResolveSimpleTypeName(typeArg);
            // 如果类型参数在当前映射中，替换为实际类型
            return _compiler.CurrentTypeParameterMapping.GetValueOrDefault(resolvedType, resolvedType);
        }).ToArray();
        var specializedClassName = $"{className}${string.Join("_", typeArgNames)}";

        // 检查是否已经生成过特化类
        if (!_compiler.IsClassName(specializedClassName))
        {
            // 生成特化类定义
            GenerateSpecializedClass(typeTemplate, typeArgNames.ToList(), specializedClassName);
        }

        // 生成创建对象的字节码
        Emit(OpCode.NewObject, specializedClassName);

        // 查找并调用构造函数
        var classMetadata = _compiler.GetClassMetadata(specializedClassName);
        string? constructorName = null;

        if (classMetadata != null)
        {
            // 优先查找 init 方法
            if (classMetadata.Methods.Any(m => m.Name == "init"))
            {
                constructorName = "init";
            }
            // 其次查找与原始类名相同的方法
            else if (classMetadata.Methods.Any(m => m.Name == className))
            {
                constructorName = className;
            }
        }

        // 如果找到构造函数，调用它
        if (constructorName != null && node.CallArguments != null)
        {
            // 复制对象引用，因为 CallMethod 会消耗它
            Emit(OpCode.Dup);

            // 生成参数代码
            foreach (var arg in node.CallArguments)
            {
                arg.Accept(this);
            }

            // 调用构造函数
            // CallMethod 操作数: [argCount, methodName]
            // argCount 包括对象本身 + 实际参数
            int totalArgCount = node.CallArguments.Count + 1; // +1 for 'this'
            Emit(OpCode.CallMethod, new object[] { totalArgCount, constructorName });


            // 实例已经留在栈上（NewObject 创建 + Dup 保留引用），
            // 构造函数自身的返回值（无返回值时是一个 VoidLangValue 占位）必须丢弃
            Emit(OpCode.Pop);
        }
    }

    /// <summary>
    /// 处理泛型函数调用
    /// </summary>
    private void HandleGenericFunctionCall(GenericInstanceExpression node, string funcName)
    {
        // 获取泛型函数定义
        var genericFunc = _compiler.GenericFunctions[funcName];

        // 构建特化函数名：FuncName$Type1_Type2_...
        var typeArgNames = node.TypeArguments.Select(ResolveSimpleTypeName).ToArray();
        var specializedFuncName = $"{funcName}${string.Join("_", typeArgNames)}";

        // 检查是否已经生成过特化函数
        if (_compiler.GetFunctionIndex(specializedFuncName) == -1)
        {
            // 生成特化函数定义
            GenerateSpecializedFunction(genericFunc, node.TypeArguments, specializedFuncName);
        }

        // 生成调用参数的字节码
        if (node.CallArguments != null)
        {
            foreach (var arg in node.CallArguments)
            {
                arg.Accept(this);
            }
        }

        // 生成函数调用字节码
        int argCount = node.CallArguments?.Count ?? 0;
        Emit(OpCode.Call, new object[] { argCount, specializedFuncName });
    }
}
