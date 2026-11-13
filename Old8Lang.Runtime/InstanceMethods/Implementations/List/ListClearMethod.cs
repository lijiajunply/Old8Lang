using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Clear 方法 - 清空列表
/// </summary>
public class ListClearMethod : BaseInstanceMethod
{
    public override string[] Names => ["Clear", "clear"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        list.Values.Clear();
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 获取 Values 字段
        var valuesField = typeof(ListLangValue).GetField("Values");
        ilGenerator.Emit(OpCodes.Ldfld, valuesField!);

        // 调用 Clear 方法
        var clearMethod = typeof(List<LangValueType>).GetMethod("Clear");
        ilGenerator.Emit(OpCodes.Callvirt, clearMethod!);

        // 返回 VoidLangValue
        var voidCtor = typeof(VoidLangValue).GetConstructor(Type.EmptyTypes);
        ilGenerator.Emit(OpCodes.Newobj, voidCtor!);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(VoidLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            list.Clear();
            return null;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
