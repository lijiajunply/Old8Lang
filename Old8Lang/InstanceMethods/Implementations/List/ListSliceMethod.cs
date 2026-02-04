using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Slice(start, end) - 获取列表的切片
/// </summary>
public class ListSliceMethod : BaseInstanceMethod
{
    public override string[] Names => ["Slice", "slice"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["start", "end"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var startValue = parameters[0].Run(manager);
        var endValue = parameters[1].Run(manager);

        if (startValue is not IntLangValue startInt)
        {
            throw new ArgumentError(position, "start 参数必须是整数类型");
        }

        if (endValue is not IntLangValue endInt)
        {
            throw new ArgumentError(position, "end 参数必须是整数类型");
        }

        var start = startInt.Value;
        var end = endInt.Value;

        // 处理负数索引
        if (start < 0) start = list.Values.Count + start;
        if (end < 0) end = list.Values.Count + end;

        // 边界检查
        if (start < 0) start = 0;
        if (end > list.Values.Count) end = list.Values.Count;
        if (start > end) start = end;

        var slicedItems = list.Values.Skip(start).Take(end - start).ToList();
        return new ListLangValue(slicedItems);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        parameters[1].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListSliceMethod).GetMethod(nameof(SliceHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> SliceHelper(List<object?> list, int start, int end)
    {
        // 处理负数索引
        if (start < 0) start = list.Count + start;
        if (end < 0) end = list.Count + end;

        // 边界检查
        if (start < 0) start = 0;
        if (end > list.Count) end = list.Count;
        if (start > end) start = end;

        return list.Skip(start).Take(end - start).ToList();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length >= 2)
        {
            if (arguments[0] is not int start || arguments[1] is not int end)
            {
                throw new ArgumentException("start 和 end 参数必须是整数类型");
            }

            // 处理负数索引
            if (start < 0) start = list.Count + start;
            if (end < 0) end = list.Count + end;

            // 边界检查
            if (start < 0) start = 0;
            if (end > list.Count) end = list.Count;
            if (start > end) start = end;

            return list.Skip(start).Take(end - start).ToList();
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
