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
/// List.IsSorted 方法 - 检查列表是否已排序
/// </summary>
public class ListIsSortedMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsSorted", "isSorted"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (list.Values.Count <= 1)
        {
            return new BoolLangValue(true);
        }

        for (int i = 0; i < list.Values.Count - 1; i++)
        {
            if (Less(list.Values[i + 1], list.Values[i]))
            {
                return new BoolLangValue(false);
            }
        }

        return new BoolLangValue(true);
    }

    /// <summary>
    /// 比较两个值的大小
    /// </summary>
    private static bool Less(LangValueType a, LangValueType b)
    {
        if (a is IntLangValue intA && b is IntLangValue intB)
        {
            return intA.Value < intB.Value;
        }
        if (a is DoubleLangValue doubleA && b is DoubleLangValue doubleB)
        {
            return doubleA.Value < doubleB.Value;
        }
        if (a is StringLangValue strA && b is StringLangValue strB)
        {
            return string.Compare(strA.Value, strB.Value, StringComparison.Ordinal) < 0;
        }
        if (a is CharLangValue charA && b is CharLangValue charB)
        {
            return charA.Value < charB.Value;
        }

        throw new InvalidOperationException($"无法比较类型 {a.GetType().Name} 和 {b.GetType().Name}");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListIsSortedMethod).GetMethod(nameof(IsSortedHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：检查是否已排序
    /// </summary>
    public static BoolLangValue IsSortedHelper(ListLangValue list)
    {
        if (list.Values.Count <= 1)
        {
            return new BoolLangValue(true);
        }

        for (int i = 0; i < list.Values.Count - 1; i++)
        {
            if (Less(list.Values[i + 1], list.Values[i]))
            {
                return new BoolLangValue(false);
            }
        }

        return new BoolLangValue(true);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (list.Count <= 1)
            {
                return true;
            }

            for (int i = 0; i < list.Count - 1; i++)
            {
                var current = list[i];
                var next = list[i + 1];

                if (current is IComparable comparable && next != null)
                {
                    if (comparable.CompareTo(next) > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
