using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Min 方法 - 获取列表中的最小值
/// </summary>
public class ListMinMethod : BaseInstanceMethod
{
    public override string[] Names => ["Min", "min"];
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
            throw new Error.InvalidOperationError(position, "无法获取空列表的最小值");
        }

        var min = list.Values[0];
        for (int i = 1; i < list.Values.Count; i++)
        {
            if (list.Values[i].Less(min))
            {
                min = list.Values[i];
            }
        }

        return min;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var minHelperMethod = typeof(ListMinMethod).GetMethod(nameof(MinHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, minHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取最小值
    /// </summary>
    public static LangValueType MinHelper(ListLangValue list)
    {
        if (list.Values.Count == 0)
        {
            throw new Exception("无法获取空列表的最小值");
        }

        var min = list.Values[0];
        for (int i = 1; i < list.Values.Count; i++)
        {
            if (list.Values[i].Less(min))
            {
                min = list.Values[i];
            }
        }

        return min;
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
                throw new InvalidOperationException("无法获取空列表的最小值");
            }

            var min = list[0];
            for (int i = 1; i < list.Count; i++)
            {
                if (Comparer<object?>.Default.Compare(list[i], min) < 0)
                {
                    min = list[i];
                }
            }

            return min;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
