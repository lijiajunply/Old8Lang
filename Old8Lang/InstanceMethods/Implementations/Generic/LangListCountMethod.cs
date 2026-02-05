using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Count() - 返回列表长度
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListCountMethod : BaseLangListMethod
{
    public override string[] Names => ["Count", "count"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var length = GetLength(instance);
        return IntLangValue.Create(length, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListCountMethod).GetMethod(nameof(CountHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static int CountHelper(ILangList langList)
    {
        return langList.GetLength();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            return langList.GetLength();
        }

        // 兼容 VM 模式下的 List<object?>
        if (instance is System.Collections.ICollection collection)
        {
            return collection.Count;
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或 ICollection 接口，当前类型：{instance?.GetType().Name}");
    }
}
