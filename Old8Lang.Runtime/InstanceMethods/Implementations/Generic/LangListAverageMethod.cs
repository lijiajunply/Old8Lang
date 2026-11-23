using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Average() - 平均值
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListAverageMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Average", "average", "Avg", "avg"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationError(instance, "序列不包含任何元素");
        }

        double sum = 0;
        foreach (var item in items)
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
                throw new TypeError(item, "数值类型", item.GetType().Name);
            }
        }

        return DoubleLangValue.Create(sum / items.Count, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(LangListAverageMethod).GetMethod(nameof(AverageNativeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>IL 模式的辅助方法：计算桥接后集合的平均值。</summary>
    public static double AverageNativeHelper(LangValueType instance)
    {
        if (instance is not ILangList langList)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        return AverageHelper(langList);
    }

    public static double AverageHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        if (items.Count == 0)
        {
            throw new InvalidOperationException("序列不包含任何元素");
        }

        double sum = 0;
        foreach (var item in items)
        {
            if (item is IntLangValue iv)
                sum += iv.Value;
            else if (item is DoubleLangValue dv)
                sum += dv.Value;
        }

        return sum / items.Count;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(double);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var list = new ListLangValue(GetItemsForVM(instance).Select(IlValueBridge.Wrap).ToList());
        return IlValueBridge.Unwrap(ExecuteInternal(list, [], new VariateManager(), default));
    }
}
