using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Zip 方法 - 将两个列表按索引配对
/// </summary>
public class ListZipMethod : BaseInstanceMethod
{
    public override string[] Names => ["Zip", "zip"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["other", "selector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var otherValue = parameters[0].Run(manager);

        if (otherValue is not ListLangValue other)
        {
            throw new TypeError(position, $"Zip 方法的第一个参数必须是列表类型，但实际是 {otherValue.GetType().Name}");
        }

        var result = new List<LangValueType>();
        var minLength = Math.Min(list.Values.Count, other.Values.Count);

        // 如果提供了选择器函数
        if (parameters.Count > 1)
        {
            var selectorParam = parameters[1].Run(manager);
            if (selectorParam is not FuncLangValue selector)
            {
                throw new TypeError(position, $"Zip 方法的第二个参数必须是函数类型，但实际是 {selectorParam.GetType().Name}");
            }

            for (int i = 0; i < minLength; i++)
            {
                var tempManager = new VariateManager();
                var paired = selector.Run(tempManager, [list.Values[i], other.Values[i]]);
                result.Add(paired);
            }
        }
        else
        {
            // 默认创建元组对
            for (int i = 0; i < minLength; i++)
            {
                var tuple = new ListLangValue([list.Values[i], other.Values[i]]);
                result.Add(tuple);
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载另一个列表
        parameters[0].LoadIlValue(ilGenerator, local);

        // 如果有选择器函数
        if (parameters.Count > 1)
        {
            parameters[1].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(ListZipMethod).GetMethod(nameof(ZipWithSelectorHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(ListZipMethod).GetMethod(nameof(ZipHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    /// <summary>
    /// 辅助方法：Zip 操作（默认创建元组）
    /// </summary>
    public static ListLangValue ZipHelper(ListLangValue list, ListLangValue other)
    {
        var result = new List<LangValueType>();
        var minLength = Math.Min(list.Values.Count, other.Values.Count);

        for (int i = 0; i < minLength; i++)
        {
            var tuple = new ListLangValue([list.Values[i], other.Values[i]]);
            result.Add(tuple);
        }

        return new ListLangValue(result);
    }

    /// <summary>
    /// 辅助方法：Zip 操作（带选择器）
    /// </summary>
    public static ListLangValue ZipWithSelectorHelper(ListLangValue list, ListLangValue other, LangValueType selectorParam)
    {
        if (selectorParam is not FuncLangValue selector)
        {
            throw new Exception("Zip 方法的第二个参数必须是函数类型");
        }

        var result = new List<LangValueType>();
        var minLength = Math.Min(list.Values.Count, other.Values.Count);

        for (int i = 0; i < minLength; i++)
        {
            var tempManager = new VariateManager();
            var paired = selector.Run(tempManager, [list.Values[i], other.Values[i]]);
            result.Add(paired);
        }

        return new ListLangValue(result);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0 && arguments[0] is List<object?> other)
        {
            if (arguments.Length > 1 && arguments[1] is Func<object?, object?, object?> selector)
            {
                return list.Zip(other, selector).ToList();
            }
            else
            {
                return list.Zip(other, (a, b) => new List<object?> { a, b }).ToList();
            }
        }

        throw new ArgumentException("实例和参数必须是 List<object?> 类型");
    }
}
