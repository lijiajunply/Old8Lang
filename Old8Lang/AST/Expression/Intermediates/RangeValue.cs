using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class RangeValue(OldExpr? start, OldExpr? end) : ValueType
{
    public override ValueType Run(VariateManager Manager)
    {
        var results = new List<ValueType>();

        var startValue = start?.Run(Manager);
        var endValue = end?.Run(Manager);

        if (startValue is not IntValue startIntValue || endValue is not IntValue endIntValue)
            throw new Exception("RangeValue: start or end is not IntValue");

        for (var i = startIntValue.Value; i <= endIntValue.Value; i++)
            results.Add(new IntValue(i));

        return new ArrayValue(results);
    }

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        start?.LoadILValue(ilGenerator, local);
        end?.LoadILValue(ilGenerator, local);
        // 创建一个长度为 5 的整数数组
        var rangeMethod = typeof(Enumerable).GetMethod("Range", [typeof(int), typeof(int)]);
        // 调用 Enumerable.Range 方法
        ilGenerator.Emit(OpCodes.Call, rangeMethod!);
        var a = typeof(Enumerable).GetMethod("ToArray")!;
        ilGenerator.Emit(OpCodes.Call, a.MakeGenericMethod(typeof(int)));
    }

    public override Type OutputType(LocalManager local) => typeof(IEnumerable<int>);
}