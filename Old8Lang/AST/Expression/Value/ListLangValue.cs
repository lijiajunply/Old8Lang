using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;


namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 列表
/// </summary>
public class ListLangValue : LangValueType, ILangList
{
    private readonly List<OldExpr> Value;

    public readonly List<LangValueType> Values = [];

    public ListLangValue(List<OldExpr> value, SourcePosition position = default) : base(position) => Value = value;

    public ListLangValue(List<object> value, SourcePosition position = default) : base(position)
    {
        Values = value.Select(ObjToValue).ToList();
        Value = Values.OfType<OldExpr>().ToList();
    }

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        if(Values.Count > 0)return this;
        foreach (var expr in Value)
            Values.Add(expr.Run(manager));
        return this;
    }

    public LangValueType Get(IntLangValue i)
    {
        if (i.Value < 0)
            i.Value = Values.Count + i.Value;
        return Values[i.Value];
    }

    public override string ToString() =>
        "[" + string.Join(", ", Values) + "]"; // Old8Lang 风格的列表，使用 [ ] 包裹

    public override LangValueType Dot(OldExpr dotExpr)
    {
        return dotExpr is not Instance a ? throw new InvalidOperationError(this, "列表类型只支持实例调用操作") : a.FromClassToResult(this);
    }

    public override object GetValue() => Apis.ListToObjects(Values);
    public IEnumerable<LangValueType> GetItems() => Values;

    public int GetLength() => Values.Count;

    public LangValueType Slice(int start, int end)
    {
        if (start < 0) start += Values.Count;
        if (end < 0) end += Values.Count + 1;
        return new ListLangValue(Values[start..end]
            .OfType<OldExpr>()
            .ToList());
    }

    public Type GetChildType() => typeof(object);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
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
            expr.LoadIlValue(ilGenerator, local);
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