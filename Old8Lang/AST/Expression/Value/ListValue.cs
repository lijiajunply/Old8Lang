using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class ListValue : ValueType, IOldList
{
    private readonly List<OldExpr> Value;

    public readonly List<ValueType> Values = [];

    public ListValue(List<OldExpr> value) => Value = value;

    public ListValue(List<object> value)
    {
        Values = value.Select(ObjToValue).ToList();
        Value = Values.OfType<OldExpr>().ToList();
    }

    public override ValueType Run(VariateManager Manager)
    {
        if(Values.Count > 0)return this;
        foreach (var expr in Value)
            Values.Add(expr.Run(Manager));
        return this;
    }

    public ValueType Get(IntValue i)
    {
        if (i.Value < 0)
            i.Value = Values.Count + i.Value;
        return Values[i.Value];
    }

    public override string ToString() =>
        "{" + Apis.ListToString(Values) + "}";

    public override ValueType Dot(OldExpr dotExpr)
    {
        return dotExpr is not Instance a ? new VoidValue() : a.FromClassToResult(this);
    }

    public override object GetValue() => Apis.ListToObjects(Values);
    public IEnumerable<ValueType> GetItems() => Values;

    public int GetLength() => Values.Count;

    public ValueType Slice(int start, int end)
    {
        if (start < 0) start += Values.Count;
        if (end < 0) end += Values.Count + 1;
        return new ListValue(Values[start..end]
            .OfType<OldExpr>()
            .ToList());
    }

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        var listConstructor = typeof(List<object>).GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor); // 创建 List<int> 实例
        if (Value.Count == 0) return;
        
        var l = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, l.LocalIndex);

        // 向 List<int> 中添加元素
        var addMethod = typeof(List<object>).GetMethod("Add")!;
        foreach (var expr in Value)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
            expr.LoadILValue(ilGenerator, local);
            var t = expr.OutputType(local);
            ilGenerator.Emit(OpCodes.Box, t!);
            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用 Add 方法
        }
        ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(List<object>);
    }
}