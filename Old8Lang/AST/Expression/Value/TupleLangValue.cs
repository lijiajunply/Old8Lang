using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 元组
/// </summary>
/// <param name="v1"></param>
/// <param name="v2"></param>
/// <param name="position"></param>
public class TupleLangValue(OldExpr v1, OldExpr v2, SourcePosition position = default) : LangValueType(position)
{
    public readonly OldExpr Item1 = v1;
    public readonly OldExpr Item2 = v2;
    public ValueTuple<LangValueType, LangValueType> Value { get; private set; }

    public override LangValueType Run(VariateManager manager)
    {
        // 运行第一个元素
        var item1Result = Item1.Run(manager);
        
        // 运行第二个元素，处理空名称的特殊情况
        LangValueType item2Result;
        if (Item2 is LangId item2Id && string.IsNullOrEmpty(item2Id.IdName))
        {
            // 如果第二个元素是空名称的LangId，直接使用NullLangValue，避免NameError
            item2Result = new NullLangValue();
        }
        else
        {
            // 正常运行第二个元素
            item2Result = Item2.Run(manager);
        }
        
        Value = (item1Result, item2Result);
        return this;
    }

    public override string ToString() => Value is (null, null) ? $"({Item1},{Item2})" : $"({Value.Item1},{Value.Item2})";
    public override object GetValue() => (Value.Item1.GetValue(), Value.Item2.GetValue());

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取两个元素的类型
        var type1 = Item1.OutputType(local) ?? typeof(object);
        var type2 = Item2.OutputType(local) ?? typeof(object);
        
        // 获取元组类型
        var tupleType = typeof(ValueTuple<,>).MakeGenericType(type1, type2);
        
        // 获取元组构造函数
        var constructor = tupleType.GetConstructor(new[] { type1, type2 })!;
        
        // 加载第一个元素的值
        Item1.LoadIlValue(ilGenerator, local);
        
        // 加载第二个元素的值
        Item2.LoadIlValue(ilGenerator, local);
        
        // 调用元组构造函数创建元组实例
        ilGenerator.Emit(OpCodes.Newobj, constructor);
    }

    public override Type OutputType(LocalManager local)
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