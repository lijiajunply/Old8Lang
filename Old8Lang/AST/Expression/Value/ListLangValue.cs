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
    
    public ListLangValue(List<LangValueType> value, SourcePosition position = default) : base(position)
    {
        Values = [.. value];
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

    public void Set(LangValueType index, LangValueType value)
    {
        if (index is IntLangValue i)
        {
            if (i.Value < 0)
                i.Value = Values.Count + i.Value;
            if (i.Value < 0 || i.Value >= Values.Count)
                throw new IndexError(this, i.Value, Values.Count);
            
            // 类型检查：确保添加的元素类型与列表中已有的元素类型一致
            if (Values.Count > 0)
            {
                var existingType = Values[0].TypeToString().ToLower();
                var newValueType = value.TypeToString().ToLower();
                
                if (existingType != newValueType)
                {
                    throw new TypeError(this, existingType, newValueType, "列表元素类型必须一致");
                }
            }
            
            Values[i.Value] = value;
        }
        else
        {
            throw new TypeError(this, "IntValue", index.GetType().Name);
        }
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

    public Type GetChildType()
    {
        // 确定列表元素的实际类型
        if (Values.Count == 0 && Value.Count > 0)
        {
            // 如果Values为空但Value有元素，尝试从Value中推导类型
            var firstType = Value[0].OutputType(null!);
            // 检查所有元素是否为同一类型
            foreach (var expr in Value)
            {
                var exprType = expr.OutputType(null!);
                if (exprType != firstType)
                {
                    return typeof(object);
                }
            }
            return firstType;
        }
        else if (Values.Count > 0)
        {
            // 如果Values不为空，从Values中推导类型
            var firstType = Values[0].GetType();
            // 检查所有元素是否为同一类型
            foreach (var value in Values)
            {
                if (value.GetType() != firstType)
                {
                    return typeof(object);
                }
            }
            return firstType;
        }
        return typeof(object);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 确定列表元素的类型
        var itemType = typeof(object);
        if (Value.Count > 0)
        {
            itemType = Value[0].OutputType(local);
            // 检查所有元素是否为同一类型
            foreach (var expr in Value)
            {
                var exprType = expr.OutputType(local);
                if (exprType != itemType)
                {
                    itemType = typeof(object);
                    break;
                }
            }
        }
        else if (Values.Count > 0)
        {
            itemType = Values[0].OutputType(local);
            // 检查所有元素是否为同一类型
            foreach (var value in Values)
            {
                var valueType = value.OutputType(local);
                if (valueType != itemType)
                {
                    itemType = typeof(object);
                    break;
                }
            }
        }
        
        var listType = typeof(List<>).MakeGenericType(itemType);
        var listConstructor = listType.GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor); // 创建泛型List实例
        if (Value.Count == 0) return;
        
        var l = ilGenerator.DeclareLocal(listType);
        ilGenerator.Emit(OpCodes.Stloc, l.LocalIndex);

        // 向List中添加元素
        var addMethod = listType.GetMethod("Add", [itemType])!;
        foreach (var expr in Value)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
            expr.LoadIlValue(ilGenerator, local);
            var t = expr.OutputType(local);
            if (t != itemType)
            {
                ilGenerator.Emit(OpCodes.Box, t!);
            }
            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用Add方法
        }
        ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
    }

    public override Type OutputType(LocalManager local)
    {
        // 确定列表元素的类型
        var itemType = typeof(object);
        if (Value.Count > 0)
        {
            itemType = Value[0].OutputType(local);
            // 检查所有元素是否为同一类型
            foreach (var expr in Value)
            {
                var exprType = expr.OutputType(local);
                if (exprType != itemType)
                {
                    itemType = typeof(object);
                    break;
                }
            }
        }
        else if (Values.Count > 0)
        {
            itemType = Values[0].OutputType(local);
            // 检查所有元素是否为同一类型
            foreach (var value in Values)
            {
                var valueType = value.OutputType(local);
                if (valueType != itemType)
                {
                    itemType = typeof(object);
                    break;
                }
            }
        }
        
        // 返回泛型List类型
        return typeof(List<>).MakeGenericType(itemType);
    }
    
    public override LangValueType Converse(LangValueType otherLangValueType, LangParser.VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value) 
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        switch (value.Value)
        {
            case "List" or "list":
                // 已经是列表，直接返回
                return this;
            case "Array" or "array":
                // 列表转换为数组
                return new ArrayLangValue(Values, Position);
            case "String" or "string":
                // 列表转换为字符串，用逗号分隔
                return new StringLangValue(string.Join(", ", Values.Select(v => v.ToDisplayString())));
            default:
                throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}");
        }
    }
}