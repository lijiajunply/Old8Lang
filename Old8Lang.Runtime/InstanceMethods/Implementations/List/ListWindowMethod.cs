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
/// List.Window(size) - 创建滑动窗口，返回所有大小为 size 的连续子列表
/// </summary>
public class ListWindowMethod : BaseInstanceMethod
{
    public override string[] Names => ["Window", "window"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["size"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var sizeValue = parameters[0].Run(manager);

        if (sizeValue is not IntLangValue sizeInt)
        {
            throw new ArgumentError(position, "size 参数必须是整数类型");
        }

        var size = sizeInt.Value;

        if (size <= 0)
        {
            throw new ArgumentError(position, "size 参数必须大于 0");
        }

        if (size > list.Values.Count)
        {
            return new ListLangValue(new List<LangValueType>());
        }

        var result = new List<LangValueType>();

        for (var i = 0; i <= list.Values.Count - size; i++)
        {
            var window = list.Values.Skip(i).Take(size).ToList();
            result.Add(new ListLangValue(window));
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListWindowMethod).GetMethod(nameof(WindowHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> WindowHelper(List<object?> list, int size)
    {
        if (size <= 0)
        {
            throw new ArgumentException("size 参数必须大于 0");
        }

        if (size > list.Count)
        {
            return new List<object?>();
        }

        var result = new List<object?>();

        for (var i = 0; i <= list.Count - size; i++)
        {
            var window = list.Skip(i).Take(size).ToList();
            result.Add(window);
        }

        return result;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (arguments[0] is not int size)
            {
                throw new ArgumentException("size 参数必须是整数类型");
            }

            return WindowHelper(list, size);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
