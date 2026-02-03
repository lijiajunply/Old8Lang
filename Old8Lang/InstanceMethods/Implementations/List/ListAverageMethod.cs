using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Average 方法 - 计算列表中所有数值元素的平均值
/// </summary>
public class ListAverageMethod : BaseInstanceMethod
{
    public override string[] Names => ["Average", "average"];
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
            throw new Error.InvalidOperationError(position, "无法计算空列表的平均值");
        }

        double sum = 0.0;
        foreach (var item in list.Values)
        {
            if (item is IntLangValue intValue)
            {
                sum += intValue.Value;
            }
            else if (item is DoubleLangValue doubleValue)
            {
                sum += doubleValue.Value;
            }
            else
            {
                throw new Error.TypeError(position, $"Average 方法只能用于数值类型的列表，但包含 {item.GetType().Name} 类型");
            }
        }

        return new DoubleLangValue(sum / list.Values.Count);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var averageHelperMethod = typeof(ListAverageMethod).GetMethod(nameof(AverageHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, averageHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：计算平均值
    /// </summary>
    public static DoubleLangValue AverageHelper(ListLangValue list)
    {
        if (list.Values.Count == 0)
        {
            throw new Exception("无法计算空列表的平均值");
        }

        double sum = 0.0;
        foreach (var item in list.Values)
        {
            if (item is IntLangValue intValue)
            {
                sum += intValue.Value;
            }
            else if (item is DoubleLangValue doubleValue)
            {
                sum += doubleValue.Value;
            }
        }

        return new DoubleLangValue(sum / list.Values.Count);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(DoubleLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("无法计算空列表的平均值");
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

            return sum / list.Count;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
