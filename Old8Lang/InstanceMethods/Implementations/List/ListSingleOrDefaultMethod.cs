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
/// List.SingleOrDefault(defaultValue) 或 List.SingleOrDefault(predicate, defaultValue) - 安全获取唯一元素
/// </summary>
public class ListSingleOrDefaultMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
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

    /// <summary>
    /// IL 模式：接收者是原生 <c>List&lt;T&gt;</c>，谓词（可选）是编译好的 .NET 委托，
    /// 默认值是 Old8Lang 值。结果按 IL 模式的原生表示返回。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 接收者：原生集合 -> Old8Lang 值
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        // 其余实参（谓词 / 默认值）同样处理：Wrap 内部会把 Delegate 转成函数值
        for (var i = 0; i < parameters.Count; i++)
        {
            IlValueBridge.EmitLoadWrapped(parameters[i], ilGenerator, local);
        }

        var helperMethod = typeof(ListSingleOrDefaultMethod)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .First(m => m.Name == nameof(SingleOrDefaultHelper) && m.GetParameters().Length == parameters.Count + 1);
        ilGenerator.Emit(OpCodes.Call, helperMethod);
    }

    /// <summary>
    /// IL 模式的辅助方法（SingleOrDefault(defaultValue) 形式）：语义与 <see cref="ExecuteInternal"/> 一致。
    /// </summary>
    public static object? SingleOrDefaultHelper(LangValueType instance, LangValueType defaultValue)
    {
        if (instance is not ListLangValue list)
        {
            throw new ArgumentException($"实例必须是列表，实际是 {instance.GetType().Name}");
        }

        if (list.Values.Count == 0 || list.Values.Count > 1)
        {
            return IlValueBridge.Unwrap(defaultValue);
        }

        return IlValueBridge.Unwrap(list.Values[0]);
    }

    /// <summary>
    /// IL 模式的辅助方法（SingleOrDefault(predicate, defaultValue) 形式）：语义与 <see cref="ExecuteInternal"/> 一致。
    /// </summary>
    public static object? SingleOrDefaultHelper(LangValueType instance, LangValueType predicate, LangValueType defaultValue)
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
        LangValueType? foundItem = null;
        var foundCount = 0;

        foreach (var item in list.Values)
        {
            try
            {
                var result = func.Run(manager, [item]);
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
            return IlValueBridge.Unwrap(defaultValue);
        }

        return IlValueBridge.Unwrap(foundItem!);
    }

    /// <summary>
    /// IL 模式下 helper 返回的是「已转换回原生表示」的元素值/默认值，与 <c>SingleOrDefaultHelper</c> 的返回类型一致。
    /// 声明为 <see cref="object"/> 而不是 <see cref="LangValueType"/>，这样结果可以继续参与原生运算
    /// （IL 生成器会把 <c>object</c> 自动拆箱为具体数值类型）。
    /// </summary>
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
