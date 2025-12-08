using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 范围表达式，创建一个整数数组
/// </summary>
/// <param name="start"></param>
/// <param name="end"></param>
/// <param name="position"></param>
public class RangeLangValue(OldExpr? start, OldExpr? end, SourcePosition position = default) : LangValueType(position)
{
    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var results = new List<LangValueType>();

        var startValue = start?.Run(manager);
        var endValue = end?.Run(manager);

        if (startValue is not IntLangValue startIntValue || endValue is not IntLangValue endIntValue)
            throw new TypeError(this, "IntValue", $"RangeValue: start 或 end 不是 IntValue，实际得到了 {startValue?.GetType().Name} 和 {endValue?.GetType().Name}");

        for (var i = startIntValue.Value; i <= endIntValue.Value; i++)
            results.Add(new IntLangValue(i));

        return new ArrayLangValue(results);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        start?.LoadIlValue(ilGenerator, local);
        end?.LoadIlValue(ilGenerator, local);
        // 创建一个长度为 5 的整数数组
        var rangeMethod = typeof(Enumerable).GetMethod("Range", [typeof(int), typeof(int)]);
        // 调用 Enumerable.Range 方法
        ilGenerator.Emit(OpCodes.Call, rangeMethod!);
        var a = typeof(Enumerable).GetMethod("ToArray")!;
        ilGenerator.Emit(OpCodes.Call, a.MakeGenericMethod(typeof(int)));
    }

    public override Type OutputType(LocalManager local) => typeof(IEnumerable<int>);

    public override string ToString() => $"{start}..{end}"; // Old8Lang 风格的范围表达式
}