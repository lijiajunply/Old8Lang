using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;


namespace Old8Lang.AST.Expression.Value;

public class OldItem(OldId listId, OldExpr key, SourcePosition position = default) : ValueType(position)
{
    public override ValueType Run(LangParser.VariateManager manager)
    {
        var a = manager.GetValue(listId);
        OldExpr result = key.Run(manager);
        if (a is ListValue list)
        {
            if (result is not IntValue intResult) throw new TypeError(this, "IntValue", result.GetType().Name);
            return list.Get(intResult);
        }

        if (a is ArrayValue array)
        {
            if (result is not IntValue i) throw new TypeError(this, "IntValue", result.GetType().Name);
            return array.Get(i);
        }

        if (a is DictionaryValue dir)
        {
            if (result is not ValueType keyResult) throw new TypeError(this, "ValueType", result.GetType().Name);
            return dir.Get(keyResult);
        }

        throw new InvalidOperationError(this, $"不支持的集合类型: {a?.GetType().Name ?? "null"}");
    }

    public override string ToString() => $"the key: {key} in {listId}";

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