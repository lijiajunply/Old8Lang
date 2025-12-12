using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;


namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// list[key] 字典/数组/列表 索引访问
/// </summary>
/// <param name="listId">列表 ID</param>
/// <param name="key">关键词或索引</param>
/// <param name="position">位置</param>
public class LangListItem(LangId listId, LangExpression key, SourcePosition position = default) : LangValueType(position)
{
    public readonly LangId ListId = listId;
    public readonly LangExpression Key = key;
    
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var a = manager.GetValue(listId);
        LangExpression result = key.Run(manager);
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

        throw new InvalidOperationError(this, $"不支持的集合类型: {a?.GetType().Name ?? "null"}");
    }

    public override string ToString() => $"{listId}[{key}]";

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        var listType = listId.OutputType(local);

        if (listType == typeof(string))
        {
            // 处理字符串索引访问
            listId.LoadIlValue(ilGenerator, local); // 加载字符串
            key.LoadIlValue(ilGenerator, local); // 加载索引
            // 使用字符串的索引器属性获取字符
            var getStringItemMethod = typeof(string).GetMethod("get_Chars", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getStringItemMethod);
        }
        else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            // 处理Dictionary<TKey, TValue>类型的字典索引访问
            listId.LoadIlValue(ilGenerator, local); // 加载字典
            key.LoadIlValue(ilGenerator, local); // 加载键

            // 获取字典的泛型参数
            var genericArgs = listType.GetGenericArguments();
            var dictKeyType = genericArgs[0];

            // 确保键类型匹配
            var keyType = key.OutputType(local);
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
            listId.LoadIlValue(ilGenerator, local); // 加载字典

            // 处理键
            if (key is StringLangValue stringKey)
            {
                // 键是字符串字面量，直接加载字符串值
                ilGenerator.Emit(OpCodes.Ldstr, stringKey.Value);
            }
            else
            {
                // 键不是字符串字面量，加载并转换为字符串
                key.LoadIlValue(ilGenerator, local);
                var keyType = key.OutputType(local);
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
            listId.LoadIlValue(ilGenerator, local); // 加载数组
            key.LoadIlValue(ilGenerator, local); // 加载索引
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
            listId.LoadIlValue(ilGenerator, local); // 加载List
            key.LoadIlValue(ilGenerator, local); // 加载索引
            // 调用List<T>的索引器
            var listItemMethod = listType.GetMethod("get_Item", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, listItemMethod);
        }
        else
        {
            throw new InvalidOperationError(this, "不支持的集合类型: " + listType.Name);
        }
    }

    public override Type OutputType(LocalManager local)
    {
        var listType = listId.OutputType(local);

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

        if (listType == typeof(Dictionary<object, object>))
        {
            // 字典值可以是任意类型
        }

        return typeof(object);
    }
}