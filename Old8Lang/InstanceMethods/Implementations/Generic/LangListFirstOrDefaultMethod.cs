using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.FirstOrDefault(defaultValue?) - 获取第一个元素或默认值
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListFirstOrDefaultMethod : BaseLangListMethod
{
    public override string[] Names => ["FirstOrDefault", "firstOrDefault"];
    public override string[]? ParameterNames => ["defaultValue"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count == 0)
        {
            // 如果提供了默认值，返回默认值
            if (parameters.Count == 1)
            {
                return parameters[0].Run(manager);
            }
            // 否则返回 null
            return NullLangValue.Instance;
        }

        return items[0];
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        if (parameters.Count == 1)
        {
            parameters[0].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(LangListFirstOrDefaultMethod).GetMethod(nameof(FirstOrDefaultWithValueHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(LangListFirstOrDefaultMethod).GetMethod(nameof(FirstOrDefaultHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static LangValueType FirstOrDefaultHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        return items.Count == 0 ? NullLangValue.Instance : items[0];
    }

    public static LangValueType FirstOrDefaultWithValueHelper(ILangList langList, LangValueType defaultValue)
    {
        var items = langList.GetItems().ToList();
        return items.Count == 0 ? defaultValue : items[0];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            var items = langList.GetItems().ToList();
            if (items.Count == 0)
            {
                return arguments.Length == 1 ? arguments[0] : null;
            }
            return items[0];
        }

        if (instance is System.Collections.IList list)
        {
            if (list.Count == 0)
            {
                return arguments.Length == 1 ? arguments[0] : null;
            }
            return list[0];
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
    }
}
