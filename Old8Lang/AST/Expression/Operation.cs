using System.Security.Cryptography;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class Operation : OldExpr
{
    private OldExpr Left { get; set; }

    private OldExpr Right { get; set; }

    private OldTokenGeneric Opera { get; set; }

    public Operation(OldExpr left, OldTokenGeneric opera, OldExpr right)
    {
        Left = left;
        Opera = opera;
        Right = right;
    }

    public string OperaToString()
    {
        if (Opera == OldTokenGeneric.PLUS)
        {
            return "+";
        }

        return "";
    }

    public override string ToString() => $"{Left} {OperaToString()} {Right}";

    public override ValueType Run(ref VariateManager Manager)
    {
        // not right
        if (Left == null && Opera == OldTokenGeneric.NOT)
            return new BoolValue(!(Right.Run(ref Manager) as BoolValue)!.Value);
        if (Left == null && Opera == OldTokenGeneric.MINUS)
            return new IntValue(-(Right.Run(ref Manager) as IntValue).Value);

        var l = Left.Run(ref Manager);
        var r = Right;

        // id.id => dot_value
        if (l is AnyValue any && Opera == OldTokenGeneric.CONCAT)
        {
            if (r is Instance r1)
                return any.Dot(r1);
            return any.Dot(r);
        }

        if (l is ListValue && Opera == OldTokenGeneric.CONCAT)
        {
            if (r is Instance r1)
            {
                List<OldExpr> values = new List<OldExpr>();
                foreach (var id in r1.Ids)
                    values.Add(id.Run(ref Manager));
                r1.Ids = values;
            }

            return l.Dot(r);
        }

        if (l is NativeStaticAny && Opera == OldTokenGeneric.CONCAT)
        {
            if (r is Instance r1)
            {
                List<OldExpr> values = new List<OldExpr>();
                foreach (var id in r1.Ids)
                    values.Add(id.Run(ref Manager));
                r1.Ids = values;
            }

            return l.Dot(r);
        }

        if (l is not AnyValue && Opera == OldTokenGeneric.CONCAT)
            return l.Dot(r);

        // r get value
        r = Right.Run(ref Manager);
        // (right)
        if (Right is OldID && l is not AnyValue)
            r = Manager.GetValue(Right as OldID);
        if (Right is Operation)
            r = Right.Run(ref Manager);


        // left and right
        if (l is BoolValue b && r is BoolValue expr && Opera == OldTokenGeneric.AND)
            return new BoolValue(b.Value && expr.Value);

        // left or right
        if (l is BoolValue b1 && r is BoolValue oldBool && Opera == OldTokenGeneric.OR)
            return new BoolValue(b1.Value || oldBool.Value);

        // left xor right
        if (l is BoolValue && r is BoolValue value && Opera == OldTokenGeneric.XOR)
            return new BoolValue(!l.Equal(value));


        // == , < , > 
        if (l is not null && r is not null && Opera == OldTokenGeneric.EQUALS)
            return new BoolValue(l.Equal(r as ValueType));
        if (l is not null && r is not null && Opera == OldTokenGeneric.LESSER)
            return new BoolValue(l.Less(r as ValueType));
        if (l is not null && r is not null && Opera == OldTokenGeneric.GREATER)
            return new BoolValue(l.Greater(r as ValueType));

        // r (+-*/) l
        if (l is not null && r is not null && Opera != null)
        {
            var r1 = r as ValueType;
            var l1 = l;
            switch (Opera)
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