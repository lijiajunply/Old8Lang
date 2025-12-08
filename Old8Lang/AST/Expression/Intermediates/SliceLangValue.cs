using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

public class SliceLangValue(LangId id, OldExpr? start = null, OldExpr? end = null) : LangValueType
{
    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var value = id.Run(manager);
        var start1 = start?.Run(manager);
        var end1 = end?.Run(manager);

        if (value is not ILangList list) throw new InvalidOperationError(this, $"类型 '{value.GetType().Name}' 不支持切片操作");

        var length = list.GetLength();
        var startValue = start1?.GetValue<int>() ?? 0;
        var endValue = end1?.GetValue<int>() ?? length;

        return list.Slice(startValue, endValue);
    }

    public override string ToString()
    {
        if (start != null && end != null)
            return $"{id}[{start}:{end}]";
        if (start != null)
            return $"{id}[{start}:]";
        if (end != null)
            return $"{id}[:{end}]";
        return $"{id}[:]"; // Old8Lang 风格的切片表达式
    }
}