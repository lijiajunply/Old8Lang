using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.All 方法 - 检查列表中是否所有元素都满足条件
/// </summary>
public class ListAllMethod : BaseInstanceMethod
{
    public override string[] Names => ["All", "all"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["predicate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var predicateParam = parameters[0].Run(manager);

        if (predicateParam is not FuncLangValue predicate)
        {
            throw new Error.TypeError(position, $"All 方法的参数必须是函数类型，但实际是 {predicateParam.GetType().Name}");
        }

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var result = predicate.Run(tempManager, [item]);

            if (result is not BoolLangValue { Value: true })
            {
                return new BoolLangValue(false);
            }
        }

        return new BoolLangValue(true);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载谓词函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var allHelperMethod = typeof(ListAllMethod).GetMethod(nameof(AllHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, allHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：检查是否所有元素都满足条件
    /// </summary>
    public static BoolLangValue AllHelper(ListLangValue list, LangValueType predicateParam)
    {
        if (predicateParam is not FuncLangValue predicate)
        {
            throw new Exception("All 方法的参数必须是函数类型");
        }

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var result = predicate.Run(tempManager, [item]);

            if (result is not BoolLangValue { Value: true })
            {
                return new BoolLangValue(false);
            }
        }

        return new BoolLangValue(true);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式下不支持高阶函数
        throw new NotSupportedException("All 方法在 VM 模式下不支持");
    }
}
