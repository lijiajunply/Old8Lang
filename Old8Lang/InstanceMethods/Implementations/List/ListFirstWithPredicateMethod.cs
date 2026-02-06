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
/// List.First(predicate) 方法 - 返回满足条件的第一个元素
/// </summary>
public class ListFirstWithPredicateMethod : BaseInstanceMethod
{
    public override string[] Names => ["First", "first"];  // 改为与无参数版本相同的名称
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["predicate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    /// <summary>
    /// 参数类型：predicate 必须是函数
    /// </summary>
    public override Type?[]? ParameterTypes => [typeof(FuncLangValue)];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "返回满足条件的第一个元素";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var predicateParam = parameters[0].Run(manager);

        if (predicateParam is not FuncLangValue predicate)
        {
            throw new Error.TypeError(position, $"FirstWith 方法的参数必须是函数类型，但实际是 {predicateParam.GetType().Name}");
        }

        if (list.Values.Count == 0)
        {
            throw new InvalidOperationError(position, "列表为空，无法获取第一个元素");
        }

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var result = predicate.Run(tempManager, [item]);

            if (result is BoolLangValue { Value: true })
            {
                return item;
            }
        }

        throw new InvalidOperationError(position, "列表中没有满足条件的元素");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载谓词函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListFirstWithPredicateMethod).GetMethod(nameof(FirstWithPredicateHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：查找第一个满足条件的元素
    /// </summary>
    public static LangValueType FirstWithPredicateHelper(ListLangValue list, LangValueType predicateParam)
    {
        if (list.Values.Count == 0)
        {
            throw new InvalidOperationException("列表为空，无法获取第一个元素");
        }

        if (predicateParam is not FuncLangValue predicate)
        {
            throw new Exception("参数必须是函数类型");
        }

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var result = predicate.Run(tempManager, [item]);

            if (result is BoolLangValue { Value: true })
            {
                return item;
            }
        }

        throw new InvalidOperationException("列表中没有满足条件的元素");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("列表为空，无法获取第一个元素");
            }

            var predicate = arguments[0] as Func<object?, bool>;
            if (predicate == null)
            {
                throw new ArgumentException("参数必须是谓词函数");
            }

            foreach (var item in list)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            throw new InvalidOperationException("列表中没有满足条件的元素");
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
