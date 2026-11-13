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
/// ILangList.Filter(predicate) - 过滤列表元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListFilterMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Filter", "filter", "Where", "where"];
    public override string[] ParameterNames => ["predicate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var predicateExpr = parameters[0].Accept(new InterpreterVisitor(manager));

        if (predicateExpr is not FuncLangValue predicate)
        {
            throw new ArgumentException("Filter 方法的参数必须是函数");
        }

        var filteredItems = new List<LangValueType>();
        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var result = predicate.Run(manager, args);

            if (result is BoolLangValue { Value: true })
            {
                filteredItems.Add(item);
            }
        }

        return new ListLangValue(filteredItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
        var helperName = TargetType == typeof(ArrayLangValue) ? nameof(FilterArrayHelper) : nameof(FilterHelper);
        ilGenerator.Emit(OpCodes.Call, typeof(LangListFilterMethod).GetMethod(helperName)!);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return TargetType == typeof(ArrayLangValue) ? typeof(object?[]) : typeof(List<object?>);
    }

    /// <summary>
    /// IL 模式下过滤原生集合并返回原生列表。
    /// </summary>
    public static List<object?> FilterHelper(LangValueType instance, LangValueType predicate)
    {
        if (instance is not ILangList list)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        if (predicate is not FuncLangValue function)
        {
            throw new ArgumentException("Filter 方法的参数必须是函数");
        }

        var manager = new VariateManager();
        var result = new List<object?>();
        foreach (var item in list.GetItems())
        {
            if (function.Run(manager, [item]) is BoolLangValue { Value: true })
            {
                result.Add(IlValueBridge.Unwrap(item));
            }
        }

        return result;
    }

    /// <summary>IL 模式下过滤数组并保持数组返回表示。</summary>
    public static object?[] FilterArrayHelper(LangValueType instance, LangValueType predicate)
    {
        return FilterHelper(instance, predicate).ToArray();
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 获取集合元素
        List<object?> items;
        if (instance is ILangList langList)
        {
            items = langList.GetItems().Cast<object?>().ToList();
        }
        else if (instance is System.Collections.IList list)
        {
            items = list.Cast<object?>().ToList();
        }
        else
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
        }

        if (arguments.Length == 0)
        {
            throw new ArgumentException("Filter 方法需要一个 predicate 参数");
        }

        var predicate = arguments[0];
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("VM 上下文未初始化");
        }

        var filteredItems = new List<object?>();
        foreach (var item in items)
        {
            var result = vm.CallFunctionObject(predicate, [item]);

            // 检查结果是否为 true
            if (result is bool boolResult && boolResult)
            {
                filteredItems.Add(item);
            }
        }

        return filteredItems;
    }
}
