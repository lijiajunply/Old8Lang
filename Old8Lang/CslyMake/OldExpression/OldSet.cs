using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldSet : OldStatement
{
    public OldID Id { get; set; } 
    public OldExpr Value { get; set; }
    public List<OldID> Init_ID { get; set; }

    public OldSet(OldID id, OldExpr value)
    {
        Id = id;
        Value = value;
    }

    public OldSet(OldID id, OldExpr value, List<OldID> a)
    {
        Id = id;
        Value = value;
        Init_ID = a;
    }

    public override void Run(ref VariateManager Manager)
    {
        if (Value is BinaryOperation)
        {
            var a = Value.Run(ref Manager);
            var b = a as OldValue;
            Manager.Set(Id, b);
        }else if(Value is OldID)
        {
            var a = Manager.GetValue(Value as OldID);
            if (a is OldFunc)
            {
                if ((a as OldFunc).Return is not null)
                {
                    Value = Manager.GetValue(Value as OldID).Run(ref Manager);
                }
                else
                {
                    return;
                }
            }
            Manager.Set(Id, Value);
        }
        else
        {
            Manager.Set(Id, Value);
        }
    }
}