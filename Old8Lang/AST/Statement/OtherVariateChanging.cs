using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class OtherVariateChanging : OldStatement
{
    private OldID   ClassID   { get; set; }
    private OldID   VariateID { get; set; }
    private OldExpr Expr      { get; set; }

    public OtherVariateChanging(OldID classId,OldID variateId,OldExpr expr)
    {
        ClassID   = classId;
        VariateID = variateId;
        Expr      = expr;
    }
    public override void Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ClassID);
        if (a is OldAny any)
        {
            var result = Expr.Run(ref Manager);
            any.Set(VariateID,result);
        }
    }
    public override string ToString() => $"{ClassID}.{VariateID} = {Expr}";
}