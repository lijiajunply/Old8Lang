using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Take(count) - 取前 N 个元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListTakeMethod : BaseLangListMethod
{
    public override string[] Names => ["Take", "take"];
    public override string[] ParameterNames => ["count"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var countValue = parameters[0].Run(manager);

        if (countValue is not IntLangValue count)
        {
            throw new ArgumentException("Take 方法的参数必须是整数");
        }

        var takenItems = items.Take(count.Value).ToList();
        return new ListLangValue(takenItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListTakeMethod).GetMethod(nameof(TakeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue TakeHelper(ILangList langList, int count)
    {
        var items = langList.GetItems().Take(count).ToList();
        return new ListLangValue(items);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList && arguments[0] is int count)
        {
            var items = langList.GetItems().Take(count).ToList();
            return new ListLangValue(items);
        }

        if (instance is List<object?> list && arguments[0] is int cnt)
        {
            return list.Take(cnt).ToList();
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或为 List<object?> 类型，当前类型：{instance?.GetType().Name}");
    }
}
