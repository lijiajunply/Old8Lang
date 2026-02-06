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
/// List.SkipWhileIndexed(predicate) - 跳过列表开头的元素，直到不满足条件（谓词接收元素和索引）
/// </summary>
public class ListSkipWhileIndexedMethod : BaseInstanceMethod
{
    public override string[] Names => ["SkipWhileIndexed", "skipWhileIndexed"];
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

        var result = new List<LangValueType>();
        var skipping = true;

        for (var i = 0; i < list.Values.Count; i++)
        {
            if (skipping)
            {
                try
                {
                    var predicateResult = predicate.Run(manager, [list.Values[i], new IntLangValue(i)]);
                    if (predicateResult is BoolLangValue { Value: false })
                    {
                        skipping = false;
                        result.Add(list.Values[i]);
                    }
                }
                catch
                {
                    skipping = false;
                    result.Add(list.Values[i]);
                }
            }
            else
            {
                result.Add(list.Values[i]);
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.SkipWhileIndexed 方法暂不支持编译模式");
    }

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
            var result = new List<object?>();
            var skipping = true;

            for (var i = 0; i < list.Count; i++)
            {
                if (skipping)
                {
                    try
                    {
                        var predicateResult = vm.CallFunctionObject(predicate, [list[i], i]);
                        if (predicateResult is bool boolResult && !boolResult)
                        {
                            skipping = false;
                            result.Add(list[i]);
                        }
                    }
                    catch
                    {
                        skipping = false;
                        result.Add(list[i]);
                    }
                }
                else
                {
                    result.Add(list[i]);
                }
            }

            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
