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

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Sum(selector) - 对列表元素应用选择器后求和
/// </summary>
public class LangListSumWithSelectorMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Sum", "sum"];
    public override string[] ParameterNames => ["selector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    /// <summary>
    /// 参数类型：selector 必须是函数
    /// </summary>
    public override Type?[]? ParameterTypes => [typeof(FuncLangValue)];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "对列表元素应用选择器后求和";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationError(position, "无法对空列表求和");
        }

        var selector = parameters[0].Run(manager) as FuncLangValue;
        if (selector == null)
        {
            throw new ArgumentError(position, "selector 参数必须是函数类型");
        }

        double sum = 0;
        bool hasDouble = false;

        foreach (var item in items)
        {
            var value = selector.Run(manager, [item]);

            if (value is IntLangValue intValue)
            {
                sum += intValue.Value;
            }
            else if (value is DoubleLangValue doubleValue)
            {
                sum += doubleValue.Value;
                hasDouble = true;
            }
            else
            {
                throw new InvalidOperationError(position, $"选择器返回的值必须是数字类型，但得到了 {value.GetType().Name}");
            }
        }

        return hasDouble ? new DoubleLangValue(sum) : new IntLangValue((int)sum);
    }

    /// <summary>
    /// IL 模式：接收者是原生集合，selector 是编译好的 .NET 委托。
    /// 先把两者转换成 Old8Lang 值调用 helper，结果以 <see cref="LangValueType"/> 返回。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(LangListSumWithSelectorMethod).GetMethod(nameof(SumWithSelectorHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：对列表元素应用选择器后求和。
    /// 与 <see cref="ExecuteInternal"/> 语义一致（整数返回 IntLangValue，出现小数则返回 DoubleLangValue）。
    /// </summary>
    public static LangValueType SumWithSelectorHelper(LangValueType instance, LangValueType selector)
    {
        var items = GetIlItems(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("无法对空列表求和");
        }

        if (selector is not FuncLangValue func)
        {
            throw new ArgumentException("selector 参数必须是函数类型");
        }

        var manager = new VariateManager();
        double sum = 0;
        bool hasDouble = false;

        foreach (var item in items)
        {
            var value = func.Run(manager, [item]);

            if (value is IntLangValue intValue)
            {
                sum += intValue.Value;
            }
            else if (value is DoubleLangValue doubleValue)
            {
                sum += doubleValue.Value;
                hasDouble = true;
            }
            else
            {
                throw new InvalidOperationException($"选择器返回的值必须是数字类型，但得到了 {value.GetType().Name}");
            }
        }

        return hasDouble ? new DoubleLangValue(sum) : new IntLangValue((int)sum);
    }

    /// <summary>
    /// 与 <see cref="BaseLangListMethod.GetItems"/> 等价的静态版本（helper 必须是静态方法）。
    /// </summary>
    private static List<LangValueType> GetIlItems(LangValueType instance)
    {
        if (instance is ILangList langList)
        {
            return langList.GetItems().ToList();
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance.GetType().Name}");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var items = GetItemsForVM(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("无法对空列表求和");
        }

        if (arguments.Length == 0)
        {
            throw new ArgumentException("需要一个选择器参数");
        }

        var selector = arguments[0];
        var vm = VMContext.CurrentVM;

        double sum = 0;
        bool hasDouble = false;

        foreach (var item in items)
        {
            var value = vm.CallFunctionObject(selector, [item]);

            if (value is int intValue)
            {
                sum += intValue;
            }
            else if (value is double doubleValue)
            {
                sum += doubleValue;
                hasDouble = true;
            }
            else if (value != null)
            {
                // 尝试转换为数字
                try
                {
                    var numValue = Convert.ToDouble(value);
                    sum += numValue;
                    if (value is not int)
                    {
                        hasDouble = true;
                    }
                }
                catch
                {
                    throw new InvalidOperationException($"选择器返回的值必须是数字类型，但得到了 {value.GetType().Name}");
                }
            }
        }

        return hasDouble ? sum : (int)sum;
    }
}
