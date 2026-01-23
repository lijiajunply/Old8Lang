using Old8Lang.AST.Expression.Linq;
using Old8Lang.Bytecode;

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
    /// 处理 OrderBy 子句
    /// </summary>
    private void ProcessLinqOrderBy(
        OrderByClause orderByClause,
        int resultListLocal)
    {
        // OrderBy 的实现策略:
        // 调用原生排序函数对结果列表进行排序
        // 由于字节码模式下难以传递 lambda 表达式,我们使用简化实现:
        // 假设排序键是简单的字段访问或表达式

        // 加载结果列表
        Emit(OpCode.LoadLocal, resultListLocal);

        // 对于每个排序键,生成排序调用
        for (int i = 0; i < orderByClause.Orderings.Count; i++)
        {
            var ordering = orderByClause.Orderings[i];

            // 加载排序方向 (true = ascending, false = descending)
            Emit(OpCode.LoadConst, _compiler.AddConstant(ordering.IsAscending));

            // 调用原生排序函数
            // 参数: list, isAscending
            // 注意: 这需要在虚拟机中实现相应的原生函数
            if (i == 0)
            {
                Emit(OpCode.CallNative, new object[] { 2, "LinqOrderBy" });
            }
            else
            {
                Emit(OpCode.CallNative, new object[] { 2, "LinqThenBy" });
            }
        }

        // 将排序后的列表存回
        Emit(OpCode.StoreLocal, resultListLocal);
    }
}
