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
public class LangListSelectManyMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
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

    /// <summary>
    /// IL 模式：接收者是原生集合，selector / resultSelector 是编译好的 .NET 委托。
    /// 按实际参数个数生成加载代码，缺少 resultSelector 时压入 null。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        if (parameters.Count > 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[1], ilGenerator, local);
        }
        else
        {
            ilGenerator.Emit(OpCodes.Ldnull);
        }

        var helperMethod = typeof(LangListSelectManyMethod).GetMethod(nameof(SelectManyHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：把每个元素映射成列表后展平，返回原生列表。
    /// 与 <see cref="ExecuteInternal"/> 语义一致（可选 resultSelector）。
    /// </summary>
    public static List<object?> SelectManyHelper(LangValueType instance, LangValueType selector,
        LangValueType? resultSelector = null)
    {
        var items = GetIlItems(instance);

        if (selector is not FuncLangValue func)
        {
            throw new ArgumentException("selector 参数必须是函数类型");
        }

        var manager = new VariateManager();
        var result = new List<object?>();

        // 如果有第二个参数（resultSelector）
        if (resultSelector is not null)
        {
            if (resultSelector is not FuncLangValue resultFunc)
            {
                throw new ArgumentException("resultSelector 参数必须是函数类型");
            }

            foreach (var item in items)
            {
                var collection = func.Run(manager, [item]);
                IEnumerable<LangValueType> innerItems;

                if (collection is ILangList collectionList)
                {
                    innerItems = collectionList.GetItems();
                }
                else
                {
                    innerItems = [collection];
                }

                foreach (var innerItem in innerItems)
                {
                    var combined = resultFunc.Run(manager, [item, innerItem]);
                    result.Add(IlValueBridge.Unwrap(combined));
                }
            }
        }
        else
        {
            // 只有一个参数（selector）
            foreach (var item in items)
            {
                var selected = func.Run(manager, [item]);
                if (selected is ILangList selectedList)
                {
                    result.AddRange(selectedList.GetItems().Select(IlValueBridge.Unwrap));
                }
                else
                {
                    result.Add(IlValueBridge.Unwrap(selected));
                }
            }
        }

        return result;
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
