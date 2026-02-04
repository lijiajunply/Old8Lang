using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Contains(item) - 检查是否包含指定元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListContainsMethod : BaseLangListMethod
{
    public override string[] Names => ["Contains", "contains"];
    public override string[] ParameterNames => ["item"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var searchItem = parameters[0].Run(manager);

        // 使用 In 方法检查是否包含
        if (instance is ILangList langList)
        {
            var contains = langList.In(searchItem);
            return BoolLangValue.Create(contains, position);
        }

        // 备用方案：手动比较
        var found = items.Any(item => item.Equal(searchItem));
        return BoolLangValue.Create(found, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListContainsMethod).GetMethod(nameof(ContainsHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool ContainsHelper(ILangList langList, LangValueType item)
    {
        return langList.In(item);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            var item = arguments[0];
            if (item is LangValueType langValue)
            {
                return langList.In(langValue);
            }
        }

        // 兼容 VM 模式下的 List<object?>
        if (instance is System.Collections.IList list)
        {
            return list.Contains(arguments[0]);
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
    }
}
