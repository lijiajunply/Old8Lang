using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldBool : OldValue
{
    public BinaryOperation Operation { get; set; }
    new bool Value { get; set; }
    public OldBool(BinaryOperation binaryOperation) => Operation = binaryOperation;
    public OldBool(bool value) => Value = value;
    public override OldExpr Run(ref VariateManager Manager)
    {
        var a = Operation.Run(ref Manager);
        if (a is OldBool)
        {
            var b = a as OldBool;
            Value = b.Value;
            return a as OldBool;
        }
        else
        {
            Value = false;
            return new OldExpr();
        }
    }
}