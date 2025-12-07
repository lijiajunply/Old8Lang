using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;


namespace Old8Lang.AST.Expression.Value;

public class OldItem(OldId listId, OldExpr key) : ValueType
{
    public override ValueType Run(LangParser.VariateManager manager)
    {
        var a = manager.GetValue(listId);
        OldExpr result = key.Run(manager);
        if (a is ListValue list && result is IntValue intResult)
            return list.Get(intResult);
        if (a is ArrayValue array && result is IntValue i)
            return array.Get(i);
        if (a is DictionaryValue dir)
        {
            if (result is ValueType keyResult) return dir.Get(keyResult);
        }

        return new VoidValue();
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