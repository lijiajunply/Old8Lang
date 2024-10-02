using Old8Lang.CslyParser;

// ReSharper disable once CheckNamespace
namespace Old8Lang.AST.Expression.Value;

public class Instance(OldID oldId, List<OldExpr> ids) : ValueType
{
    public List<OldExpr> Ids { get; set; } = ids;
    public OldID Id => oldId;

    public override ValueType Run(ref VariateManager Manager)
    {
        switch (Id.IdName)
        {
            case "Type":
                return new TypeValue(Ids[0]).Run(ref Manager);
            case "Exec":
            {
                if (Ids[0] is not StringValue stringValue) return new VoidValue();
                var a = Manager.Interpreter?.Build(code: stringValue.Value);
                a?.ImportRun(ref Manager);
                return new VoidValue();
            }
            case "ShowValues":
            {
                Console.WriteLine(Manager);
                return new VoidValue();
            }
            case "Json":
            {
                return (Ids[0].Run(ref Manager) as AnyValue)?.ToJson()
                    as ValueType ?? new VoidValue();
            }
            case "ToObj":
                return (Ids[0].Run(ref Manager) as StringValue)?.ToObj()
                    as ValueType ?? new VoidValue();
            case "PrintLine":
            {
                Console.WriteLine(Ids[0].Run(ref Manager));
                return new VoidValue();
            }
            case "Print":
            {
                Console.WriteLine(Ids[0].Run(ref Manager));
                return new VoidValue();
            }
        }

        var result = Id.Run(ref Manager);
        if (result is FuncValue funcValue)
        {
            result = funcValue.Run(ref Manager, Ids);
        }

        if (result is AnyValue anyValue)
        {
            if (anyValue.Result.TryGetValue("init", out result))
                anyValue.Dot(result);
            result = anyValue;
        }

        if (result is NativeAnyValue nativeAnyValue)
        {
            List<ValueType> a = [];
            foreach (var id in Ids)
                a.Add(id.Run(ref Manager));
            nativeAnyValue.New(Apis.ListToObjects(a).ToArray());
            result = nativeAnyValue;
        }

        return result;
    }

    public ValueType FromClassToResult(ValueType baseValue)
    {
        var type = baseValue.GetType();
        var m = type.GetMethod(Id.IdName);
        if (m == null)
        {
            type = Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic");
            m = type?.GetMethod(Id.IdName);
        }

        var os = new List<object>() { baseValue };
        os.AddRange(Ids);
        var r = m?.Invoke(baseValue, os.ToArray());
        if (r is ValueType v) return v;
        return ObjToValue(r!);
    }

    public override string ToString() => Id + "(" + Apis.ListToString(Ids) + ")";
}