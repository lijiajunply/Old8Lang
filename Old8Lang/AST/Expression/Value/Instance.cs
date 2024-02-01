using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class Instance(OldID oldId, List<OldExpr> ids) : ValueType
{
    public List<OldExpr> Ids { get; set; } = ids;
    public OldID Id => oldId;

    public override ValueType Run(ref VariateManager Manager)
    {
        switch (Id.IdName)
        {
            case "type":
                return new TypeValue(Ids[0]).Run(ref Manager);
            case "exec":
            {
                if (Ids[0] is not StringValue stringValue) return new VoidValue();
                var a = Manager.Interpreter?.Build(code: stringValue.Value);
                a?.ImportRun(ref Manager);
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

    public override string ToString() => Id + "(" + Apis.ListToString(Ids) + ")";
}