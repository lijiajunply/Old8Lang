using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Sum 方法 - 计算列表中所有数值元素的总和
/// </summary>
public class ListSumMethod : BaseInstanceMethod
{
    public override string[] Names => ["Sum", "sum"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (list.Values.Count == 0)
        {
            return new IntLangValue(0);
        }

        // 检查第一个元素的类型来决定返回类型
        var firstElement = list.Values[0];
        bool isDouble = firstElement is DoubleLangValue;

        if (isDouble)
        {
            double sum = 0.0;
            foreach (var item in list.Values)
            {
                if (item is DoubleLangValue doubleValue)
                {
                    sum += doubleValue.Value;
                }
                else if (item is IntLangValue intValue)
                {
                    sum += intValue.Value;
                }
                else
                {
                    throw new Error.TypeError(position, $"Sum 方法只能用于数值类型的列表，但包含 {item.GetType().Name} 类型");
                }
            }
            return new DoubleLangValue(sum);
        }
        else
        {
            int sum = 0;
            foreach (var item in list.Values)
            {
                if (item is IntLangValue intValue)
                {
                    sum += intValue.Value;
                }
                else if (item is DoubleLangValue doubleValue)
                {
                    // 如果遇到 double，转换为 double 计算
                    double doubleSum = sum + doubleValue.Value;
                    for (int i = list.Values.IndexOf(item) + 1; i < list.Values.Count; i++)
                    {
                        if (list.Values[i] is IntLangValue iv)
                        {
                            doubleSum += iv.Value;
                        }
                        else if (list.Values[i] is DoubleLangValue dv)
                        {
                            doubleSum += dv.Value;
                        }
                    }
                    return new DoubleLangValue(doubleSum);
                }
                else
                {
                    throw new Error.TypeError(position, $"Sum 方法只能用于数值类型的列表，但包含 {item.GetType().Name} 类型");
                }
            }
            return new IntLangValue(sum);
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var sumHelperMethod = typeof(ListSumMethod).GetMethod(nameof(SumHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, sumHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：计算总和
    /// </summary>
    public static LangValueType SumHelper(ListLangValue list)
    {
        if (list.Values.Count == 0)
        {
            return new IntLangValue(0);
        }

        var firstElement = list.Values[0];
        bool isDouble = firstElement is DoubleLangValue;

        if (isDouble)
        {
            double sum = 0.0;
            foreach (var item in list.Values)
            {
                if (item is DoubleLangValue doubleValue)
                {
                    sum += doubleValue.Value;
                }
                else if (item is IntLangValue intValue)
                {
                    sum += intValue.Value;
                }
            }
            return new DoubleLangValue(sum);
        }
        else
        {
            int sum = 0;
            bool hasDouble = false;
            double doubleSum = 0.0;

            foreach (var item in list.Values)
            {
                if (item is IntLangValue intValue)
                {
                    if (hasDouble)
                    {
                        doubleSum += intValue.Value;
                    }
                    else
                    {
                        sum += intValue.Value;
                    }
                }
                else if (item is DoubleLangValue doubleValue)
                {
                    if (!hasDouble)
                    {
                        doubleSum = sum + doubleValue.Value;
                        hasDouble = true;
                    }
                    else
                    {
                        doubleSum += doubleValue.Value;
                    }
                }
            }

            return hasDouble ? new DoubleLangValue(doubleSum) : new IntLangValue(sum);
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (list.Count == 0)
            {
                return 0;
            }

            double sum = 0.0;
            foreach (var item in list)
            {
                if (item is int intValue)
                {
                    sum += intValue;
                }
                else if (item is double doubleValue)
                {
                    sum += doubleValue;
                }
            }

            return sum;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
