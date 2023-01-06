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

        // value => value
        if (l is not null && r == null && Oper == null)
        {
            return l;
        }
        if (r is not null && l == null && Oper == null)
        {
            return r;
        }
        // var/binary -> value
        if (Left is OldID)
            l = Manager.GetValue(Left as OldID);
        if (Left is BinaryOperation)
            l = Left.Run(ref Manager);
        
        if (Right is OldID && l is not OldAny)
            r = Manager.GetValue(Right as OldID);
        if (Right is BinaryOperation)
            r = Right.Run(ref Manager);
        
        // oldany.id
        if (l is OldAny && r is OldID)
        {
            var l1 = l as OldAny;
            bool v = false;
            var r1 = r as OldID;
            foreach (var VARIABLE in l1.Variates.Keys)
                if (VARIABLE == r1.IdName)
                    v = true;
            if (v)
                return l1.Variates[r1.IdName];
            else
                return new OldExpr();
        }
        
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

        return new OldExpr();
    }
}