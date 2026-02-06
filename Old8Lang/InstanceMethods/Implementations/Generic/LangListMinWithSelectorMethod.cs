using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Min(selector) - 对列表元素应用选择器后求最小值
/// </summary>
public class LangListMinWithSelectorMethod : BaseLangListMethod
{
    public override string[] Names => ["Min", "min"];
    public override string[] ParameterNames => ["selector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    /// <summary>
    /// 参数类型：selector 必须是函数
    /// </summary>
    public override Type?[]? ParameterTypes => [typeof(FuncLangValue)];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "对列表元素应用选择器后求最小值";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationError(position, "无法对空列表求最小值");
        }

        var selector = parameters[0].Run(manager) as FuncLangValue;
        if (selector == null)
        {
            throw new ArgumentError(position, "selector 参数必须是函数类型");
        }

        var minValue = selector.Run(manager, [items[0]]);

        for (int i = 1; i < items.Count; i++)
        {
            var currentValue = selector.Run(manager, [items[i]]);

            if (CompareValues(currentValue, minValue) < 0)
            {
                minValue = currentValue;
            }
        }

        return minValue;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Min(selector) 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var items = GetItemsForVM(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("无法对空列表求最小值");
        }

        if (arguments.Length == 0)
        {
            throw new ArgumentException("需要一个选择器参数");
        }

        var selector = arguments[0];
        var vm = VMContext.CurrentVM;

        var minValue = vm.CallFunctionObject(selector, [items[0]]);

        for (int i = 1; i < items.Count; i++)
        {
            var currentValue = vm.CallFunctionObject(selector, [items[i]]);

            if (CompareValuesVM(currentValue, minValue) < 0)
            {
                minValue = currentValue;
            }
        }

        return minValue;
    }

    /// <summary>
    /// 比较两个值的大小（解释器模式）
    /// </summary>
    private static int CompareValues(LangValueType a, LangValueType b)
    {
        return (a, b) switch
        {
            (IntLangValue ia, IntLangValue ib) => ia.Value.CompareTo(ib.Value),
            (DoubleLangValue da, DoubleLangValue db) => da.Value.CompareTo(db.Value),
            (StringLangValue sa, StringLangValue sb) => string.Compare(sa.Value, sb.Value, StringComparison.Ordinal),
            (BoolLangValue ba, BoolLangValue bb) => ba.Value.CompareTo(bb.Value),
            (CharLangValue ca, CharLangValue cb) => ca.Value.CompareTo(cb.Value),
            _ => string.Compare(a.ToDisplayString(), b.ToDisplayString(), StringComparison.Ordinal)
        };
    }

    /// <summary>
    /// 比较两个值的大小（VM 模式）
    /// </summary>
    private static int CompareValuesVM(object? a, object? b)
    {
        return (a, b) switch
        {
            (int ia, int ib) => ia.CompareTo(ib),
            (double da, double db) => da.CompareTo(db),
            (string sa, string sb) => string.Compare(sa, sb, StringComparison.Ordinal),
            (bool ba, bool bb) => ba.CompareTo(bb),
            (char ca, char cb) => ca.CompareTo(cb),
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal)
        };
    }
}
