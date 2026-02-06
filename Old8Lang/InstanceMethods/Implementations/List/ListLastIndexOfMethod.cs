using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.LastIndexOf(element) - 查找元素在列表中最后一次出现的索引
/// </summary>
public class ListLastIndexOfMethod : BaseInstanceMethod
{
    public override string[] Names => ["LastIndexOf", "lastIndexOf"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["element"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var element = parameters[0].Run(manager);

        // 从后向前查找
        for (var i = list.Values.Count - 1; i >= 0; i--)
        {
            if (list.Values[i].Equal(element))
            {
                return new IntLangValue(i);
            }
        }

        return new IntLangValue(-1); // 未找到返回-1
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListLastIndexOfMethod).GetMethod(nameof(LastIndexOfHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static int LastIndexOfHelper(List<object?> list, object? element)
    {
        // 从后向前查找
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (Equals(list[i], element))
            {
                return i;
            }
        }

        return -1; // 未找到返回-1
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            return LastIndexOfHelper(list, arguments[0]);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
