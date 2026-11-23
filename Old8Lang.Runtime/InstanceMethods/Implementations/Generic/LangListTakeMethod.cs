using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Take(count) - 取前 N 个元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListTakeMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Take", "take"];
    public override string[] ParameterNames => ["count"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var countValue = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));

        if (countValue is not IntLangValue count)
        {
            throw new ArgumentException("Take 方法的参数必须是整数");
        }

        var takenItems = items.Take(count.Value).ToList();
        return new ListLangValue(takenItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperName = TargetType == typeof(ArrayLangValue) ? nameof(TakeArrayHelper) : nameof(TakeHelper);
        var helperMethod = typeof(LangListTakeMethod).GetMethod(helperName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>IL 模式的辅助方法：取桥接后集合的前 N 项并返回原生列表。</summary>
    public static List<object?> TakeHelper(LangValueType instance, LangValueType count)
    {
        if (instance is not ILangList langList || count is not IntLangValue integer)
        {
            throw new ArgumentException("Take 方法需要集合和整数参数");
        }

        return langList.GetItems().Take(integer.Value).Select(IlValueBridge.Unwrap).ToList();
    }

    /// <summary>IL 模式下数组 Take 并保持数组返回表示。</summary>
    public static object?[] TakeArrayHelper(LangValueType instance, LangValueType count) =>
        TakeHelper(instance, count).ToArray();

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return TargetType == typeof(ArrayLangValue) ? typeof(object?[]) : typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (arguments[0] is not int count)
            throw new ArgumentException("Take 的参数必须是整数");
        return GetItemsForVM(instance).Take(count).ToList();
    }
}
