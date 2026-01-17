using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;


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


    public override LangValueType Run(VariateManager manager)
    {
        var a = manager.GetValue(ListId);
        LangExpression result = Key.Run(manager);

        // 检查是否是 AnyLangValue（类实例）且定义了 _getitem 方法
        if (a is AnyLangValue anyValue)
        {
            var getitemMethods = anyValue.Metadata.MethodTable.LookupMethod("_getitem");
            if (getitemMethods is not null && getitemMethods.Count > 0)
            {
                // 创建一个 Instance 表达式来调用 _getitem 方法
                var getitemCall = new Instance(
                    new LangId("_getitem"),
                    [result],
                    [],
                    Position
                );

                // 通过 Dot 操作调用方法
                return anyValue.Dot(getitemCall, manager);
            }
        }

        if (a is ListLangValue list)
        {
            if (result is not IntLangValue intResult) throw new TypeError(this, "IntValue", result.GetType().Name);
            return list.Get(intResult);
        }

        if (a is ArrayLangValue array)
        {
            if (result is not IntLangValue i) throw new TypeError(this, "IntValue", result.GetType().Name);
            return array.Get(i);
        }

        if (a is DictionaryLangValue dir)
        {
            if (result is not LangValueType keyResult) throw new TypeError(this, "ValueType", result.GetType().Name);
            return dir.Get(keyResult);
        }

        if (a is StringLangValue str)
        {
            if (result is not IntLangValue intResult) throw new TypeError(this, "IntValue", result.GetType().Name);
            return str.Get(intResult);
        }

        if (a is TupleLangValue tuple)
        {
            if (result is not IntLangValue intResult) throw new TypeError(this, "IntValue", result.GetType().Name);
            return tuple.Get(intResult);
        }

        throw new InvalidOperationError(this, $"不支持的集合类型: {a?.GetType().Name ?? "null"}");
    }

    public override string ToString() => $"{ListId}[{Key}]";

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        var listType = ListId.OutputType(local);

        if (listType == typeof(string))
        {
            // 处理字符串索引访问
            ListId.LoadIlValue(ilGenerator, local); // 加载字符串
            Key.LoadIlValue(ilGenerator, local); // 加载索引
            // 使用字符串的索引器属性获取字符
            var getStringItemMethod = typeof(string).GetMethod("get_Chars", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getStringItemMethod);
        }
        else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            // 处理Dictionary<TKey, TValue>类型的字典索引访问
            ListId.LoadIlValue(ilGenerator, local); // 加载字典
            Key.LoadIlValue(ilGenerator, local); // 加载键

            // 获取字典的泛型参数
            var genericArgs = listType.GetGenericArguments();
            var dictKeyType = genericArgs[0];

            // 确保键类型匹配
            var keyType = Key.OutputType(local);
            if (keyType != dictKeyType)
            {
                if (dictKeyType.IsValueType && !keyType!.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, dictKeyType);
                }
                else if (!dictKeyType.IsValueType && keyType!.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, keyType);
                }
            }

            // 调用字典的索引器（get_Item方法）
            var getDictionaryItemMethod = listType.GetMethod("get_Item", [dictKeyType])!;
            ilGenerator.Emit(OpCodes.Callvirt, getDictionaryItemMethod);
        }
        else if (listType == typeof(Dictionary<string, object>))
        {
            // 处理Dictionary<string, object>类型的字典索引访问
            ListId.LoadIlValue(ilGenerator, local); // 加载字典

            // 处理键
            if (Key is StringLangValue stringKey)
            {
                // 键是字符串字面量，直接加载字符串值
                ilGenerator.Emit(OpCodes.Ldstr, stringKey.Value);
            }
            else
            {
                // 键不是字符串字面量，加载并转换为字符串
                Key.LoadIlValue(ilGenerator, local);
                var keyType = Key.OutputType(local);
                if (keyType != typeof(string))
                {
                    // 如果键不是字符串类型，转换为字符串
                    ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToString", [typeof(object)])!);
                }
            }

            // 调用字典的索引器（get_Item方法）
            var getDictionaryItemMethod = typeof(Dictionary<string, object>).GetMethod("get_Item", [typeof(string)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getDictionaryItemMethod);
        }
        else if (listType.IsArray)
        {
            // 处理数组索引访问
            ListId.LoadIlValue(ilGenerator, local); // 加载数组
            Key.LoadIlValue(ilGenerator, local); // 加载索引
            // 根据元素类型选择适当的 Ldelem 指令
            if (listType.GetElementType()!.IsValueType)
            {
                // 对于值类型数组，使用 Ldelem 指令
                ilGenerator.Emit(OpCodes.Ldelem, listType.GetElementType()!);
            }
            else
            {
                // 对于引用类型数组，使用 Ldelem_Ref 指令
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
            }
        }
        else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // 处理List<T>索引访问
            ListId.LoadIlValue(ilGenerator, local); // 加载List
            Key.LoadIlValue(ilGenerator, local); // 加载索引
            // 调用List<T>的索引器
            var listItemMethod = listType.GetMethod("get_Item", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, listItemMethod);
        }
        else if (listType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问
            ListId.LoadIlValue(ilGenerator, local);

            if (Key is IntLangValue intVal)
            {
                // 常量索引，优化为字段访问
                OperationHelpers.DotOperatorILHelper.GenerateValueTupleItemAccess(ilGenerator, listType, intVal.Value);
            }
            else
            {
                // 变量索引，装箱为 ITuple 并使用索引器
                ilGenerator.Emit(OpCodes.Box, listType);
                Key.LoadIlValue(ilGenerator, local);
                var keyType = Key.OutputType(local);
                if (keyType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                }

                var indexer = typeof(System.Runtime.CompilerServices.ITuple).GetProperty("Item")!;
                ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
            }
        }
        else if (listType == typeof(object))
        {
            // 处理object类型的索引访问（运行时动态处理）
            // 这种情况通常发生在嵌套数组或动态类型的场景中
            ListId.LoadIlValue(ilGenerator, local); // 加载对象

            // 尝试将object转换为数组
            var objectArrayLocal = ilGenerator.DeclareLocal(typeof(object[]));
            ilGenerator.Emit(OpCodes.Isinst, typeof(object[]));
            ilGenerator.Emit(OpCodes.Stloc, objectArrayLocal);
            ilGenerator.Emit(OpCodes.Ldloc, objectArrayLocal);

            // 加载索引
            Key.LoadIlValue(ilGenerator, local);

            // 从数组中获取元素
            ilGenerator.Emit(OpCodes.Ldelem_Ref);
        }
        else
        {
            throw new InvalidOperationError(this, "不支持的集合类型: " + listType.Name);
        }
    }

    public override Type OutputType(LocalManager local)
    {
        var listType = ListId.OutputType(local);

        if (listType == typeof(string))
        {
            return typeof(char); // 字符串索引访问返回 char 类型
        }

        if (listType.IsArray)
        {
            // 数组索引访问返回数组元素类型
            return listType.GetElementType() ?? typeof(object);
        }

        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // List<T>索引访问返回T类型
            return listType.GetGenericArguments()[0];
        }

        if (listType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问
            // 如果索引是常量，可以推断出具体的元素类型
            if (Key is IntLangValue intValue)
            {
                var index = intValue.Value;
                var elementType = GetValueTupleElementType(listType, index);
                if (elementType != null)
                {
                    return elementType;
                }
            }

            // 如果索引不是常量，返回 object（因为 ITuple 索引器返回 object）
            return typeof(object);
        }

        if (listType == typeof(Dictionary<object, object>))
        {
            // 字典值可以是任意类型
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
}