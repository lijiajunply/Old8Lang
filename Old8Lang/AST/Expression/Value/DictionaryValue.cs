using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class DictionaryValue : ValueType, IOldList
{
    private readonly List<TupleValue> tuples;
    public readonly List<(ValueType Key, ValueType Value)> Value = [];

    public DictionaryValue(List<TupleValue> tuples)
    {
        this.tuples = tuples;
    }

    public DictionaryValue()
    {
        tuples = [];
    }

    public DictionaryValue(List<KeyValuePair<OldExpr, OldExpr>> list)
    {
        tuples = list.Select(x => new TupleValue(x.Key, x.Value)).ToList();
    }

    public override ValueType Run(VariateManager Manager)
    {
        foreach (var tuple in tuples)
        {
            tuple.Run(Manager);
            Value.Add(tuple.Value);
        }

        return this;
    }

    public override ValueType Dot(OldExpr dotExpr)
    {
        return dotExpr is not Instance a ? new VoidValue() : a.FromClassToResult(this);
    }

    public ValueType Get(ValueType key)
    {
        var a = Value.Where(x => x.Key.Equal(key)).ToList();
        return a[0].Value;
    }

    public void Update(ValueType key, ValueType valueType)
    {
        Get(key);
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        Value[b] = (key, valueType);
    }

    public override string ToString()
    {
        if (Value.Count == 0) return "{" + Apis.ListToString(tuples) + "}";
        var sb = new StringBuilder();
        foreach (var valueTuple in Value)
            sb.Append($"{valueTuple.Key},{valueTuple.Value};");

        return "{" + sb + "}";
    }

    public override ValueType Converse(ValueType otherValueType, VariateManager Manager)
    {
        if (otherValueType is not AnyValue typeAny) return new VoidValue();

        foreach (var a in Value)
        {
            var aKey = a.Key.Run(Manager);
            var aValue = a.Value.Run(Manager);
            if (aKey is not StringValue s) continue;
            typeAny.Set(new OldID(s.Value), aValue);
        }

        return typeAny;
    }

    public IEnumerable<ValueType> GetItems()
        => Value.Select(x => new TupleValue(x.Key, x.Value));

    public int GetLength() => Value.Count;

    public ValueType Slice(int start, int end)
    {
        throw new Exception("dictionary is not support Slice");
    }

    public Type GetChildType() => typeof(KeyValuePair<object, object>);

    public override Type OutputType(LocalManager local) => typeof(Dictionary<object, object>);

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        var listConstructor = typeof(Dictionary<object, object>).GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor); // 创建 List<int> 实例

        var l = ilGenerator.DeclareLocal(typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Stloc, l.LocalIndex);

        // 向 List<int> 中添加元素
        var addMethod = typeof(Dictionary<object, object>).GetMethod("Add")!;
        foreach (var expr in tuples)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
            expr.Item1.LoadILValue(ilGenerator, local);
            var t = expr.Item1.OutputType(local);
            ilGenerator.Emit(OpCodes.Box, t!);
            expr.Item2.LoadILValue(ilGenerator, local);
            t = expr.Item2.OutputType(local);
            ilGenerator.Emit(OpCodes.Box, t!);
            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用 Add 方法
        }

        ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
    }
}