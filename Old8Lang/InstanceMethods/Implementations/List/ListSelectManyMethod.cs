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

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.SelectMany(selector) - 将每个元素映射到一个列表，然后展平结果
/// </summary>
public class ListSelectManyMethod : BaseInstanceMethod
{
    public override string[] Names => ["SelectMany", "selectMany"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["selector", "resultSelector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
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

            foreach (var item in list.Values)
            {
                var collection = selector.Run(manager, [item]);
                IEnumerable<LangValueType> innerItems;

                if (collection is ListLangValue innerList)
                {
                    innerItems = innerList.Values;
                }
                else if (collection is ArrayLangValue innerArray)
                {
                    innerItems = innerArray.RunResult;
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
            foreach (var item in list.Values)
            {
                var selected = selector.Run(manager, [item]);
                if (selected is ListLangValue innerList)
                {
                    result.AddRange(innerList.Values);
                }
                else if (selected is ArrayLangValue innerArray)
                {
                    result.AddRange(innerArray.RunResult);
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
        throw new NotSupportedException("List.SelectMany 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 支持 List<object?> 和 object?[] 两种类型
        List<object?> list;
        if (instance is List<object?> listInstance)
        {
            list = listInstance;
        }
        else if (instance is object?[] arrayInstance)
        {
            list = arrayInstance.ToList();
        }
        else
        {
            throw new ArgumentException("实例必须是 List<object?> 或 object?[] 类型");
        }

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

            foreach (var item in list)
            {
                var collection = vm.CallFunctionObject(selector, [item]);
                IEnumerable<object?> innerItems;

                if (collection is List<object?> innerList)
                {
                    innerItems = innerList;
                }
                else if (collection is object?[] innerArray)
                {
                    innerItems = innerArray;
                }
                else
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
            foreach (var item in list)
            {
                var selected = vm.CallFunctionObject(selector, [item]);
                if (selected is List<object?> innerList)
                {
                    result.AddRange(innerList);
                }
                else if (selected is object?[] innerArray)
                {
                    result.AddRange(innerArray);
                }
                else
                {
                    result.Add(selected);
                }
            }
        }

        return result;
    }
}
