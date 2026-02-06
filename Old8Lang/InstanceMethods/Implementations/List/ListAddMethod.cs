using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Add 方法 - 向列表中添加元素
/// </summary>
public class ListAddMethod : BaseInstanceMethod
{
    public override string[] Names => ["Add", "add"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["item"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    /// <summary>
    /// 参数类型：item 可以是任意类型
    /// </summary>
    public override Type?[]? ParameterTypes => [null]; // null 表示接受任意类型

    /// <summary>
    /// 返回类型：返回添加的元素
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "向列表中添加一个元素";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var item = parameters[0].Run(manager);
        list.Values.Add(item);
        return item;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 获取 Values 字段
        var valuesField = typeof(ListLangValue).GetField("Values");
        ilGenerator.Emit(OpCodes.Ldfld, valuesField!);

        // 加载要添加的元素
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 List<LangValueType>.Add 方法
        var addMethod = typeof(List<LangValueType>).GetMethod("Add");
        ilGenerator.Emit(OpCodes.Callvirt, addMethod!);

        // 返回添加的元素
        parameters[0].LoadIlValue(ilGenerator, local);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            var item = arguments[0];
            list.Add(item);
            return item;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
