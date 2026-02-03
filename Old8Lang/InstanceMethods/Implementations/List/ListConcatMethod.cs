using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Concat 方法 - 连接两个列表，返回包含所有元素的新列表
/// </summary>
public class ListConcatMethod : BaseInstanceMethod
{
    public override string[] Names => ["Concat", "concat"];
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
            throw new Error.TypeError(position, $"Concat 方法的参数必须是列表类型，但实际是 {otherParam.GetType().Name}");
        }

        var result = new List<LangValueType>(list.Values);
        result.AddRange(otherList.Values);
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载另一个列表参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var concatHelperMethod = typeof(ListConcatMethod).GetMethod(nameof(ConcatHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, concatHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：连接两个列表
    /// </summary>
    public static ListLangValue ConcatHelper(ListLangValue list, LangValueType otherParam)
    {
        if (otherParam is not ListLangValue otherList)
        {
            throw new Exception("Concat 方法的参数必须是列表类型");
        }

        var result = new List<LangValueType>(list.Values);
        result.AddRange(otherList.Values);
        return new ListLangValue(result);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (arguments[0] is not List<object?> otherList)
            {
                throw new ArgumentException("Concat 方法的参数必须是列表类型");
            }

            var result = new List<object?>(list);
            result.AddRange(otherList);
            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
