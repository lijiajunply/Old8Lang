using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Skip 方法 - 跳过列表的前n个元素，返回剩余元素
/// </summary>
public class ListSkipMethod : BaseInstanceMethod
{
    public override string[] Names => ["Skip", "skip"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["count"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var countParam = parameters[0].Run(manager);

        if (countParam is not IntLangValue countValue)
        {
            throw new Error.TypeError(position, $"Skip 方法的参数必须是整数类型，但实际是 {countParam.GetType().Name}");
        }

        var skipCount = Math.Max(0, Math.Min(countValue.Value, list.Values.Count));
        var result = list.Values.Skip(skipCount).ToList();
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载跳过数量
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var skipHelperMethod = typeof(ListSkipMethod).GetMethod(nameof(SkipHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, skipHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：跳过元素
    /// </summary>
    public static ListLangValue SkipHelper(ListLangValue list, LangValueType countParam)
    {
        if (countParam is not IntLangValue countValue)
        {
            throw new Exception("Skip 方法的参数必须是整数类型");
        }

        var skipCount = Math.Max(0, Math.Min(countValue.Value, list.Values.Count));
        var result = list.Values.Skip(skipCount).ToList();
        return new ListLangValue(result);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (arguments[0] is not int count)
            {
                throw new ArgumentException("Skip 方法的参数必须是整数类型");
            }

            var skipCount = Math.Max(0, Math.Min(count, list.Count));
            return list.Skip(skipCount).ToList();
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
