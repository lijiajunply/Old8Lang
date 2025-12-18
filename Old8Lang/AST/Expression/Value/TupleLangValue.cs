using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.LangParser;
using Old8Lang.Error;
using System.Linq;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 元组
/// </summary>
/// <param name="v1"></param>
/// <param name="v2"></param>
/// <param name="position"></param>
public class TupleLangValue(LangExpression v1, LangExpression v2, SourcePosition position = default) : LangValueType(position)
{
    public readonly LangExpression V1 = v1;
    public readonly LangExpression V2 = v2;

    public ValueTuple<LangValueType, LangValueType> Value { get; private set; }

    public override LangValueType Run(VariateManager manager)
    {
        // 运行第一个元素
        var item1Result = V1.Run(manager);

        // 运行第二个元素，处理空名称的特殊情况
        LangValueType item2Result;
        if (V2 is LangId item2Id && string.IsNullOrEmpty(item2Id.IdName))
        {
            // 如果第二个元素是空名称的LangId，直接使用NullLangValue，避免NameError
            item2Result = NullLangValue.Instance;
        }
        else
        {
            // 正常运行第二个元素
            item2Result = V2.Run(manager);
        }

        Value = (item1Result, item2Result);
        return this;
    }

    public override string ToString() => $"({V1},{V2})";
    public override object GetValue() => (Value.Item1.GetValue(), Value.Item2.GetValue());

    // 支持多元元组的构造函数
    public TupleLangValue(List<LangExpression> elements, SourcePosition position = default) : this(elements[0], elements[1], position)
    {
        // 如果是多元元组，递归构建嵌套结构
        for (int i = 2; i < elements.Count; i++)
        {
            V2 = new TupleLangValue(V2, elements[i], position);
        }
    }

    /// <summary>
    /// 获取元组指定索引的元素
    /// </summary>
    /// <param name="index">索引值</param>
    /// <returns>指定索引的元素</returns>
    public LangValueType Get(IntLangValue index)
    {
        return Get(index.Value);
    }

    /// <summary>
    /// 获取元组指定索引的元素（支持嵌套多元元组）
    /// </summary>
    /// <param name="index">索引值</param>
    /// <returns>指定索引的元素</returns>
    public LangValueType Get(int index)
    {
        if (index == 0)
            return Value.Item1;

        if (index == 1)
        {
            // 如果第二个元素是嵌套元组，返回其第一个元素
            if (Value.Item2 is TupleLangValue secondNested)
            {
                return secondNested.Get(0);
            }
            return Value.Item2;
        }

        // 对于索引大于1的情况，需要在第二个元素的嵌套元组中查找
        if (Value.Item2 is TupleLangValue nestedTuple)
        {
            return nestedTuple.Get(index - 1);
        }

        throw new InvalidOperationError(this, $"元组索引越界: {index}，当前元组只支持索引 0、1");
    }

    /// <summary>
    /// 支持元组的点操作访问，包括索引访问
    /// </summary>
    /// <param name="dotExpression">点操作表达式</param>
    /// <returns>访问结果</returns>
    public override LangValueType Dot(LangExpression dotExpression)
    {
        // 支持数字索引访问：tuple.0, tuple.1, tuple.2 等
        if (dotExpression is LangId id)
        {
            if (int.TryParse(id.IdName, out int index))
            {
                return Get(index);
            }

            // 支持 ToStr() 方法
            if (id.IdName == "ToStr")
            {
                return new StringLangValue(ToString());
            }

            // 支持 Length 属性
            if (id.IdName == "Length")
            {
                // 计算元组的长度（对于嵌套元组，需要计算所有元素的总数）
                int length = GetTupleLength(this);
                return new IntLangValue(length);
            }
        }

        // 如果是Instance，检查是否是ToStr()调用
        if (dotExpression is Instance instance &&
            instance.Ids.Count == 0)
        {
            if (instance.Id.IdName == "ToStr")
            {
                return new StringLangValue(ToString());
            }

            if (instance.Id.IdName == "Length")
            {
                // 计算元组的长度（对于嵌套元组，需要计算所有元素的总数）
                int length = GetTupleLength(this);
                return new IntLangValue(length);
            }
        }

        throw new InvalidOperationError(this, $"不支持元组的点操作: {dotExpression}");
    }

    /// <summary>
    /// 计算元组的长度（支持嵌套多元元组）
    /// </summary>
    /// <param name="tuple">要计算长度的元组</param>
    /// <returns>元组中元素的总数</returns>
    private static int GetTupleLength(TupleLangValue tuple)
    {
        // 如果第二个元素是嵌套元组，则递归计算长度
        if (tuple.Value.Item2 is TupleLangValue lengthTuple)
        {
            return 1 + GetTupleLength(lengthTuple);
        }
        return 2; // 二元元组固定为2个元素
    }

    // 覆盖 Equal 方法以支持元组深度比较
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is not TupleLangValue otherTuple)
            return false;

        // 比较两个元素是否都相等
        return Value.Item1.Equal(otherTuple.Value.Item1) &&
               Value.Item2.Equal(otherTuple.Value.Item2);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取两个元素的类型
        var type1 = V1.OutputType(local) ?? typeof(object);
        var type2 = V2.OutputType(local) ?? typeof(object);
        
        // 获取元组类型
        var tupleType = typeof(ValueTuple<,>).MakeGenericType(type1, type2);
        
        // 获取元组构造函数
        var constructor = tupleType.GetConstructor([type1, type2])!;
        
        // 加载第一个元素的值
        V1.LoadIlValue(ilGenerator, local);
        
        // 加载第二个元素的值
        V2.LoadIlValue(ilGenerator, local);
        
        // 调用元组构造函数创建元组实例
        ilGenerator.Emit(OpCodes.Newobj, constructor);
    }

    public override Type OutputType(LocalManager local)
    {
        // 获取两个元素的类型
        var type1 = V1.OutputType(local);
        var type2 = V2.OutputType(local);
        
        // 确保类型不为空
        if (type1 == null || type2 == null)
        {
            return typeof(ValueTuple<object, object>);
        }
        
        // 返回对应的元组类型
        return typeof(ValueTuple<,>).MakeGenericType(type1, type2);
    }
}