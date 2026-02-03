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
/// List.Insert 方法 - 在指定索引处插入元素
/// </summary>
public class ListInsertMethod : BaseInstanceMethod
{
    public override string[] Names => ["Insert", "insert"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["index", "element"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var indexParam = parameters[0].Run(manager);
        var element = parameters[1].Run(manager);

        if (indexParam is not IntLangValue indexValue)
        {
            throw new Error.TypeError(position, $"Insert 方法的第一个参数必须是整数类型，但实际是 {indexParam.GetType().Name}");
        }

        var insertIndex = Math.Max(0, Math.Min(indexValue.Value, list.Values.Count));
        list.Values.Insert(insertIndex, element);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载索引参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载元素参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var insertHelperMethod = typeof(ListInsertMethod).GetMethod(nameof(InsertHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, insertHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：插入元素
    /// </summary>
    public static VoidLangValue InsertHelper(ListLangValue list, LangValueType indexParam, LangValueType element)
    {
        if (indexParam is not IntLangValue indexValue)
        {
            throw new Exception("Insert 方法的第一个参数必须是整数类型");
        }

        var insertIndex = Math.Max(0, Math.Min(indexValue.Value, list.Values.Count));
        list.Values.Insert(insertIndex, element);
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
            if (arguments[0] is not int index)
            {
                throw new ArgumentException("Insert 方法的第一个参数必须是整数类型");
            }

            var insertIndex = Math.Max(0, Math.Min(index, list.Count));
            list.Insert(insertIndex, arguments[1]);
            return null;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
