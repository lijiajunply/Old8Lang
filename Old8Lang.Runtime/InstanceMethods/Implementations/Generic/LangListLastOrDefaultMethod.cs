using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.LastOrDefault(defaultValue?) - 获取最后一个元素或默认值
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListLastOrDefaultMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["LastOrDefault", "lastOrDefault"];
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
                return parameters[0].Accept(new InterpreterVisitor(manager));
            }
            // 否则返回 null
            return NullLangValue.Instance;
        }

        return items[items.Count - 1];
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        if (parameters.Count == 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
            var helperMethod = typeof(LangListLastOrDefaultMethod).GetMethod(nameof(LastOrDefaultWithValueNativeHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(LangListLastOrDefaultMethod).GetMethod(nameof(LastOrDefaultNativeHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static object? LastOrDefaultNativeHelper(LangValueType instance)
    {
        if (instance is not ILangList langList)
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        return IlValueBridge.Unwrap(LastOrDefaultHelper(langList));
    }

    public static object? LastOrDefaultWithValueNativeHelper(LangValueType instance, LangValueType defaultValue)
    {
        if (instance is not ILangList langList)
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        return IlValueBridge.Unwrap(LastOrDefaultWithValueHelper(langList, defaultValue));
    }

    public static LangValueType LastOrDefaultHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        return items.Count == 0 ? NullLangValue.Instance : items[items.Count - 1];
    }

    public static LangValueType LastOrDefaultWithValueHelper(ILangList langList, LangValueType defaultValue)
    {
        var items = langList.GetItems().ToList();
        return items.Count == 0 ? defaultValue : items[items.Count - 1];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
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
            return items[items.Count - 1];
        }

        if (instance is System.Collections.IList list)
        {
            if (list.Count == 0)
            {
                return arguments.Length == 1 ? arguments[0] : null;
            }
            return list[list.Count - 1];
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
    }
}
