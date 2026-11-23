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
/// ILangList.Max() - 最大值
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListMaxMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Max", "max"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    /// <summary>
    /// 参数类型：无参数
    /// </summary>
    public override Type?[]? ParameterTypes => [];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "获取列表中的最大值";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationError(instance, "序列不包含任何元素");
        }

        LangValueType max = items[0];
        foreach (var item in items.Skip(1))
        {
            if (item.Greater(max))
            {
                max = item;
            }
        }

        return max;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(LangListMaxMethod).GetMethod(nameof(MaxNativeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>IL 模式的辅助方法：返回原生最大元素。</summary>
    public static object? MaxNativeHelper(LangValueType instance)
    {
        if (instance is not ILangList langList)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        return IlValueBridge.Unwrap(MaxHelper(langList));
    }

    public static LangValueType MaxHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        if (items.Count == 0)
        {
            throw new InvalidOperationException("序列不包含任何元素");
        }

        LangValueType max = items[0];
        foreach (var item in items.Skip(1))
        {
            if (item.Greater(max))
            {
                max = item;
            }
        }

        return max;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var list = new ListLangValue(GetItemsForVM(instance).Select(IlValueBridge.Wrap).ToList());
        return IlValueBridge.Unwrap(ExecuteInternal(list, [], new VariateManager(), default));
    }
}
