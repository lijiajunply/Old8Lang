using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class DictionaryLangValue : LangValueType, ILangList
{
    private readonly List<TupleLangValue> Tuples;
    public readonly List<(LangValueType Key, LangValueType Value)> Value = [];

    public DictionaryLangValue(List<TupleLangValue> tuples, SourcePosition position = default) : base(position)
    {
        Tuples = tuples;
    }

    public DictionaryLangValue(SourcePosition position = default) : base(position)
    {
        Tuples = [];
    }

    public DictionaryLangValue(List<KeyValuePair<OldExpr, OldExpr>> list, SourcePosition position = default) : base(position)
    {
        Tuples = list.Select(x => new TupleLangValue(x.Key, x.Value)).ToList();
    }

    public override LangValueType Run(VariateManager manager)
    {
        foreach (var tuple in Tuples)
        {
            tuple.Run(manager);
            Value.Add(tuple.Value);
        }

        return this;
    }

    public override LangValueType Dot(OldExpr dotExpr)
    {
        return dotExpr is not Instance a ? throw new InvalidOperationError(this, "字典类型只支持实例调用操作") : a.FromClassToResult(this);
    }

    public LangValueType Get(LangValueType key)
    {
        var a = Value.Where(x => x.Key.Equal(key)).ToList();
        return a[0].Value;
    }

    public void Update(LangValueType key, LangValueType langValueType)
    {
        Get(key);
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        Value[b] = (key, langValueType);
    }

    public override string ToString()
    {
        if (Value.Count == 0)
        {
            return "{" + string.Join(", ", Tuples) + "}";
        }
        
        var sb = new StringBuilder();
        for (var i = 0; i < Value.Count; i++)
        {
            var valueTuple = Value[i];
            sb.Append($"{valueTuple.Key}: {valueTuple.Value}");
            if (i < Value.Count - 1)
            {
                sb.Append(", ");
            }
        }

        return "{" + sb + "}"; // Old8Lang 风格的字典，使用 { } 包裹，键值对用 : 分隔
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not AnyLangValue typeAny) throw new TypeError(this, "AnyValue", otherLangValueType.GetType().Name);

        foreach (var a in Value)
        {
            var aKey = a.Key.Run(manager);
            var aValue = a.Value.Run(manager);
            if (aKey is not StringLangValue s) continue;
            typeAny.Set(new LangId(s.Value), aValue);
        }

        return typeAny;
    }

    public IEnumerable<LangValueType> GetItems()
        => Value.Select(x => new TupleLangValue(x.Key, x.Value));

    public int GetLength() => Value.Count;

    public LangValueType Slice(int start, int end)
    {
        throw new InvalidOperationError(this, "字典类型不支持切片操作");
    }

    public Type GetChildType() => typeof(KeyValuePair<object, object>);

    public override Type OutputType(LocalManager local) => typeof(Dictionary<object, object>);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        var listConstructor = typeof(Dictionary<object, object>).GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor); // 创建 List<int> 实例

        var l = ilGenerator.DeclareLocal(typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Stloc, l.LocalIndex);

        // 向 List<int> 中添加元素
        var addMethod = typeof(Dictionary<object, object>).GetMethod("Add")!;
        foreach (var expr in Tuples)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
            expr.Item1.LoadIlValue(ilGenerator, local);
            var t = expr.Item1.OutputType(local);
            ilGenerator.Emit(OpCodes.Box, t!);
            expr.Item2.LoadIlValue(ilGenerator, local);
            t = expr.Item2.OutputType(local);
            ilGenerator.Emit(OpCodes.Box, t!);
            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用 Add 方法
        }

        ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
    }
}