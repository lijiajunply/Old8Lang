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
public class LangListItem(LangId listId, OldExpr key, SourcePosition position = default) : LangValueType(position)
{
    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var a = manager.GetValue(listId);
        OldExpr result = key.Run(manager);
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
            listId.LoadIlValue(ilGenerator, local); // 加载字符串
            key.LoadIlValue(ilGenerator, local); // 加载索引
            // 使用字符串的索引器属性获取字符
            var getStringItemMethod = typeof(string).GetMethod("get_Chars", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getStringItemMethod);
            // 直接返回 char 值，CharLangValue.LoadIlValue 会处理它
        }
        else
        {
            listId.LoadIlValue(ilGenerator, local); // 加载集合
            key.LoadIlValue(ilGenerator, local); // 加载索引
            ilGenerator.Emit(OpCodes.Ldelem_Ref); // 获取引用类型的元素
        }
    }

    public override Type OutputType(LocalManager local)
    {
        var listType = listId.OutputType(local);
        
        if (listType == typeof(string))
        {
            return typeof(char); // 字符串索引访问返回 char 类型
        }
        else
        {
            return typeof(object);
        }
    }
}