using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// ListComprehension 的解释执行辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static class ListComprehensionRuntimeExtensions
{
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>条件是否满足</returns>
    internal static bool CheckCondition(this ListComprehension node, VariateManager manager)
    {
        // 如果没有条件，直接返回true
        if (node.Condition is null)
        {
            return true;
        }

        // 执行条件表达式
        var conditionValue = node.Condition.Accept(RuntimeVisitors.CreateInterpreter(manager));

        // 检查条件结果是否为布尔值
        if (conditionValue is BoolLangValue boolValue)
        {
            return boolValue.Value;
        }

        // 如果条件结果不是布尔值，抛出错误
        throw new TypeError(
            node.Condition,
            $"类型不匹配: 期望布尔表达式，但得到 {conditionValue.GetType().Name}");
    }
    /// <summary>
    /// 处理嵌套的for循环
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="loops">嵌套的for循环列表</param>
    /// <returns>生成的元素列表</returns>
    internal static List<LangValueType> ProcessNestedLoops(this ListComprehension node, VariateManager manager, List<ListComprehension> loops)
    {
        var resultList = new List<LangValueType>();

        // 获取当前循环
        var currentLoop = loops[0];

        // 执行当前循环的可迭代对象
        var iterableValue = currentLoop.Iterable.Accept(RuntimeVisitors.CreateInterpreter(manager));

        // 获取可迭代对象的元素
        IEnumerable<LangValueType> items;

        if (iterableValue is ILangList list)
        {
            items = list.GetItems();
        }
        else if (iterableValue is ArrayLangValue array)
        {
            items = array.GetItems();
        }
        else if (iterableValue is ListLangValue listValue)
        {
            items = listValue.GetItems();
        }
        else if (iterableValue is RangeLangValue range)
        {
            // 处理范围类型，执行range表达式获取实际的数组
            var rangeResult = range.Accept(RuntimeVisitors.CreateInterpreter(manager));
            if (rangeResult is ArrayLangValue rangeArray)
            {
                items = rangeArray.GetItems();
            }
            else
            {
                throw new TypeError(
                    currentLoop.Iterable,
                    "范围表达式必须返回数组类型");
            }
        }
        else if (iterableValue is StringLangValue str)
        {
            // 处理字符串，生成字符列表
            items = str.Value
                .Select(c => new CharLangValue(c) as LangValueType);
        }
        else
        {
            throw new TypeError(
                currentLoop.Iterable,
                $"类型不匹配: 列表推导式的可迭代对象必须是数组、列表、范围或字符串，但得到 {iterableValue.GetType().Name}");
        }

        // 遍历可迭代对象的每个元素
        foreach (var item in items)
        {
            // 创建一个全新的变量管理器，复制当前的变量状态
            var newManager = manager.NewManger();

            // 设置当前变量的值
            newManager.Set(currentLoop.Variable, item);

            // 如果还有更多嵌套循环，递归处理
            if (loops.Count > 1)
            {
                var nestedResults = node.ProcessNestedLoops(newManager, loops.Skip(1).ToList());
                resultList.AddRange(nestedResults);
            }
            else
            {
                // 检查所有条件，包括当前循环和所有外层循环的条件
                var allConditionsMet = currentLoop.CheckCondition(newManager);

                // 检查当前循环的条件

                // 检查所有外层循环的条件（如果有）
                ListComprehension? outerLoop = node;
                while (outerLoop is not null)
                {
                    if (!outerLoop.CheckCondition(newManager))
                    {
                        allConditionsMet = false;
                        break;
                    }

                    outerLoop = null; // 跳出循环，因为this是最外层
                }

                // 所有条件都满足，计算表达式值并添加到结果列表
                if (allConditionsMet)
                {
                    // 使用最外层的表达式，而不是当前循环的表达式
                    var exprValue = node.Expression.Accept(RuntimeVisitors.CreateInterpreter(newManager));
                    resultList.Add(exprValue);
                }
            }
        }

        return resultList;
    }
}
