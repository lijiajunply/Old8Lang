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

    public bool BoolSet() => true;
    public override OldExpr Run(ref VariateManager Manager)
    {
        var l = Left;
        var r = Right;
        
        // var/binary -> value (left)
        if (Left is OldID)
            l = Manager.GetValue(Left as OldID);
        if (Left is BinaryOperation)
            l = Left.Run(ref Manager);
        
        // id.id => dot_value
        if (l is OldValue && r is OldID && Oper == OldTokenGeneric.CONCAT)
            return (l as OldValue).Dot(r as OldID);

        // (right)
        if (Right is OldID && l is not OldAny)
            r = Manager.GetValue(Right as OldID);
        if (Right is BinaryOperation)
            r = Right.Run(ref Manager);
        
        // oldany.id

        // not right 
        if (r is OldBool && l == null && Oper == OldTokenGeneric.NOT)
            return new OldBool(!(bool)(r as OldBool).Value);
        
        // left and right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.AND)
            return new OldBool((bool)(l as OldBool).Value && (bool)(r as OldBool).Value);
        
        // left or right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.OR)
            return new OldBool((bool)(l as OldBool).Value || (bool)(r as OldBool).Value);
        
        // left xor right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.XOR)
            return new OldBool(!((bool)(l as OldBool).Value == (bool)(l as OldBool).Value));
        
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
            return new OldBool((l as OldValue).EQUAL(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.LESSER)
            return new OldBool((l as OldValue).LESS(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.GREATER)
            return new OldBool((l as OldValue).GREATER(r as OldValue));
        
        // r (+-*/) l
        if (l is not null && r is not null && Oper != null)
        {
            var r1 = r as OldValue;
            var l1 = l as OldValue;
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