using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class OtherVariateChanging : OldStatement
{
    private OldID ID { get; set; }

    private OldExpr Sum { get; set; }

    private OldExpr Expr { get; set; }

    public OtherVariateChanging(OldID id,OldExpr sumId,OldExpr expr)
    {
        ID    = id;
        Sum = sumId;
        Expr  = expr;
    }
    public override void Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ID);
        if (a is AnyValue any)
        {
            var SumID = Sum as OldID;
            var result = Expr.Run(ref Manager);
            any.Set(SumID,result);
        }
        if (a is ArrayValue array)
        {
            var s      = Sum.Run(ref Manager) as IntValue;
            var result = Expr.Run(ref Manager);
            array.Post(s,result);
        }
        if (a is DictionaryValue dictionary)
        {
            var s      = Sum.Run(ref Manager);
            var result = Expr.Run(ref Manager);
            dictionary.Post(s,result);
        }
    }
    public override string ToString() => $"{ID}.{Sum} = {Expr}";
}