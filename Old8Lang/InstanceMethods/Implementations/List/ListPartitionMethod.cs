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
public class ListPartitionMethod : BaseInstanceMethod
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

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.Partition 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object[]);
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
