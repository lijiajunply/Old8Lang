using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Zip3(second, third) - 将三个列表合并为三元组列表
/// </summary>
public class LangListZip3Method : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Zip3", "zip3"];
    public override string[] ParameterNames => ["second", "third"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var secondValue = parameters[0].Run(manager);
        var thirdValue = parameters[1].Run(manager);

        if (!IsLangList(secondValue))
        {
            throw new ArgumentError(position, "second 参数必须是列表或数组类型");
        }

        if (!IsLangList(thirdValue))
        {
            throw new ArgumentError(position, "third 参数必须是列表或数组类型");
        }

        var secondItems = GetItems(secondValue);
        var thirdItems = GetItems(thirdValue);

        var result = new List<LangValueType>();
        var minLength = Math.Min(Math.Min(items.Count, secondItems.Count), thirdItems.Count);

        for (int i = 0; i < minLength; i++)
        {
            var tuple = CreateTupleWithValues(items[i], secondItems[i], thirdItems[i]);
            result.Add(tuple);
        }

        return new ListLangValue(result);
    }

    /// <summary>
    /// IL 模式：接收者与两个参数都是原生集合，先把它们转换成 Old8Lang 值调用 helper。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[1], ilGenerator, local);

        var helperMethod = typeof(LangListZip3Method).GetMethod(nameof(Zip3Helper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：把三个列表按最短长度合并，每个元素是 <c>List&lt;object?&gt;</c> 三元组。
    /// 与 <see cref="ExecuteInternal"/> 语义一致。
    /// </summary>
    public static List<object?> Zip3Helper(LangValueType instance, LangValueType second, LangValueType third)
    {
        var items = GetIlItems(instance);

        if (second is not ILangList secondList)
        {
            throw new ArgumentException("second 参数必须是列表或数组类型");
        }

        if (third is not ILangList thirdList)
        {
            throw new ArgumentException("third 参数必须是列表或数组类型");
        }

        var secondItems = secondList.GetItems().ToList();
        var thirdItems = thirdList.GetItems().ToList();

        var result = new List<object?>();
        var minLength = Math.Min(Math.Min(items.Count, secondItems.Count), thirdItems.Count);

        for (int i = 0; i < minLength; i++)
        {
            result.Add(new List<object?>
            {
                IlValueBridge.Unwrap(items[i]),
                IlValueBridge.Unwrap(secondItems[i]),
                IlValueBridge.Unwrap(thirdItems[i])
            });
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

        if (arguments.Length < 2)
        {
            throw new ArgumentException("需要两个参数");
        }

        var secondItems = GetItemsForVM(arguments[0]);
        var thirdItems = GetItemsForVM(arguments[1]);

        var result = new List<object?>();
        var minLength = Math.Min(Math.Min(items.Count, secondItems.Count), thirdItems.Count);

        for (int i = 0; i < minLength; i++)
        {
            result.Add(new object?[] { items[i], secondItems[i], thirdItems[i] });
        }

        return result;
    }

    /// <summary>
    /// 创建一个带有预填充 ItemValues 的 TupleLangValue
    /// </summary>
    private static TupleLangValue CreateTupleWithValues(params LangValueType[] values)
    {
        var tuple = new TupleLangValue(values.Cast<LangExpression>().ToList());
        foreach (var value in values)
        {
            tuple.ItemValues.Add(value);
        }
        return tuple;
    }
}
