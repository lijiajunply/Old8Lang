using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Intersect 方法 - 返回两个列表的交集
/// </summary>
public class ListIntersectMethod : BaseInstanceMethod
{
    public override string[] Names => ["Intersect", "intersect"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["other"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var otherValue = parameters[0].Run(manager);

        if (otherValue is not ListLangValue other)
        {
            throw new TypeError(position, $"Intersect 方法的参数必须是列表类型，但实际是 {otherValue.GetType().Name}");
        }

        var result = new List<LangValueType>();

        // 找出在两个列表中都存在的元素
        foreach (var item in list.Values)
        {
            if (ContainsValue(other.Values, item) && !ContainsValue(result, item))
            {
                result.Add(item);
            }
        }

        return new ListLangValue(result);
    }

    /// <summary>
    /// 检查列表是否包含指定值
    /// </summary>
    private static bool ContainsValue(List<LangValueType> list, LangValueType value)
    {
        foreach (var item in list)
        {
            if (AreEqual(item, value))
            {
                return true;
            }
        }
        return false;
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

        // 加载另一个列表
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListIntersectMethod).GetMethod(nameof(IntersectHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：交集操作
    /// </summary>
    public static ListLangValue IntersectHelper(ListLangValue list, ListLangValue other)
    {
        var result = new List<LangValueType>();

        // 找出在两个列表中都存在的元素
        foreach (var item in list.Values)
        {
            if (ContainsValue(other.Values, item) && !ContainsValue(result, item))
            {
                result.Add(item);
            }
        }

        return new ListLangValue(result);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0 && arguments[0] is List<object?> other)
        {
            return list.Intersect(other).ToList();
        }

        throw new ArgumentException("实例和参数必须是 List<object?> 类型");
    }
}
