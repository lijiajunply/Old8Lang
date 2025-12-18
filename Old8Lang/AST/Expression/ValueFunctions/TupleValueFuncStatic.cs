using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// TupleLangValue类型的扩展方法类，提供元组操作功能
/// </summary>
[Serializable]
public static class TupleValueFuncStatic
{
    extension(TupleLangValue tuple)
    {
        /// <summary>
        /// 将元组中的所有元素连接成字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>连接后的字符串</returns>
        public StringLangValue Join(StringLangValue separator)
        {
            var elements = GetAllElements(tuple);
            var separatorValue = separator?.Value ?? "";

            var result = string.Join(separatorValue, elements.Select(e => e.ToDisplayString()));
            return new StringLangValue(result);
        }

        /// <summary>
        /// 获取元组中的所有元素（包括嵌套元组的元素）
        /// </summary>
        /// <param name="sourceTuple">元组</param>
        /// <returns>所有元素的列表</returns>
        private static List<LangValueType> GetAllElements(TupleLangValue sourceTuple)
        {
            var elements = new List<LangValueType>();
            CollectElements(sourceTuple, elements);
            return elements;
        }

        /// <summary>
        /// 递归收集元组中的所有元素
        /// </summary>
        /// <param name="current">当前元组或元素</param>
        /// <param name="elements">元素列表</param>
        private static void CollectElements(LangValueType current, List<LangValueType> elements)
        {
            if (current is TupleLangValue currentTuple)
            {
                // 如果是元组，递归收集其元素
                CollectElements(currentTuple.Value.Item1, elements);
                CollectElements(currentTuple.Value.Item2, elements);
            }
            else
            {
                // 如果是普通元素，添加到列表
                elements.Add(current);
            }
        }
    }
}