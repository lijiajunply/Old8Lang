using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class OldItem(OldID listId, OldExpr key) : ValueType
{
    public override ValueType Run(VariateManager Manager)
    {
        var a = Manager.GetValue(listId);
        OldExpr result = key.Run(Manager);
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

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        listId.LoadILValue(ilGenerator, local); // 加载 enumerator
        key.LoadILValue(ilGenerator, local); // 加载 index
        ilGenerator.Emit(OpCodes.Ldelem_I4); // 获取元素
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(int);
    }
}