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
/// List.Pop() - 移除并返回列表的最后一个元素
/// </summary>
public class ListPopMethod : BaseInstanceMethod
{
    public override string[] Names => ["Pop", "pop"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (list.Values.Count == 0)
        {
            throw new InvalidOperationError(position, "无法从空列表中弹出元素");
        }

        var lastIndex = list.Values.Count - 1;
        var item = list.Values[lastIndex];
        list.Values.RemoveAt(lastIndex);
        return item;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListPopMethod).GetMethod(nameof(PopHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object? PopHelper(List<object?> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("无法从空列表中弹出元素");
        }

        var lastIndex = list.Count - 1;
        var item = list[lastIndex];
        list.RemoveAt(lastIndex);
        return item;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("无法从空列表中弹出元素");
            }

            var lastIndex = list.Count - 1;
            var item = list[lastIndex];
            list.RemoveAt(lastIndex);
            return item;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
