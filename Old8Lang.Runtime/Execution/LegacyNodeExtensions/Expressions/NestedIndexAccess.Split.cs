using System.Reflection.Emit;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// NestedIndexAccess 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class NestedIndexAccess
{
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
