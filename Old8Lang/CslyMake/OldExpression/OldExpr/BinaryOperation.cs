using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class BinaryOperation : OldExpr
{
    public OldExpr Left { get; set; }
    public OldExpr Right { get; set; }
    public OldTokenGeneric Oper { get; set; }
    public BinaryOperation(OldExpr left, OldTokenGeneric oper, OldExpr right)
    {
        Left = left;
        Oper = oper;
        Right = right;
    }

    public override string ToString() => $"{Left} {Oper} {Right}";

    public override OldValue Run(ref VariateManager Manager)
    {
        // not right
        if (Left == null && Oper == OldTokenGeneric.NOT)
            return new OldBool(!(Right.Run(ref Manager) as OldBool).Value);
        
        var l = Left.Run(ref Manager);
        var r = Right;
        
        // id.id => dot_value
        if (l is OldValue && r is not null && Oper == OldTokenGeneric.CONCAT)
            return l.Dot(r);
        
        r = Right.Run(ref Manager);
        // (right)
        if (Right is OldID && l is not OldAny)
            r = Manager.GetValue(Right as OldID);
        if (Right is BinaryOperation)
            r = Right.Run(ref Manager);

        
        // left and right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.AND)
            return new OldBool((l as OldBool).Value && (r as OldBool).Value);
        
        // left or right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.OR)
            return new OldBool((l as OldBool).Value || (r as OldBool).Value);
        
        // left xor right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.XOR)
            return new OldBool(!l.EQUAL(r as OldValue));
        
        // - right
        if (l is null && r is OldInt && Oper == OldTokenGeneric.MINUS)
        {
            var r1 = r as OldInt;
            r1.Value = -(int)r1.Value;
            return r1;
        }
        if (r is OldDouble && l is null && Oper == OldTokenGeneric.MINUS)
        {
            var r1 = r as OldDouble;
            r1.Value = -(int)r1.Value;
            return r1;
        }
        
        // == , < , > 
        if (l is not null && r is not null && Oper == OldTokenGeneric.EQUALS)
            return new OldBool(l.EQUAL(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.LESSER)
            return new OldBool(l.LESS(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.GREATER)
            return new OldBool(l.GREATER(r as OldValue));
        
        // r (+-*/) l
        if (l is not null && r is not null && Oper != null)
        {
            var r1 = r as OldValue;
            var l1 = l ;
            switch (Oper)
            {
                case OldTokenGeneric.PLUS:
                    return l1.PLUS(r1);
                case OldTokenGeneric.MINUS:
                    return l1.MINUS(r1);
                case OldTokenGeneric.TIMES:
                    return l1.TIMES(r1);
                case OldTokenGeneric.DIVIDE:
                    return l1.DIVIDE(r1);
            }
        }
        
        return null;
    }
}