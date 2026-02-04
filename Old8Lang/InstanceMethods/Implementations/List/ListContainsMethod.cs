using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Contains 方法 - 检查列表是否包含指定元素
/// </summary>
public class ListContainsMethod : BaseInstanceMethod
{
    public override string[] Names => ["Contains", "contains"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["item"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var item = parameters[0].Run(manager);

        // 使用 LangValueType 的 Equals 方法进行比较
        // 对于基本类型，需要比较它们的值而不是引用
        var contains = list.Values.Any(v =>
        {
            // 如果两个对象类型相同，比较它们的值
            if (v.GetType() == item.GetType())
            {
                // 对于 IntLangValue
                if (v is IntLangValue intV && item is IntLangValue intItem)
                {
                    return intV.Value == intItem.Value;
                }
                // 对于 DoubleLangValue
                if (v is DoubleLangValue doubleV && item is DoubleLangValue doubleItem)
                {
                    return Math.Abs(doubleV.Value - doubleItem.Value) < 0.0000001;
                }
                // 对于 StringLangValue
                if (v is StringLangValue strV && item is StringLangValue strItem)
                {
                    return strV.Value == strItem.Value;
                }
                // 对于 BoolLangValue
                if (v is BoolLangValue boolV && item is BoolLangValue boolItem)
                {
                    return boolV.Value == boolItem.Value;
                }
                // 对于 CharLangValue
                if (v is CharLangValue charV && item is CharLangValue charItem)
                {
                    return charV.Value == charItem.Value;
                }
            }

            // 默认使用 Equals
            return v.Equals(item);
        });

        return new BoolLangValue(contains);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 获取 Values 字段
        var valuesField = typeof(ListLangValue).GetField("Values");
        ilGenerator.Emit(OpCodes.Ldfld, valuesField!);

        // 加载要查找的元素
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var containsHelperMethod = typeof(ListContainsMethod).GetMethod(nameof(ContainsHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, containsHelperMethod!);

        // 创建 BoolLangValue
        var boolCtor = typeof(BoolLangValue).GetConstructor([typeof(bool)]);
        ilGenerator.Emit(OpCodes.Newobj, boolCtor!);
    }

    /// <summary>
    /// 辅助方法：检查列表是否包含元素
    /// </summary>
    public static bool ContainsHelper(List<LangValueType> list, LangValueType item)
    {
        return list.Any(v => v.Equals(item));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            var item = arguments[0];
            return list.Contains(item);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
