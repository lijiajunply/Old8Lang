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
/// List.SingleOrDefault(defaultValue) 或 List.SingleOrDefault(predicate, defaultValue) - 安全获取唯一元素
/// </summary>
public class ListSingleOrDefaultMethod : BaseInstanceMethod
{
    public override string[] Names => ["SingleOrDefault", "singleOrDefault"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (parameters.Count == 1)
        {
            // SingleOrDefault(defaultValue)
            var defaultValue = parameters[0].Run(manager);

            if (list.Values.Count == 0 || list.Values.Count > 1)
            {
                return defaultValue;
            }

            return list.Values[0];
        }
        else
        {
            // SingleOrDefault(predicate, defaultValue)
            var predicate = parameters[0].Run(manager) as FuncLangValue;
            var defaultValue = parameters[1].Run(manager);

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

            if (foundCount == 0 || foundCount > 1)
            {
                return defaultValue;
            }

            return foundItem!;
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.SingleOrDefault 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (arguments.Length == 1)
            {
                // SingleOrDefault(defaultValue)
                var defaultValue = arguments[0];

                if (list.Count == 0 || list.Count > 1)
                {
                    return defaultValue;
                }

                return list[0];
            }
            else if (arguments.Length == 2)
            {
                // SingleOrDefault(predicate, defaultValue)
                var predicate = arguments[0];
                var defaultValue = arguments[1];
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

                if (foundCount == 0 || foundCount > 1)
                {
                    return defaultValue;
                }

                return foundItem;
            }
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
