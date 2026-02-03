using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Distinct 方法 - 去除列表中的重复元素
/// </summary>
public class ListDistinctMethod : BaseInstanceMethod
{
    public override string[] Names => ["Distinct", "distinct"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var distinct = new List<LangValueType>();

        foreach (var item in list.Values)
        {
            bool found = false;
            foreach (var existingItem in distinct)
            {
                if (AreEqual(item, existingItem))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                distinct.Add(item);
            }
        }

        return new ListLangValue(distinct);
    }

    /// <summary>
    /// 比较两个值是否相等
    /// </summary>
    private static bool AreEqual(LangValueType a, LangValueType b)
    {
        if (a.GetType() != b.GetType())
        {
            return false;
        }

        if (a is IntLangValue intA && b is IntLangValue intB)
        {
            return intA.Value == intB.Value;
        }
        if (a is DoubleLangValue doubleA && b is DoubleLangValue doubleB)
        {
            return Math.Abs(doubleA.Value - doubleB.Value) < 0.0000001;
        }
        if (a is StringLangValue strA && b is StringLangValue strB)
        {
            return strA.Value == strB.Value;
        }
        if (a is BoolLangValue boolA && b is BoolLangValue boolB)
        {
            return boolA.Value == boolB.Value;
        }
        if (a is CharLangValue charA && b is CharLangValue charB)
        {
            return charA.Value == charB.Value;
        }

        return a.Equals(b);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var distinctHelperMethod = typeof(ListDistinctMethod).GetMethod(nameof(DistinctHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, distinctHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：去重
    /// </summary>
    public static ListLangValue DistinctHelper(ListLangValue list)
    {
        var distinct = new List<LangValueType>();

        foreach (var item in list.Values)
        {
            bool found = false;
            foreach (var existingItem in distinct)
            {
                if (AreEqual(item, existingItem))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                distinct.Add(item);
            }
        }

        return new ListLangValue(distinct);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            return list.Distinct().ToList();
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
