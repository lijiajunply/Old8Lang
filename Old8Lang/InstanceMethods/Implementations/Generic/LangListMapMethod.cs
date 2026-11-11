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
/// ILangList.Map(mapper) - 映射列表元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListMapMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Map", "map", "Select", "select"];
    public override string[] ParameterNames => ["mapper"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var mapperExpr = parameters[0].Run(manager);

        if (mapperExpr is not FuncLangValue mapper)
        {
            throw new ArgumentException("Map 方法的参数必须是函数");
        }

        var mappedItems = new List<LangValueType>();
        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var result = mapper.Run(manager, args);
            mappedItems.Add(result);
        }

        return new ListLangValue(mappedItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
        ilGenerator.Emit(OpCodes.Call, typeof(LangListMapMethod).GetMethod(nameof(MapHelper))!);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    /// <summary>
    /// IL 模式下映射原生集合并返回原生列表。
    /// </summary>
    public static List<object?> MapHelper(LangValueType instance, LangValueType mapper)
    {
        if (instance is not ILangList list)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        if (mapper is not FuncLangValue function)
        {
            throw new ArgumentException("Map 方法的参数必须是函数");
        }

        var manager = new VariateManager();
        return list.GetItems()
            .Select(item => IlValueBridge.Unwrap(function.Run(manager, [item])))
            .ToList();
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
            throw new ArgumentException("Map 方法需要一个 mapper 参数");
        }

        var mapper = arguments[0];
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("VM 上下文未初始化");
        }

        var mappedItems = new List<object?>();
        foreach (var item in items)
        {
            var result = vm.CallFunctionObject(mapper, [item]);
            mappedItems.Add(result);
        }

        return mappedItems;
    }
}
