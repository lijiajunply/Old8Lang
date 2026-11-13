using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Skip(count) - 跳过前 N 个元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListSkipMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Skip", "skip"];
    public override string[] ParameterNames => ["count"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var countValue = parameters[0].Run(manager);

        if (countValue is not IntLangValue count)
        {
            throw new ArgumentException("Skip 方法的参数必须是整数");
        }

        var skippedItems = items.Skip(count.Value).ToList();
        return new ListLangValue(skippedItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperName = TargetType == typeof(ArrayLangValue) ? nameof(SkipArrayHelper) : nameof(SkipHelper);
        var helperMethod = typeof(LangListSkipMethod).GetMethod(helperName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>IL 模式的辅助方法：跳过桥接后集合的前 N 项并返回原生列表。</summary>
    public static List<object?> SkipHelper(LangValueType instance, LangValueType count)
    {
        if (instance is not ILangList langList || count is not IntLangValue integer)
        {
            throw new ArgumentException("Skip 方法需要集合和整数参数");
        }

        return langList.GetItems().Skip(integer.Value).Select(IlValueBridge.Unwrap).ToList();
    }

    /// <summary>IL 模式下数组 Skip 并保持数组返回表示。</summary>
    public static object?[] SkipArrayHelper(LangValueType instance, LangValueType count) =>
        SkipHelper(instance, count).ToArray();

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return TargetType == typeof(ArrayLangValue) ? typeof(object?[]) : typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList && arguments[0] is int count)
        {
            return new ListLangValue(SkipHelper((LangValueType)langList, new IntLangValue(count))
                .Select(IlValueBridge.Wrap).ToList());
        }

        if (instance is List<object?> list && arguments[0] is int cnt)
        {
            return list.Skip(cnt).ToList();
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或为 List<object?> 类型，当前类型：{instance?.GetType().Name}");
    }
}
