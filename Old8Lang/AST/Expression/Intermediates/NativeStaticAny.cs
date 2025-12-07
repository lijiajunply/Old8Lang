using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

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
                    throw new AttributeError(this, id.IdName, ClassName);
                return ObjToValue(field.GetValue(null)!);
            }

            return ObjToValue(prop.GetValue(null)!);
        }

        if (dotExpr is Instance instance)
        {
            var method = classType.GetMethod(instance.Id.IdName);
            if (method is null)
                throw new AttributeError(this, instance.Id.IdName, ClassName);
            var a = Apis.ListToObjects(instance.Ids.OfType<ValueType>().ToList()).ToArray();
            var invoke = method.Invoke(null, a);
            return ObjToValue(invoke!);
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }
}