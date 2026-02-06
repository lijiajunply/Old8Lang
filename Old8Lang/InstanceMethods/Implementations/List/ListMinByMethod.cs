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
/// List.MinBy(selector) - 获取选择器返回最小值的元素
/// </summary>
public class ListMinByMethod : BaseInstanceMethod
{
    public override string[] Names => ["MinBy", "minBy"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["selector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (list.Values.Count == 0)
        {
            throw new InvalidOperationError(position, "无法从空列表中获取最小值元素");
        }

        var selector = parameters[0].Run(manager) as FuncLangValue;
        if (selector == null)
        {
            throw new ArgumentError(position, "selector 参数必须是函数类型");
        }

        LangValueType? minItem = null;
        LangValueType? minValue = null;

        foreach (var item in list.Values)
        {
            try
            {
                var value = selector.Run(manager, [item]);

                if (minItem == null || (minValue != null && value.Less(minValue)))
                {
                    minItem = item;
                    minValue = value;
                }
            }
            catch
            {
                // 忽略执行错误
            }
        }

        if (minItem == null)
        {
            throw new InvalidOperationError(position, "无法计算最小值");
        }

        return minItem;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.MinBy 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("无法从空列表中获取最小值元素");
            }

            var selector = arguments[0];
            var vm = VMContext.CurrentVM;

            object? minItem = null;
            IComparable? minValue = null;

            foreach (var item in list)
            {
                try
                {
                    var value = vm.CallFunctionObject(selector, [item]);

                    if (minItem == null || (value is IComparable comparable && minValue != null && comparable.CompareTo(minValue) < 0))
                    {
                        minItem = item;
                        minValue = value as IComparable;
                    }
                }
                catch
                {
                    // 忽略执行错误
                }
            }

            if (minItem == null)
            {
                throw new InvalidOperationException("无法计算最小值");
            }

            return minItem;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
