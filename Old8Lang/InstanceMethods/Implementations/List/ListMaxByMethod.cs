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
/// List.MaxBy(selector) - 获取选择器返回最大值的元素
/// </summary>
public class ListMaxByMethod : BaseInstanceMethod
{
    public override string[] Names => ["MaxBy", "maxBy"];
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
            throw new InvalidOperationError(position, "无法从空列表中获取最大值元素");
        }

        var selector = parameters[0].Run(manager) as FuncLangValue;
        if (selector == null)
        {
            throw new ArgumentError(position, "selector 参数必须是函数类型");
        }

        LangValueType? maxItem = null;
        LangValueType? maxValue = null;

        foreach (var item in list.Values)
        {
            try
            {
                var value = selector.Run(manager, [item]);

                if (maxItem == null || (maxValue != null && value.Greater(maxValue)))
                {
                    maxItem = item;
                    maxValue = value;
                }
            }
            catch
            {
                // 忽略执行错误
            }
        }

        if (maxItem == null)
        {
            throw new InvalidOperationError(position, "无法计算最大值");
        }

        return maxItem;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.MaxBy 方法暂不支持编译模式");
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
                throw new InvalidOperationException("无法从空列表中获取最大值元素");
            }

            var selector = arguments[0];
            var vm = VMContext.CurrentVM;

            object? maxItem = null;
            IComparable? maxValue = null;

            foreach (var item in list)
            {
                try
                {
                    var value = vm.CallFunctionObject(selector, [item]);

                    if (maxItem == null || (value is IComparable comparable && maxValue != null && comparable.CompareTo(maxValue) > 0))
                    {
                        maxItem = item;
                        maxValue = value as IComparable;
                    }
                }
                catch
                {
                    // 忽略执行错误
                }
            }

            if (maxItem == null)
            {
                throw new InvalidOperationException("无法计算最大值");
            }

            return maxItem;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
