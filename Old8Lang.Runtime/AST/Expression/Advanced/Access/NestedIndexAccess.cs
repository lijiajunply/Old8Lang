using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 嵌套索引访问表达式，用于处理 array[index1][index2] 的情况
/// </summary>
/// <param name="baseExpression">基础表达式，可以是 LangListItem 或 NestedIndexAccess</param>
/// <param name="nestedIndex">嵌套索引，如 [index2]</param>
/// <param name="position">源代码位置</param>
public partial class NestedIndexAccess(
    LangExpression baseExpression,
    LangExpression nestedIndex,
    SourcePosition position = default)
    : LangValueType(position)
{

    public readonly LangExpression BaseExpression = baseExpression;
    public readonly LangExpression NestedIndex = nestedIndex;

    public override string ToString() => $"{BaseExpression}[{NestedIndex}]";

    /// <summary>
    /// 获取 ValueTuple 指定索引位置的元素类型
    /// </summary>
    internal Type? GetValueTupleElementType(Type tupleType, int index)
    {
        if (index < 0) return null;

        var genericArgs = tupleType.GetGenericArguments();

        // 如果索引在前 7 个元素范围内
        if (index < 7 && index < genericArgs.Length)
        {
            return genericArgs[index];
        }

        // 如果索引超过 7，需要递归查找 Rest 元素
        if (genericArgs.Length == 8)
        {
            var restType = genericArgs[7];
            if (restType.FullName?.StartsWith("System.ValueTuple") == true)
            {
                return GetValueTupleElementType(restType, index - 7);
            }
        }

        return null;
    }

    /// <summary>
    /// 获取 ValueTuple 字段名称
    /// </summary>
    internal string GetValueTupleFieldName(int index)
    {
        return $"Item{index + 1}";
    }

}