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
        // not id
        if (Left is null && Right is not null && Oper == OldTokenGeneric.NOT)
        {
            var a = new OldValue();
            if (Right is OldID)
            {
                a = Manager.GetValue(Right as OldID);
            }else if (Right is OldValue)
            {
                a = Right as OldValue;
            }
            return new OldBool(!(bool)a.Value);
        }
        // id.id => output : id.id 's value
        if (Left is OldID && Right is OldID && Oper == OldTokenGeneric.CONCAT)
        {
            var a = Manager.GetValue(Left as OldID);
            if (a is OldAny)
            {
                var b = a as OldAny;
                return b.Variates[Right as OldID];
            }
        }
        // id1/value (and or xor) id2/value
        if (Left is OldID && Right is OldID && Oper != null)
        {
            var a = new OldValue();
            var b = new OldValue();
            if (Left is OldID)
            {
                a = Manager.GetValue(Left as OldID);   
            }

            if (Right is OldID)
            {
                b = Manager.GetValue(Right as OldID);
            }

            if (Left is OldValue && Right is OldValue)
            {
                a = Left as OldValue;
                b = Right as OldValue;
            }
            if (a is OldBool && b is OldBool)
            {
                switch (Oper)
                {
                    case OldTokenGeneric.AND:
                        return new OldBool((bool)a.Value && (bool)b.Value);
                    case OldTokenGeneric.OR:
                        return new OldBool((bool)a.Value || (bool)b.Value);
                    case OldTokenGeneric.XOR:
                        if (a.Value == b.Value)
                            return new OldBool(false);
                        else
                            return new OldBool(true);
                    default:
                        return new OldExpr();
                }
            }
        }
        // var => output : var.id
        if (Left is OldID && Right == null && Oper == null)
            return Left as OldID;
        // value => output : value
        if (Left is OldValue && Right == null && Oper == null)
            return Left as OldValue;
        // var (+-*/) value => output : new value
        if (Left is OldID && Right is OldValue && Oper != null)
        {
            var l = Manager.GetValue(Left as OldID);
            if (l is OldString)
            {
                var r = Right as OldValue;
                switch (Oper)
                {
                    case OldTokenGeneric.PLUS:
                        return new OldString((string)l.Value + r.Value);
                    case OldTokenGeneric.TIMES:
                        string a = "";
                        string b = (string)Manager.GetValue(Left as OldID).Value;
                        for (int i = 0; i < (int)r.Value; i++)
                        {
                            a += b;
                        }
                        return new OldString(a);
                    default:
                        return new OldExpr();
                }
            }else if (Manager.GetValue(Left as OldID) is OldBool)
            {
                return new OldExpr();
            }
            else
            {
                var r = Right as OldValue;
                switch (Oper)
                {
                    case OldTokenGeneric.PLUS:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value + (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value + (double)l.Value);
                        }
                    case OldTokenGeneric.MINUS:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value - (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value - (double)l.Value);
                        }
                    case OldTokenGeneric.TIMES:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value * (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value * (double)l.Value);
                        }
                    case OldTokenGeneric.DIVIDE:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value + (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value + (double)l.Value);
                        }
                    default:
                        return new OldExpr();
                }
            }
        }
        // value (+-*/) var => output : new value
        if (Left is OldValue && Right is OldID && Oper != null)
        {
            var l = Manager.GetValue(Right as OldID);
            if (l is OldString)
            {
                var r = Left as OldValue;
                switch (Oper)
                {
                    case OldTokenGeneric.PLUS:
                        return new OldString((string)l.Value + r.Value);
                    case OldTokenGeneric.TIMES:
                        string a = "";
                        string b = (string)l.Value;
                        for (int i = 0; i < (int)r.Value; i++)
                        {
                            a += b;
                        }

                        return new OldString(a);
                    default:
                        return new OldExpr();
                }
            }
            else if (Manager.GetValue(Left as OldID) is OldBool)
            {
                return new OldExpr();
            }
            else
            {
                var r = Left as OldValue;
                switch (Oper)
                {
                    case OldTokenGeneric.PLUS:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value + (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value + (double)l.Value);
                        }
                    case OldTokenGeneric.MINUS:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value - (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value - (double)l.Value);
                        }
                    case OldTokenGeneric.TIMES:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value * (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value * (double)l.Value);
                        }
                    case OldTokenGeneric.DIVIDE:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r.Value + (int)l.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r.Value + (double)l.Value);
                        }
                    default:
                        return new OldExpr();
                }
            }
        }
        // binary (+-*/) binaty => output : new value
        if (Left is BinaryOperation && Right is BinaryOperation && Oper != null)
        {
            var r = Left.Run(ref Manager);
            var l = Right.Run(ref Manager);
            if (l is OldString && r is OldInt)
            {
                var r1 = r as OldString;
                var l1 = l as OldInt;
                switch (Oper)
                {
                    case OldTokenGeneric.PLUS:
                        return new OldString((string)l1.Value + r1.Value);
                    case OldTokenGeneric.TIMES:
                        string a = "";
                        string b = (string)l1.Value;
                        for (int i = 0; i < (int)r1.Value; i++)
                        {
                            a += b;
                        }
                        return new OldString(a);
                    default:
                        return new OldExpr();
                }
            }else if(l is OldInt && r is OldString)
            {
                var r1 = l as OldString;
                var l1 = r as OldInt;
                switch (Oper)
                {
                    case OldTokenGeneric.PLUS:
                        return new OldString((string)l1.Value + r1.Value);
                    case OldTokenGeneric.TIMES:
                        string a = "";
                        string b = (string)l1.Value;
                        for (int i = 0; i < (int)r1.Value; i++)
                        {
                            a += b;
                        }
                        return new OldString(a);
                    default:
                        return new OldExpr();
                }
            }else if (r is not OldString && l is not OldString && r is not OldBool && l is not OldBool && r is not OldChar && l is not OldChar)
            {
                var l1 = l as OldValue;
                var r1 = r as OldValue;
                switch (Oper)
                {
                    case OldTokenGeneric.PLUS:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r1.Value + (int)l1.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r1.Value + (double)l1.Value);
                        }
                    case OldTokenGeneric.MINUS:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r1.Value - (int)l1.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r1.Value - (double)l1.Value);
                        }
                    case OldTokenGeneric.TIMES:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r1.Value * (int)l1.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r1.Value * (double)l1.Value);
                        }
                    case OldTokenGeneric.DIVIDE:
                        if (r is OldInt && l is OldInt)
                        {
                            return new OldInt((int)r1.Value + (int)l1.Value);
                        }
                        else
                        {
                            return new OldDouble((double)r1.Value + (double)l1.Value);
                        }
                    default:
                        return new OldExpr();
                }
            }
        }
        return new OldExpr();
    }
}