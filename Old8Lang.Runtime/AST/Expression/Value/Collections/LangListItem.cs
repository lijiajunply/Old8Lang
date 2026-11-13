using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;


using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// list[key] 字典/数组/列表 索引访问
/// </summary>
/// <param name="listId">列表 ID</param>
/// <param name="key">关键词或索引</param>
/// <param name="position">位置</param>
public partial class LangListItem(LangId listId, LangExpression key, SourcePosition position = default)
    : LangValueType(position)
{

    public readonly LangId ListId = listId;
    public readonly LangExpression Key = key;

    public override string ToString() => $"{ListId}[{Key}]";

    /// <summary>
    /// 加载索引表达式并保证栈上是 <see cref="int"/>（IlIndexHelper 的参数类型）。
    /// </summary>
    internal void EmitIntIndex(ILGenerator ilGenerator, LocalManager local)
    {
        Key.Accept(new CompilerVisitor(ilGenerator, local));
        var keyType = Key.Accept(new TypeInferenceVisitor(local));

        if (keyType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
        }
        else if (keyType == typeof(long))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
        }
    }

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
}
