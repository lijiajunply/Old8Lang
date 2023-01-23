using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class ClassVariateChanging : OldStatement
{
    private OldID   ClassID   { get; set; }
    private OldID   VariateID { get; set; }
    private OldExpr Expr      { get; set; }

    public ClassVariateChanging(OldID classId,OldID variateId,OldExpr expr)
    {
        ClassID   = classId;
        VariateID = variateId;
        Expr      = expr;
    }
    public override void Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ClassID);
        if (a is OldAny)
        {
            var b      = a as OldAny;
            var result = Expr.Run(ref Manager);
            b.Post(VariateID,result);
        }
    }
    public override string ToString() => $"{ClassID}.{VariateID} = {Expr}";
}