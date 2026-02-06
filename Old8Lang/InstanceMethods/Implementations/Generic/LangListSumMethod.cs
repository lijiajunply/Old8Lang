using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Sum() - 求和
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListSumMethod : BaseLangListMethod
{
    public override string[] Names => ["Sum", "sum"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    /// <summary>
    /// 参数类型：无参数
    /// </summary>
    public override Type?[]? ParameterTypes => [];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "对列表中的所有元素求和";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count == 0)
        {
            return IntLangValue.Create(0, position);
        }

        // 检查第一个元素的类型来决定返回类型
        var firstItem = items[0];
        bool isDouble = firstItem is DoubleLangValue;

        if (isDouble)
        {
            double sum = 0;
            foreach (var item in items)
            {
                if (item is DoubleLangValue doubleValue)
                {
                    sum += doubleValue.Value;
                }
                else if (item is IntLangValue intValue)
                {
                    sum += intValue.Value;
                }
                else
                {
                    throw new TypeError(item, "数值类型", item.GetType().Name);
                }
            }
            return DoubleLangValue.Create(sum, position);
        }
        else
        {
            int sum = 0;
            foreach (var item in items)
            {
                if (item is IntLangValue intValue)
                {
                    sum += intValue.Value;
                }
                else if (item is DoubleLangValue doubleValue)
                {
                    // 如果遇到 double，转换为 double 计算
                    double doubleSum = sum + doubleValue.Value;
                    for (int i = items.IndexOf(item) + 1; i < items.Count; i++)
                    {
                        if (items[i] is IntLangValue iv)
                            doubleSum += iv.Value;
                        else if (items[i] is DoubleLangValue dv)
                            doubleSum += dv.Value;
                    }
                    return DoubleLangValue.Create(doubleSum, position);
                }
                else
                {
                    throw new TypeError(item, "数值类型", item.GetType().Name);
                }
            }
            return IntLangValue.Create(sum, position);
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListSumMethod).GetMethod(nameof(SumHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static LangValueType SumHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        if (items.Count == 0)
        {
            return IntLangValue.Create(0);
        }

        bool isDouble = items[0] is DoubleLangValue;
        if (isDouble)
        {
            double sum = 0;
            foreach (var item in items)
            {
                if (item is DoubleLangValue dv)
                    sum += dv.Value;
                else if (item is IntLangValue iv)
                    sum += iv.Value;
            }
            return DoubleLangValue.Create(sum);
        }
        else
        {
            int sum = 0;
            foreach (var item in items)
            {
                if (item is IntLangValue iv)
                    sum += iv.Value;
                else if (item is DoubleLangValue dv)
                    return DoubleLangValue.Create(sum + dv.Value + items.Skip(items.IndexOf(item) + 1)
                        .Sum(x => x is IntLangValue i ? i.Value : (x as DoubleLangValue)?.Value ?? 0));
            }
            return IntLangValue.Create(sum);
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            return SumHelper(langList);
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance?.GetType().Name}");
    }
}
