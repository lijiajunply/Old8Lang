using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class OtherVariateChanging(OldID id, OldExpr sumId, OldExpr expr) : OldStatement
{
    private OldID ID { get; set; } = id;

    private OldExpr Sum { get; set; } = sumId;

    private OldExpr Expr { get; set; } = expr;

    public override void Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ID);
        if (a is AnyValue any)
        {
            if (Sum is not OldID sum) throw new TypeError(this,this);
            var result = Expr.Run(ref Manager);
            any.Set(sum, result);
        }   

        if (a is ArrayValue array)
        {
            var s = Sum.Run(ref Manager);
            if (s is not IntValue sum) throw new TypeError(this,this);
            var result = Expr.Run(ref Manager);
            array.Add(sum, result);
        }

        if (a is DictionaryValue dictionary)
        {
            var s = Sum.Run(ref Manager);
            var result = Expr.Run(ref Manager);
            dictionary.Post(s, result);
        }
    }

    public override string ToString() => $"{ID}.{Sum} = {Expr}";
}