using System.Security.Cryptography;
using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression;

public class Operation : OldExpr
{
    public OldExpr Left { get; set; }
    public OldExpr Right { get; set; }
    public OldTokenGeneric Oper { get; set; }
    public Operation(OldExpr left, OldTokenGeneric oper, OldExpr right)
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
            return new OldBool(!(Right.Run(ref Manager) as OldBool)!.Value);
        
        var l = Left.Run(ref Manager);
        var r = Right;
        
        // id.id => dot_value
        if (l is OldAny any  && Oper == OldTokenGeneric.CONCAT)
        {
            if (r is OldInstance r1)
                return any.Dot(r1,r1.Ids);
            return any.Dot(r,new List<OldExpr>());
        }
        if (l is OldList && Oper == OldTokenGeneric.CONCAT)
        {
            if (r is OldInstance r1)
            {
                List<OldExpr> values = new List<OldExpr>();
                foreach (var id in r1.Ids)
                    values.Add(id.Run(ref Manager));
                r1.Ids = values;
            }
            return l.Dot(r);
        }
        if (l is not OldAny && Oper == OldTokenGeneric.CONCAT)
            return l.Dot(r);
        
        // r get value
        r = Right.Run(ref Manager);
        // (right)
        if (Right is OldID && l is not OldAny)
            r = Manager.GetValue(Right as OldID);
        if (Right is Operation)
            r = Right.Run(ref Manager);

        
        // left and right
        if (l is OldBool b && r is OldBool expr && Oper == OldTokenGeneric.AND)
            return new OldBool(b.Value && expr.Value);
        
        // left or right
        if (l is OldBool b1 && r is OldBool oldBool && Oper == OldTokenGeneric.OR)
            return new OldBool(b1.Value || oldBool.Value);
        
        // left xor right
        if (l is OldBool && r is OldBool value && Oper == OldTokenGeneric.XOR)
            return new OldBool(!l.Equal(value));
        
        // - right
        if (l is null && r is OldInt i && Oper == OldTokenGeneric.MINUS)
        {
            i.Value = -i.Value;
            return i;
        }
        if (r is OldDouble && l is null && Oper == OldTokenGeneric.MINUS)
        {
            var r1 = r as OldDouble;
            r1.Value = -(int)r1.Value;
            return r1;
        }
        
        // == , < , > 
        if (l is not null && r is not null && Oper == OldTokenGeneric.EQUALS)
            return new OldBool(l.Equal(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.LESSER)
            return new OldBool(l.Less(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.GREATER)
            return new OldBool(l.Greater(r as OldValue));
        
        // r (+-*/) l
        if (l is not null && r is not null && Oper != null)
        {
            var r1 = r as OldValue;
            var l1 = l ;
            switch (Oper)
            {
                case OldTokenGeneric.PLUS:
                    return l1.Plus(r1);
                case OldTokenGeneric.MINUS:
                    return l1.Minus(r1);
                case OldTokenGeneric.TIMES:
                    return l1.Times(r1);
                case OldTokenGeneric.DIVIDE:
                    return l1.Divide(r1);
            }
        }
        
        return null;
    }
}