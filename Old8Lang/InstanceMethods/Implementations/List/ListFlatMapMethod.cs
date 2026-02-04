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
/// List.FlatMap(transform) - 对每个元素应用转换函数，然后扁平化结果
/// </summary>
public class ListFlatMapMethod : BaseInstanceMethod
{
    public override string[] Names => ["FlatMap", "flatMap"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["transform"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var transform = parameters[0].Run(manager) as FuncLangValue;

        if (transform == null)
        {
            throw new ArgumentError(position, "transform 参数必须是函数类型");
        }

        var result = new List<LangValueType>();

        foreach (var item in list.Values)
        {
            try
            {
                var transformed = transform.Run(manager, [item]);

                // 如果转换结果是列表，则扁平化
                if (transformed is ListLangValue transformedList)
                {
                    result.AddRange(transformedList.Values);
                }
                else
                {
                    result.Add(transformed);
                }
            }
            catch
            {
                // 忽略转换错误
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.FlatMap 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            var transform = arguments[0];
            var vm = VMContext.CurrentVM;
            var result = new List<object?>();

            foreach (var item in list)
            {
                try
                {
                    var transformed = vm.CallFunctionObject(transform, [item]);

                    // 如果转换结果是列表，则扁平化
                    if (transformed is List<object?> transformedList)
                    {
                        result.AddRange(transformedList);
                    }
                    else
                    {
                        result.Add(transformed);
                    }
                }
                catch
                {
                    // 忽略转换错误
                }
            }

            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
