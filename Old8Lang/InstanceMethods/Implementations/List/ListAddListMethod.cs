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
/// List.AddList 方法 - 将另一个列表的所有元素添加到当前列表
/// </summary>
public class ListAddListMethod : BaseInstanceMethod
{
    public override string[] Names => ["AddList", "addList"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["otherList"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var otherParam = parameters[0].Run(manager);

        if (otherParam is not ListLangValue otherList)
        {
            throw new Error.TypeError(position, $"AddList 方法的参数必须是列表类型，但实际是 {otherParam.GetType().Name}");
        }

        list.Values.AddRange(otherList.Values);
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

        // 加载另一个列表参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var addListHelperMethod = typeof(ListAddListMethod).GetMethod(nameof(AddListHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, addListHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：添加另一个列表的所有元素
    /// </summary>
    public static VoidLangValue AddListHelper(List<LangValueType> list, LangValueType otherParam)
    {
        if (otherParam is not ListLangValue otherList)
        {
            throw new Exception("AddList 方法的参数必须是列表类型");
        }

        list.AddRange(otherList.Values);
        return new VoidLangValue();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(VoidLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (arguments[0] is not List<object?> otherList)
            {
                throw new ArgumentException("AddList 方法的参数必须是列表类型");
            }

            list.AddRange(otherList);
            return null;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
