using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Count 方法 - 返回列表元素数量
/// </summary>
public class ListCountMethod : BaseInstanceMethod
{
    public override string[] Names => ["Count", "count"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        return new IntLangValue(list.Values.Count);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 获取 Values 字段
        var valuesField = typeof(ListLangValue).GetField("Values");
        ilGenerator.Emit(OpCodes.Ldfld, valuesField!);

        // 获取 Count 属性
        var countProperty = typeof(List<LangValueType>).GetProperty("Count");
        ilGenerator.Emit(OpCodes.Callvirt, countProperty!.GetMethod!);

        // 创建 IntLangValue
        var intCtor = typeof(IntLangValue).GetConstructor([typeof(int)]);
        ilGenerator.Emit(OpCodes.Newobj, intCtor!);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(IntLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            return list.Count;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
