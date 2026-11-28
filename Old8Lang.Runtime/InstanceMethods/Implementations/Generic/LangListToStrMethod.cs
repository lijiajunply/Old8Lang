using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ToStr() - 转换为字符串
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListToStrMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["ToStr", "toStr", "ToString", "toString"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var strings = items.Select(item => item.ToString() ?? "null");
        var result = "[" + string.Join(", ", strings) + "]";

        return StringLangValue.Create(result, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(LangListToStrMethod).GetMethod(nameof(ToStrNativeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static string ToStrNativeHelper(LangValueType instance)
    {
        if (instance is not ILangList langList)
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        return ToStrHelper(langList);
    }

    public static string ToStrHelper(ILangList langList)
    {
        var items = langList.GetItems();
        var strings = items.Select(item => item.ToString() ?? "null");
        return "[" + string.Join(", ", strings) + "]";
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 使用基类的辅助方法获取元素列表（支持 object[], List<object?>, ILangList）
        // 元素按显示口径格式化（字符串带引号），与解释器的 ToStr 一致
        var items = GetItemsForVM(instance);
        var strings = items.Select(Runtime.VMValueFormatter.FormatValueForDisplay);
        return "[" + string.Join(", ", strings) + "]";
    }
}
