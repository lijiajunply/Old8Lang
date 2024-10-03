using Old8Lang.CslyParser;

// ReSharper disable once CheckNamespace
namespace Old8Lang.AST.Expression.Value;

public class Instance(OldID oldId, List<OldExpr> ids) : ValueType
{
    public List<OldExpr> Ids { get; } = ids;
    public OldID Id => oldId;

    public override ValueType Run(ref VariateManager Manager)
    {
        var results = new List<ValueType>();

        foreach (var t in Ids)
        {
            results.Add(t.Run(ref Manager));
        }

        switch (Id.IdName)
        {
            case "Type":
                return new TypeValue(results[0]).Run(ref Manager);
            case "Exec":
            {
                if (results[0] is not StringValue stringValue) return new VoidValue();
                var a = Manager.Interpreter?.Build(code: stringValue.Value);
                a?.Run(ref Manager);
                return new VoidValue();
            }
            case "ShowValues":
            {
                Console.WriteLine(Manager);
                return new VoidValue();
            }
            case "Json":
            {
                return (results[0] as AnyValue)?.ToJson()
                    as ValueType ?? new VoidValue();
            }
            case "ToObj":
                return (results[0] as StringValue)?.ToObj()
                    as ValueType ?? new VoidValue();
            case "PrintLine":
            {
                if (results.Count == 0)
                {
                    Console.WriteLine();
                    return new VoidValue();
                }

                var value = results[0];
                for (var i = 1; i < results.Count; i++)
                {
                    value = value.Plus(results[i]);
                }

                Console.WriteLine(value);
                return new VoidValue();
            }
            case "Print":
            {
                if (results.Count == 0) return new VoidValue();

                var value = results[0];
                for (var i = 1; i < results.Count; i++) value = value.Plus(results[i]);

                Console.Write(value);
                return new VoidValue();
            }
        }

        var result = Id.Run(ref Manager);
        if (result is FuncValue funcValue)
        {
            result = funcValue.Run(ref Manager, Ids);
        }

        // 初始化 调用init方法
        if (result is AnyValue anyValue)
        {
            if (anyValue.Result.TryGetValue("init", out result))
            {
                if (result is not FuncValue value) throw new Exception("init is not function");
                value.Run(ref anyValue.manager, results.OfType<OldExpr>().ToList());
            }
            else if (results.Count != 0)
            {
                throw new Exception("No corresponding init function found");
            }

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
            type = baseValue switch
            {
                DictionaryValue => Type.GetType("Old8Lang.AST.Expression.DictionaryValueFuncStatic"),
                ListValue => Type.GetType("Old8Lang.AST.Expression.ListValueFuncStatic"),
                _ => Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic")
            };
            m = type?.GetMethod(Id.IdName);
        }

        if (m == null && baseValue is not DictionaryValue or ListValue)
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