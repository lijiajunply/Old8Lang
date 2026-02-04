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
/// List.Single() 或 List.Single(predicate) - 获取列表中的唯一元素
/// </summary>
public class ListSingleMethod : BaseInstanceMethod
{
    public override string[] Names => ["Single", "single"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (parameters.Count == 0)
        {
            // 无参数版本：返回唯一元素
            if (list.Values.Count == 0)
            {
                throw new InvalidOperationError(position, "列表为空，无法获取唯一元素");
            }

            if (list.Values.Count > 1)
            {
                throw new InvalidOperationError(position, $"列表包含多个元素（{list.Values.Count} 个），无法获取唯一元素");
            }

            return list.Values[0];
        }
        else
        {
            // 带谓词版本：返回满足条件的唯一元素
            var predicate = parameters[0].Run(manager) as FuncLangValue;
            if (predicate == null)
            {
                throw new ArgumentError(position, "predicate 参数必须是函数类型");
            }

            LangValueType? foundItem = null;
            var foundCount = 0;

            foreach (var item in list.Values)
            {
                try
                {
                    var result = predicate.Run(manager, [item]);
                    if (result is BoolLangValue { Value: true })
                    {
                        foundItem = item;
                        foundCount++;
                    }
                }
                catch
                {
                    // 忽略执行错误
                }
            }

            if (foundCount == 0)
            {
                throw new InvalidOperationError(position, "没有元素满足条件");
            }

            if (foundCount > 1)
            {
                throw new InvalidOperationError(position, $"有 {foundCount} 个元素满足条件，无法获取唯一元素");
            }

            return foundItem!;
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.Single 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (arguments.Length == 0)
            {
                // 无参数版本
                if (list.Count == 0)
                {
                    throw new InvalidOperationException("列表为空，无法获取唯一元素");
                }

                if (list.Count > 1)
                {
                    throw new InvalidOperationException($"列表包含多个元素（{list.Count} 个），无法获取唯一元素");
                }

                return list[0];
            }
            else
            {
                // 带谓词版本
                var predicate = arguments[0];
                var vm = VMContext.CurrentVM;

                object? foundItem = null;
                var foundCount = 0;

                foreach (var item in list)
                {
                    try
                    {
                        var result = vm.CallFunctionObject(predicate, [item]);
                        if (result is bool boolResult && boolResult)
                        {
                            foundItem = item;
                            foundCount++;
                        }
                    }
                    catch
                    {
                        // 忽略执行错误
                    }
                }

                if (foundCount == 0)
                {
                    throw new InvalidOperationException("没有元素满足条件");
                }

                if (foundCount > 1)
                {
                    throw new InvalidOperationException($"有 {foundCount} 个元素满足条件，无法获取唯一元素");
                }

                return foundItem;
            }
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
