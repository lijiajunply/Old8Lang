using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Min(selector) - 对列表元素应用选择器后求最小值
/// </summary>
public class LangListMinWithSelectorMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
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

    /// <summary>
    /// IL 模式：接收者是原生集合，selector 是编译好的 .NET 委托。
    /// 先把两者转换成 Old8Lang 值调用 helper，结果以 <see cref="LangValueType"/> 返回。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(LangListMinWithSelectorMethod).GetMethod(nameof(MinWithSelectorHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：对列表元素应用选择器后求最小值，返回选择器给出的那个值。
    /// 与 <see cref="ExecuteInternal"/> 语义一致。
    /// </summary>
    public static LangValueType MinWithSelectorHelper(LangValueType instance, LangValueType selector)
    {
        var items = GetIlItems(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("无法对空列表求最小值");
        }

        if (selector is not FuncLangValue func)
        {
            throw new ArgumentException("selector 参数必须是函数类型");
        }

        var manager = new VariateManager();
        var minValue = func.Run(manager, [items[0]]);

        for (int i = 1; i < items.Count; i++)
        {
            var currentValue = func.Run(manager, [items[i]]);

            if (CompareValues(currentValue, minValue) < 0)
            {
                minValue = currentValue;
            }
        }

        return minValue;
    }

    /// <summary>
    /// 与 <see cref="BaseLangListMethod.GetItems"/> 等价的静态版本（helper 必须是静态方法）。
    /// </summary>
    private static List<LangValueType> GetIlItems(LangValueType instance)
    {
        if (instance is ILangList langList)
        {
            return langList.GetItems().ToList();
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance.GetType().Name}");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
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
