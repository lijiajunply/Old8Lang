using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
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

    public override LangValueType Run(VariateManager manager)
    {
        // 首先运行基础表达式，获取结果
        var baseResult = BaseExpression.Run(manager);

        // 检查基础结果是否支持索引访问
        if (baseResult is ListLangValue list)
        {
            if (NestedIndex.Run(manager) is not IntLangValue intResult)
                throw new TypeError(this, "IntValue", NestedIndex.Run(manager).GetType().Name);
            return list.Get(intResult);
        }

        if (baseResult is ArrayLangValue array)
        {
            if (NestedIndex.Run(manager) is not IntLangValue intResult)
                throw new TypeError(this, "IntValue", NestedIndex.Run(manager).GetType().Name);
            return array.Get(intResult);
        }

        if (baseResult is DictionaryLangValue dict)
        {
            if (NestedIndex.Run(manager) is not { } keyResult)
                throw new TypeError(this, "ValueType", NestedIndex.Run(manager).GetType().Name);
            return dict.Get(keyResult);
        }

        if (baseResult is StringLangValue str)
        {
            if (NestedIndex.Run(manager) is not IntLangValue intResult)
                throw new TypeError(this, "IntValue", NestedIndex.Run(manager).GetType().Name);
            return str.Get(intResult);
        }

        if (baseResult is TupleLangValue tuple)
        {
            return NestedIndex.Run(manager) is not IntLangValue intResult
                ? throw new TypeError(this, "IntValue", NestedIndex.Run(manager).GetType().Name)
                : tuple.Get(intResult);
        }

        throw new InvalidOperationError(this, $"不支持的嵌套索引访问类型: {baseResult.GetType().Name}");
    }

    public override string ToString() => $"{BaseExpression}[{NestedIndex}]";

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 根据基础表达式的输出类型选择适当的索引访问方法
        var baseType = BaseExpression.OutputType(local);

        // 特殊处理 ValueTuple 的常量索引访问
        if (baseType.FullName?.StartsWith("System.ValueTuple") == true && NestedIndex is IntLangValue intValue)
        {
            var index = intValue.Value;

            // 只加载基础表达式的结果，不加载索引值
            BaseExpression.LoadIlValue(ilGenerator, local);

            // 使用递归方法加载字段（支持超过 7 个元素）
            LoadValueTupleField(ilGenerator, baseType, index);
            return;
        }

        // 对于其他情况，先加载基础表达式的结果和索引值
        BaseExpression.LoadIlValue(ilGenerator, local);
        NestedIndex.LoadIlValue(ilGenerator, local);

        if (baseType == typeof(string))
        {
            // 字符串索引访问
            var getCharMethod = typeof(string).GetMethod("get_Chars", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getCharMethod);
        }
        else if (baseType.IsArray)
        {
            // 数组索引访问
            var elementType = baseType.GetElementType()!;
            if (elementType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldelem, elementType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
            }
        }
        else if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // List<T> 索引访问
            var getItemMethod = baseType.GetMethod("get_Item", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        }
        else if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            // Dictionary<TKey, TValue> 索引访问
            var keyType = baseType.GetGenericArguments()[0];
            var getItemMethod = baseType.GetMethod("get_Item", [keyType])!;
            ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        }
        else if (baseType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问（非常量索引）
            // 使用 ITuple 索引器
            var indexType = NestedIndex.OutputType(local);
            if (indexType == typeof(object))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            }

            var indexLocal = ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.Emit(OpCodes.Stloc, indexLocal.LocalIndex);

            // Box Tuple
            ilGenerator.Emit(OpCodes.Box, baseType);

            // Load Index
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);

            var indexer = typeof(System.Runtime.CompilerServices.ITuple).GetProperty("Item")!;
            ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
        }
        else if (baseType == typeof(object))
        {
            // 动态索引访问
            // 调用 DotOperatorILHelper.GenerateDynamicIndexAccess
            // 该方法假设栈上有 [Left] [Right]，并生成动态分发代码
            // 我们的栈上正好有 BaseIndex 和 NestedIndex 的值

            var indexType = NestedIndex.OutputType(local);

            if (indexType != null)
            {
                OperationHelpers.DotOperatorILHelper.GenerateDynamicIndexAccess(
                    ilGenerator,
                    indexType
                );
            }
        }
        else
        {
            throw new InvalidOperationError(this, "不支持的嵌套索引访问类型: " + baseType.Name);
        }
    }

    public override Type OutputType(LocalManager local)
    {
        var baseType = BaseExpression.OutputType(local);

        if (baseType == typeof(string))
        {
            return typeof(char);
        }

        if (baseType.IsArray)
        {
            return baseType.GetElementType() ?? typeof(object);
        }

        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return baseType.GetGenericArguments()[0];
        }

        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return baseType.GetGenericArguments()[1];
        }

        // 处理 ValueTuple 类型
        if (baseType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // 如果索引是常量，我们可以推断出具体的元素类型
            if (NestedIndex is IntLangValue intValue)
            {
                var index = intValue.Value;
                var elementType = GetValueTupleElementType(baseType, index);
                if (elementType != null)
                {
                    return elementType;
                }
            }

            // 如果索引不是常量，返回 object（因为 ITuple 索引器返回 object）
        }

        return typeof(object);
    }

    /// <summary>
    /// 获取 ValueTuple 指定索引位置的元素类型
    /// </summary>
    private Type? GetValueTupleElementType(Type tupleType, int index)
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
    private string GetValueTupleFieldName(int index)
    {
        return $"Item{index + 1}";
    }

    /// <summary>
    /// 在 IL 中加载 ValueTuple 指定索引位置的字段
    /// 处理超过 7 个元素的情况（递归访问 Rest 字段）
    /// </summary>
    private void LoadValueTupleField(ILGenerator ilGenerator, Type tupleType, int index)
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