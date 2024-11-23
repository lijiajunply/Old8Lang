using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class Operation(OldExpr? left, OldTokenGeneric opera, OldExpr right) : OldExpr
{
    private string OperaToString()
    {
        if (opera == OldTokenGeneric.PLUS)
            return "+";
        if (opera == OldTokenGeneric.MINUS)
            return "-";
        if (opera == OldTokenGeneric.TIMES)
            return "*";
        if (opera == OldTokenGeneric.DIVIDE)
            return "/";
        if (opera == OldTokenGeneric.GREATER)
            return ">";
        if (opera == OldTokenGeneric.LESSER)
            return "<";
        if (opera == OldTokenGeneric.EQUALS)
            return "==";
        if (opera == OldTokenGeneric.DIFFERENT)
            return "!=";
        if (opera == OldTokenGeneric.CONCAT)
            return ".";
        return "";
    }

    public override string ToString() => $"{left} {OperaToString()} {right}";

    public override ValueType Run(ref VariateManager Manager)
    {
        // not right
        if (left == null && opera == OldTokenGeneric.NOT)
            return new BoolValue(!(right.Run(ref Manager) as BoolValue)!.Value);
        if (left == null && opera == OldTokenGeneric.MINUS)
            return new IntValue(-(right.Run(ref Manager) as IntValue)!.Value);

        var l = left?.Run(ref Manager);
        var r = right;

        // id.id => dot_value
        if (l is AnyValue any && opera == OldTokenGeneric.CONCAT)
        {
            if (r is Instance r1)
                return any.Dot(r1);
            return any.Dot(r);
        }

        if (l is ListValue && opera == OldTokenGeneric.CONCAT)
        {
            if (r is not Instance r1) return l.Dot(r);
            List<OldExpr> values = [];
            foreach (var id in r1.Ids)
            {
                values.Add(id.Run(ref Manager));
            }

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is NativeStaticAny && opera == OldTokenGeneric.CONCAT)
        {
            if (r is not Instance r1) return l.Dot(r);
            List<OldExpr> values = [];
            foreach (var id in r1.Ids)
            {
                values.Add(id.Run(ref Manager));
            }

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is not AnyValue && opera == OldTokenGeneric.CONCAT)
            return l?.Dot(r)!;

        // r get value
        r = right.Run(ref Manager);
        // (right)
        if (right is OldID oldId && l is not AnyValue)
            r = Manager.GetValue(oldId);
        if (right is Operation)
            r = right.Run(ref Manager);


        // left and right
        if (l is BoolValue b && r is BoolValue expr && opera == OldTokenGeneric.AND)
            return new BoolValue(b.Value && expr.Value);

        // left or right
        if (l is BoolValue b1 && r is BoolValue oldBool && opera == OldTokenGeneric.OR)
            return new BoolValue(b1.Value || oldBool.Value);

        // left xor right
        if (l is BoolValue && r is BoolValue value && opera == OldTokenGeneric.XOR)
            return new BoolValue(!l.Equal(value));


        // == , < , > 
        if (l is not null && r != null! && opera == OldTokenGeneric.EQUALS)
            return new BoolValue(l.Equal(r as ValueType ?? new VoidValue()));
        if (l is not null && r is not null && opera == OldTokenGeneric.LESSER)
            return new BoolValue(l.Less(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.GREATER)
            return new BoolValue(l.Greater(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.DIFFERENT)
            return new BoolValue(!l.Equal(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.LESS_EQUAL)
            return new BoolValue(l.LessEqual(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.GREATER_EQUAL)
            return new BoolValue(l.GreaterEqual(r as ValueType));

        // r (+-*/) l
        if (l is not null && r is not null)
        {
            if (r is not ValueType r1) return new VoidValue();
            switch (opera)
            {
                case OldTokenGeneric.PLUS:
                    return l.Plus(r1);
                case OldTokenGeneric.MINUS:
                    return l.Minus(r1);
                case OldTokenGeneric.TIMES:
                    return l.Times(r1);
                case OldTokenGeneric.DIVIDE:
                    return l.Divide(r1);
            }
        }

        return new VoidValue();
    }
}