using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 嵌套索引访问表达式，用于处理 array[index1][index2] 的情况
/// </summary>
/// <param name="baseIndex">基础索引访问，如 array[index1]</param>
/// <param name="nestedIndex">嵌套索引，如 [index2]</param>
/// <param name="position">源代码位置</param>
public partial class NestedIndexAccess(LangListItem baseIndex, LangExpression nestedIndex, SourcePosition position = default)
    : LangValueType(position)
{
    public readonly LangListItem BaseIndex = baseIndex;
    public readonly LangExpression NestedIndex = nestedIndex;

    public override LangValueType Run(VariateManager manager)
    {
        // 首先运行基础索引访问，获取结果
        var baseResult = BaseIndex.Run(manager);

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
            if (NestedIndex.Run(manager) is not LangValueType keyResult)
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
            if (NestedIndex.Run(manager) is not IntLangValue intResult)
                throw new TypeError(this, "IntValue", NestedIndex.Run(manager).GetType().Name);
            return tuple.Get(intResult);
        }

        throw new InvalidOperationError(this, $"不支持的嵌套索引访问类型: {baseResult?.GetType().Name ?? "null"}");
    }

    public override string ToString() => $"{BaseIndex}[{NestedIndex}]";

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 首先加载基础索引访问的结果
        BaseIndex.LoadIlValue(ilGenerator, local);

        // 然后加载嵌套索引
        NestedIndex.LoadIlValue(ilGenerator, local);

        // 根据基础索引访问的输出类型选择适当的索引访问方法
        var baseType = BaseIndex.OutputType(local);

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
        else
        {
            throw new InvalidOperationError(this, "不支持的嵌套索引访问类型: " + baseType.Name);
        }
    }

    public override Type OutputType(LocalManager local)
    {
        var baseType = BaseIndex.OutputType(local);

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

        return typeof(object);
    }
}