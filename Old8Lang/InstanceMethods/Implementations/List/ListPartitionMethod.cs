using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Partition(predicate) - 根据条件将列表分为两部分，返回包含两个列表的元组
/// </summary>
public class ListPartitionMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Partition", "partition"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["predicate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var predicate = parameters[0].Run(manager) as FuncLangValue;

        if (predicate == null)
        {
            throw new ArgumentError(position, "predicate 参数必须是函数类型");
        }

        var trueList = new List<LangValueType>();
        var falseList = new List<LangValueType>();

        foreach (var item in list.Values)
        {
            try
            {
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    trueList.Add(item);
                }
                else
                {
                    falseList.Add(item);
                }
            }
            catch
            {
                // 执行错误的元素放入 false 列表
                falseList.Add(item);
            }
        }

        var tuple = new TupleLangValue(new ListLangValue(trueList), new ListLangValue(falseList));
        tuple.ItemValues.Add(new ListLangValue(trueList));
        tuple.ItemValues.Add(new ListLangValue(falseList));
        return tuple;
    }

    /// <summary>
    /// IL 模式：接收者是原生 <c>List&lt;T&gt;</c>，谓词是编译好的 .NET 委托。
    /// 先把两者转换成 Old8Lang 值调用 helper，再由 helper 把结果转回原生列表。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 接收者：原生集合 -> Old8Lang 值
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        // 谓词：编译好的委托 -> 函数值
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(ListPartitionMethod).GetMethod(nameof(PartitionHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：语义与 <see cref="ExecuteInternal"/> 一致（满足条件的在前，其余在后），
    /// 结果用「两个原生列表」表示，返回包含这两个列表的原生列表。
    /// </summary>
    public static List<object?> PartitionHelper(LangValueType instance, LangValueType predicate)
    {
        if (instance is not ListLangValue list)
        {
            throw new ArgumentException($"实例必须是列表，实际是 {instance.GetType().Name}");
        }

        if (predicate is not FuncLangValue func)
        {
            throw new ArgumentException("predicate 参数必须是函数类型");
        }

        var manager = new VariateManager();
        var trueList = new List<object?>();
        var falseList = new List<object?>();

        foreach (var item in list.Values)
        {
            try
            {
                var result = func.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    trueList.Add(IlValueBridge.Unwrap(item));
                }
                else
                {
                    falseList.Add(IlValueBridge.Unwrap(item));
                }
            }
            catch
            {
                // 执行错误的元素放入 false 列表
                falseList.Add(IlValueBridge.Unwrap(item));
            }
        }

        return [trueList, falseList];
    }

    /// <summary>
    /// IL 模式下 helper 返回包含「满足条件」与「不满足条件」两个原生列表的原生列表，
    /// 与 <c>PartitionHelper</c> 的返回类型一致。
    /// </summary>
    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            var predicate = arguments[0];
            var vm = VMContext.CurrentVM;
            var trueList = new List<object?>();
            var falseList = new List<object?>();

            foreach (var item in list)
            {
                try
                {
                    var result = vm.CallFunctionObject(predicate, [item]);
                    if (result is bool boolResult && boolResult)
                    {
                        trueList.Add(item);
                    }
                    else
                    {
                        falseList.Add(item);
                    }
                }
                catch
                {
                    // 执行错误的元素放入 false 列表
                    falseList.Add(item);
                }
            }

            return new object?[] { trueList, falseList };
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
