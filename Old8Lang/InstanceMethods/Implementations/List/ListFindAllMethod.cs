using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.FindAll(predicate) - 查找所有满足条件的元素（别名：IndexOfAll 返回索引列表）
/// </summary>
public class ListFindAllMethod : BaseInstanceMethod
{
    public override string[] Names => ["FindAll", "findAll"];
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

        foreach (var item in list.Values)
        {
            try
            {
                var predicateResult = predicate.Run(manager, [item]);
                if (predicateResult is BoolLangValue { Value: true })
                {
                    result.Add(item);
                }
            }
            catch
            {
                // 忽略执行错误
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.FindAll 方法暂不支持编译模式");
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

            foreach (var item in list)
            {
                try
                {
                    var predicateResult = vm.CallFunctionObject(predicate, [item]);
                    if (predicateResult is bool boolResult && boolResult)
                    {
                        result.Add(item);
                    }
                }
                catch
                {
                    // 忽略执行错误
                }
            }

            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
