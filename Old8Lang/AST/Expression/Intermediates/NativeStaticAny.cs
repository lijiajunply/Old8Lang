using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.Intermediates;

public class NativeStaticAny(string className, Type classType) : ValueType
{
    public readonly string ClassName = className;

    public override ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldId id)
        {
            var prop = classType.GetProperty(id.IdName);
            if (prop is null)
            {
                var field = classType.GetField(id.IdName);
                if (field is null)
                    return new VoidValue();
                return ObjToValue(field.GetValue(null)!);
            }

            return ObjToValue(prop.GetValue(null)!);
        }

        if (dotExpr is Instance instance)
        {
            var method = classType.GetMethod(instance.Id.IdName);
            var a = Apis.ListToObjects(instance.Ids.OfType<ValueType>().ToList()).ToArray();
            var invoke = method?.Invoke(null, a);
            return ObjToValue(invoke!);
        }

        return new VoidValue();
    }
}