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

    /// <summary>
    /// 在 IL 中加载 ValueTuple 指定索引位置的字段
    /// 处理超过 7 个元素的情况（递归访问 Rest 字段）
    /// </summary>
    internal void LoadValueTupleField(ILGenerator ilGenerator, Type tupleType, int index)
    {
        if (index < 0)
        {
            throw new InvalidOperationError(this, $"元组索引不能为负数: {index}");
        }

        // 如果索引在前 7 个元素范围内
        if (index < 7)
        {
            var fieldName = GetValueTupleFieldName(index);
            var fieldInfo = tupleType.GetField(fieldName);
            if (fieldInfo != null)
            {
                ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
                return;
            }

            throw new InvalidOperationError(this, $"找不到元组字段: {fieldName}");
        }

        // 如果索引超过 7，需要递归访问 Rest 字段
        var restField = tupleType.GetField("Rest");
        if (restField == null)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}，当前元组类型: {tupleType.Name}");
        }

        // 加载 Rest 字段
        ilGenerator.Emit(OpCodes.Ldfld, restField);

        // 递归加载 Rest 中的字段
        var restType = restField.FieldType;
        if (restType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            LoadValueTupleField(ilGenerator, restType, index - 7);
        }
        else
        {
            throw new InvalidOperationError(this, $"Rest 字段类型不是 ValueTuple: {restType.Name}");
        }
    }
}