using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

public class SliceValue(OldId id, OldExpr? start = null, OldExpr? end = null) : ValueType
{
    public override ValueType Run(LangParser.VariateManager manager)
    {
        var value = id.Run(manager);
        var start1 = start?.Run(manager);
        var end1 = end?.Run(manager);

        if (value is not IOldList list) throw new InvalidOperationError(this, $"类型 '{value.GetType().Name}' 不支持切片操作");

        var length = list.GetLength();
        var startValue = start1?.GetValue<int>() ?? 0;
        var endValue = end1?.GetValue<int>() ?? length;

        return list.Slice(startValue, endValue);
    }
}