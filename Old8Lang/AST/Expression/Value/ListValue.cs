using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class ListValue : ValueType
{
    private new List<OldExpr> Value  { get; set; }

    private List<ValueType> Values { get; set; } = new List<ValueType>();

    public ListValue(List<OldExpr> value) => Value = value;
    public ListValue(List<object>  value) => Values = value.Select(x => ValueType.ObjToValue(x)).ToList();
    
    public override ValueType Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Value)
            Values.Add(VARIABLE.Run(ref Manager));
        return this;
    }

    public ValueType Get(IntValue i)
    {
        if (i.Value < 0)
            i.Value = Values.Count+i.Value;
        return Values[i.Value];
    }

    private ValueType Add(ValueType valueType)
    {
        Values.Add(valueType);
        return valueType;
    }

    private ValueType Remove(IntValue num)
    {
        var a = Values[num.Value];
        Values.RemoveAt(num.Value);
        return a;
    }

    public override string ToString() =>
        !Values.Any() ? "{"+APIs.ListToString(Value)+"}" : "{"+APIs.ListToString(Values)+"}";

    public override ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is Instance a)
        {
            if (a.Id.IdName == "Add")
            {
               var result = a.Ids[0] as ValueType;
               return Add(result);
            }
            if (a.Id.IdName == "Remove")
            {
                var result = a.Ids[0] as IntValue;
                return Remove(result);
            }
        }
        return null;
    }

    public override object GetValue() => APIs.ListToObjects(Values);
}