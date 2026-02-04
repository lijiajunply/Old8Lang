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
/// ILangList.SelectMany(selector) - 将每个元素映射到一个列表，然后展平结果
/// </summary>
public class LangListSelectManyMethod : BaseLangListMethod
{
    public override string[] Names => ["SelectMany", "selectMany"];
    public override string[] ParameterNames => ["selector", "resultSelector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var selector = parameters[0].Run(manager) as FuncLangValue;

        if (selector == null)
        {
            throw new ArgumentError(position, "selector 参数必须是函数类型");
        }

        var result = new List<LangValueType>();

        // 如果有第二个参数（resultSelector）
        if (parameters.Count > 1)
        {
            var resultSelector = parameters[1].Run(manager) as FuncLangValue;
            if (resultSelector == null)
            {
                throw new ArgumentError(position, "resultSelector 参数必须是函数类型");
            }

            foreach (var item in items)
            {
                var collection = selector.Run(manager, [item]);
                IEnumerable<LangValueType> innerItems;

                if (IsLangList(collection))
                {
                    innerItems = GetItems(collection);
                }
                else
                {
                    innerItems = [collection];
                }

                foreach (var innerItem in innerItems)
                {
                    var combined = resultSelector.Run(manager, [item, innerItem]);
                    result.Add(combined);
                }
            }
        }
        else
        {
            // 只有一个参数（selector）
            foreach (var item in items)
            {
                var selected = selector.Run(manager, [item]);
                if (IsLangList(selected))
                {
                    result.AddRange(GetItems(selected));
                }
                else
                {
                    result.Add(selected);
                }
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("SelectMany 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var items = GetItemsForVM(instance);

        if (arguments.Length == 0)
        {
            throw new ArgumentException("需要至少一个参数");
        }

        var selector = arguments[0];
        var vm = VMContext.CurrentVM;
        var result = new List<object?>();

        // 如果有第二个参数（resultSelector）
        if (arguments.Length > 1)
        {
            var resultSelector = arguments[1];

            foreach (var item in items)
            {
                var collection = vm.CallFunctionObject(selector, [item]);
                IEnumerable<object?> innerItems;

                try
                {
                    innerItems = GetItemsForVM(collection);
                }
                catch
                {
                    innerItems = [collection];
                }

                foreach (var innerItem in innerItems)
                {
                    var combined = vm.CallFunctionObject(resultSelector, [item, innerItem]);
                    result.Add(combined);
                }
            }
        }
        else
        {
            // 只有一个参数（selector）
            foreach (var item in items)
            {
                var selected = vm.CallFunctionObject(selector, [item]);
                try
                {
                    var innerItems = GetItemsForVM(selected);
                    result.AddRange(innerItems);
                }
                catch
                {
                    result.Add(selected);
                }
            }
        }

        return result;
    }
}
