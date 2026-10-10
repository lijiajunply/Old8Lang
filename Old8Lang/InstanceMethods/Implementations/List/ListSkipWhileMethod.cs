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
/// List.SkipWhile(predicate) - 跳过列表开头的元素，直到不满足条件
/// </summary>
public class ListSkipWhileMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["SkipWhile", "skipWhile"];
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

        foreach (var item in list.Values)
        {
            if (skipping)
            {
                try
                {
                    var predicateResult = predicate.Run(manager, [item]);
                    if (predicateResult is BoolLangValue { Value: false })
                    {
                        skipping = false;
                        result.Add(item);
                    }
                }
                catch
                {
                    skipping = false;
                    result.Add(item);
                }
            }
            else
            {
                result.Add(item);
            }
        }

        return new ListLangValue(result);
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

        var helperMethod = typeof(ListSkipWhileMethod).GetMethod(nameof(SkipWhileHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：语义与 <see cref="ExecuteInternal"/> 一致，返回原生列表。
    /// </summary>
    public static List<object?> SkipWhileHelper(LangValueType instance, LangValueType predicate)
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
        var result = new List<object?>();
        var skipping = true;

        foreach (var item in list.Values)
        {
            if (skipping)
            {
                try
                {
                    var predicateResult = func.Run(manager, [item]);
                    if (predicateResult is BoolLangValue { Value: false })
                    {
                        skipping = false;
                        result.Add(IlValueBridge.Unwrap(item));
                    }
                }
                catch
                {
                    skipping = false;
                    result.Add(IlValueBridge.Unwrap(item));
                }
            }
            else
            {
                result.Add(IlValueBridge.Unwrap(item));
            }
        }

        return result;
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

            foreach (var item in list)
            {
                if (skipping)
                {
                    try
                    {
                        var predicateResult = vm.CallFunctionObject(predicate, [item]);
                        if (predicateResult is bool boolResult && !boolResult)
                        {
                            skipping = false;
                            result.Add(item);
                        }
                    }
                    catch
                    {
                        skipping = false;
                        result.Add(item);
                    }
                }
                else
                {
                    result.Add(item);
                }
            }

            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
