using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Expression.Value;

public class TupleValue(OldExpr v1, OldExpr v2, SourcePosition position = default) : ValueType(position)
{
    public readonly OldExpr Item1 = v1;
    public readonly OldExpr Item2 = v2;
    public ValueTuple<ValueType, ValueType> Value { get; private set; }

    public override ValueType Run(LangParser.VariateManager manager)
    {
        Value = (Item1.Run(manager), Item2.Run(manager));
        return this;
    }

    public override string ToString() => Value is (null, null) ? $"({Item1},{Item2})" : $"({Value.Item1},{Value.Item2})";
    public override object GetValue() => (Value.Item1.GetValue(), Value.Item2.GetValue());

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载第一个元素的值
        Item1.LoadIlValue(ilGenerator, local);
        // 加载第二个元素的值
        Item2.LoadIlValue(ilGenerator, local);
        // 元组将自动由堆栈上的两个值组成
    }

    public override Type? OutputType(LocalManager local)
    {
        // 获取两个元素的类型
        var type1 = Item1.OutputType(local);
        var type2 = Item2.OutputType(local);
        
        // 确保类型不为空
        if (type1 == null || type2 == null)
        {
            return typeof(ValueTuple<object, object>);
        }
        
        // 返回对应的元组类型
        return typeof(ValueTuple<,>).MakeGenericType(type1, type2);
    }
}