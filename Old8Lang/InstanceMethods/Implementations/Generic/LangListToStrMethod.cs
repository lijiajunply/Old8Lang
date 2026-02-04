using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ToStr() - 转换为字符串
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListToStrMethod : BaseLangListMethod
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
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListToStrMethod).GetMethod(nameof(ToStrHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
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
        if (instance is ILangList langList)
        {
            return ToStrHelper(langList);
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance?.GetType().Name}");
    }
}
