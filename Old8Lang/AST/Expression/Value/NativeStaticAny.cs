using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class NativeStaticAny : ValueType
{
    public readonly string ClassName;

    private readonly Type ClassType;

    public NativeStaticAny(string classname,Type classType)
    {
        ClassName = classname;
        ClassType = classType;
    }
    public override ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldID id)
        {
            var prop = ClassType.GetProperty(id.IdName);
            if (prop is null)
            {
                var fie = ClassType.GetField(id.IdName);
                if (fie is null)
                    return null;
                return ObjToValue(fie.GetValue(null));
            }
            return ObjToValue(prop.GetValue(null));
        }
        if (dotExpr is Instance instance)
        {
            var Method = ClassType.GetMethod(instance.Id.IdName);
            var a      = APIs.ListToObjects(instance.Ids.OfType<ValueType>().ToList()).ToArray();
            var invoke = Method.Invoke(null,a);
            return ObjToValue(invoke);
        }
        return null;
    }
}