using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;


namespace Old8Lang.AST.Expression.Value;

public class OldItem(LangId listId, OldExpr key, SourcePosition position = default) : LangValueType(position)
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

        throw new InvalidOperationError(this, $"不支持的集合类型: {a?.GetType().Name ?? "null"}");
    }

    public override string ToString() => $"{listId}[{key}]";

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        listId.LoadIlValue(ilGenerator, local); // 加载 enumerator
        key.LoadIlValue(ilGenerator, local); // 加载 index
        ilGenerator.Emit(OpCodes.Ldelem_I4); // 获取元素
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(int);
    }
}