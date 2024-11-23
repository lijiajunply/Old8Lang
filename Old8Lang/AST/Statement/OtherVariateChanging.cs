using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class OtherVariateChanging(OldID id, OldExpr sumId, OldExpr expr) : OldStatement
{
    public override void Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(id);
        if (a is AnyValue any)
        {
            if (sumId is not OldID sum) throw new TypeError(this,this);
            var result = expr.Run(ref Manager);
            any.Set(sum, result);
        }   

        if (a is ArrayValue array)
        {
            var s = sumId.Run(ref Manager);
            if (s is not IntValue sum) throw new TypeError(this,this);
            var result = expr.Run(ref Manager);
            array.Set(sum, result);
        }

        if (a is DictionaryValue dictionary)
        {
            var s = sumId.Run(ref Manager);
            var result = expr.Run(ref Manager);
            dictionary.Update(s, result);
        }
    }

    public override string ToString() => $"{id}.{sumId} = {expr}";
}