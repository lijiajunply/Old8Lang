namespace Old8Lang.AST.Expression.Value;

public class NativeStaticAny(string className, Type ClassType) : ValueType
{
    public readonly string ClassName = className;

    public override ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldID id)
        {
            var prop = ClassType.GetProperty(id.IdName);
            if (prop is null)
            {
                var field = ClassType.GetField(id.IdName);
                if (field is null)
                    return new VoidValue();
                return ObjToValue(field.GetValue(null)!);
            }

            return ObjToValue(prop.GetValue(null)!);
        }

        if (dotExpr is Instance instance)
        {
            var Method = ClassType.GetMethod(instance.Id.IdName);
            var a = Apis.ListToObjects(instance.Ids.OfType<ValueType>().ToList()).ToArray();
            var invoke = Method?.Invoke(null, a);
            return ObjToValue(invoke!);
        }

        return new VoidValue();
    }
}