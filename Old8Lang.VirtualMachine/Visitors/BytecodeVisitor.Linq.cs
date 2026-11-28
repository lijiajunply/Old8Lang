using Old8Lang.AST.Expression.Linq;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - LINQ 相关的辅助方法
/// </summary>
public partial class BytecodeVisitor
{
    /// <summary>
    /// 处理查询体子句 (where, let)
    /// </summary>
    /// <param name="bodyClauses">查询体子句列表</param>
    /// <param name="letVariables">let 变量的局部变量索引映射</param>
    /// <param name="skipElementJumps">跳过元素的跳转位置列表</param>
    private void ProcessLinqBodyClauses(
        List<LinqClause> bodyClauses,
        Dictionary<string, int> letVariables,
        List<int> skipElementJumps)
    {
        foreach (var clause in bodyClauses)
        {
            if (clause is WhereClause whereClause)
            {
                // 处理 where 子句
                // 计算条件表达式
                whereClause.Condition.Accept(this);

                // 如果条件为 false,跳过当前元素
                int skipJump = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1); // 占位,稍后修补
                skipElementJumps.Add(skipJump);
            }
            else if (clause is LetClause letClause)
            {
                // 处理 let 子句
                // 计算 let 表达式
                letClause.Expression.Accept(this);

                // 存储到已分配的局部变量
                int letVarLocal = letVariables[letClause.Variable];
                Emit(OpCode.StoreLocal, letVarLocal);
            }
            else if (clause is OrderByClause)
            {
                // OrderBy 需要在收集所有元素后统一处理
                // 这里不做处理
            }
        }
    }

    /// <summary>
    /// 处理终止子句 (select 或 group)
    /// </summary>
    private void ProcessLinqTerminationClause(
        LinqClause terminationClause,
        int resultListLocal)
    {
        if (terminationClause is SelectClause selectClause)
        {
            // 计算投影表达式
            selectClause.Projection.Accept(this);

            // 将结果添加到结果列表
            // 栈: [projectedValue]

            // 使用临时变量来调整栈顺序
            int valueLocal = _compiler.AllocateLocal();
            Emit(OpCode.StoreLocal, valueLocal); // 存储 projectedValue
            // 栈: []

            Emit(OpCode.LoadLocal, resultListLocal); // 加载 resultList
            Emit(OpCode.LoadLocal, valueLocal); // 加载 projectedValue
            // 栈: [resultList, projectedValue]

            // 调用 List.Add 方法
            Emit(OpCode.CallMethod, new object[] { 2, "Add" });

            // 弹出返回值 (Add 方法返回 void,但为了安全起见)
            Emit(OpCode.Pop);

            // 释放临时变量
            _compiler.FreeLocal(valueLocal);
        }
        else if (terminationClause is GroupByClause groupByClause)
        {
            // GroupBy 的实现策略:
            // 1. 创建一个分组字典 (Dictionary<key, List<element>>)
            // 2. 对于每个元素:
            //    - 计算分组键 (KeyExpression)
            //    - 计算分组元素 (ElementExpression)
            //    - 将元素添加到对应键的分组中
            // 3. 将分组字典转换为分组列表

            // 注意: 这个方法在循环内部被调用,每次处理一个元素

            // 加载分组字典
            Emit(OpCode.LoadLocal, resultListLocal);
            // 栈: [groupDict]

            // 计算分组键表达式
            groupByClause.KeyExpression.Accept(this);
            // 栈: [groupDict, key]

            // 计算分组元素表达式
            groupByClause.ElementExpression.Accept(this);
            // 栈: [groupDict, key, element]

            // 添加元素到分组 (会从栈中弹出 element, key, groupDict)
            Emit(OpCode.AddToGroup);
            // 栈: []
        }
    }

    /// <summary>
    /// 处理带 OrderBy 的终止子句：收集 [值..., key0, key1, ...] 行到列表，循环结束后统一按键稳定排序。
    /// </summary>
    private void ProcessLinqOrderedTerminationClause(
        LinqClause terminationClause,
        OrderByClause orderByClause,
        int sortRowsLocal)
    {
        int payloadCount;
        if (terminationClause is SelectClause selectClause)
        {
            selectClause.Projection.Accept(this);
            payloadCount = 1;
        }
        else if (terminationClause is GroupByClause groupByClause)
        {
            groupByClause.KeyExpression.Accept(this);
            groupByClause.ElementExpression.Accept(this);
            payloadCount = 2;
        }
        else
        {
            return;
        }

        foreach (var ordering in orderByClause.Orderings)
        {
            ordering.KeyExpression.Accept(this);
        }

        Emit(OpCode.NewArray, payloadCount + orderByClause.Orderings.Count);
        int rowLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, rowLocal);

        Emit(OpCode.LoadLocal, sortRowsLocal);
        Emit(OpCode.LoadLocal, rowLocal);
        Emit(OpCode.CallMethod, new object[] { 2, "Add" });
        Emit(OpCode.Pop);

        _compiler.FreeLocal(rowLocal);
    }

    /// <summary>
    /// 处理 OrderBy 子句：按键对已收集的排序行稳定排序，并还原为最终结果列表（或分组列表）。
    /// </summary>
    private void ProcessLinqOrderBy(
        OrderByClause orderByClause,
        bool isGroupBy,
        int resultListLocal)
    {
        Emit(OpCode.LoadLocal, resultListLocal);
        foreach (var ordering in orderByClause.Orderings)
        {
            Emit(OpCode.LoadConst, _compiler.AddConstant(ordering.IsAscending));
        }
        Emit(OpCode.NewArray, orderByClause.Orderings.Count);
        Emit(OpCode.CallNative, new object[] { 2, isGroupBy ? "LinqGroupSortedEntries" : "LinqSortByKeys" });
        Emit(OpCode.StoreLocal, resultListLocal);
    }
}
