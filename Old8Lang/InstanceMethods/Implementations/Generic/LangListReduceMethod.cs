using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Reduce(reducer) - 归约
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListReduceMethod : BaseLangListMethod
{
    public override string[] Names => ["Reduce", "reduce"];
    public override string[] ParameterNames => ["reducer"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var reducerExpr = parameters[0].Run(manager);

        if (reducerExpr is not FuncLangValue reducer)
        {
            throw new ArgumentException("Reduce 方法的参数必须是函数");
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("序列不包含任何元素");
        }

        var accumulator = items[0];
        for (int i = 1; i < items.Count; i++)
        {
            var args = new List<LangExpression> { accumulator, items[i] };
            accumulator = reducer.Run(manager, args);
        }

        return accumulator;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持高阶函数
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
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
            throw new ArgumentException("Reduce 方法需要一个 reducer 参数");
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("序列不包含任何元素");
        }

        var reducer = arguments[0];
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("VM 上下文未初始化");
        }

        var accumulator = items[0];
        for (int i = 1; i < items.Count; i++)
        {
            accumulator = vm.CallFunctionObject(reducer, [accumulator, items[i]]);
        }

        return accumulator;
    }
}
