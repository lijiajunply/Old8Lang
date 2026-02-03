using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Remove 方法 - 从列表中移除指定元素
/// </summary>
public class ListRemoveMethod : BaseInstanceMethod
{
    public override string[] Names => ["Remove", "remove"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["item"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var itemToRemove = parameters[0].Run(manager);

        for (var i = 0; i < list.Values.Count; i++)
        {
            var v = list.Values[i];
            bool isEqual = false;

            // 使用值比较而不是引用比较
            if (v.GetType() == itemToRemove.GetType())
            {
                // 对于 IntLangValue
                if (v is IntLangValue intV && itemToRemove is IntLangValue intItem)
                {
                    isEqual = intV.Value == intItem.Value;
                }
                // 对于 DoubleLangValue
                else if (v is DoubleLangValue doubleV && itemToRemove is DoubleLangValue doubleItem)
                {
                    isEqual = Math.Abs(doubleV.Value - doubleItem.Value) < 0.0000001;
                }
                // 对于 StringLangValue
                else if (v is StringLangValue strV && itemToRemove is StringLangValue strItem)
                {
                    isEqual = strV.Value == strItem.Value;
                }
                // 对于 BoolLangValue
                else if (v is BoolLangValue boolV && itemToRemove is BoolLangValue boolItem)
                {
                    isEqual = boolV.Value == boolItem.Value;
                }
                // 对于 CharLangValue
                else if (v is CharLangValue charV && itemToRemove is CharLangValue charItem)
                {
                    isEqual = charV.Value == charItem.Value;
                }
                else
                {
                    isEqual = v.Equals(itemToRemove);
                }
            }
            else
            {
                isEqual = v.Equals(itemToRemove);
            }

            if (isEqual)
            {
                var removed = list.Values[i];
                list.Values.RemoveAt(i);
                return removed;
            }
        }

        throw new InvalidOperationError(position, "找不到要移除的元素");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 获取 Values 字段
        var valuesField = typeof(ListLangValue).GetField("Values");
        ilGenerator.Emit(OpCodes.Ldfld, valuesField!);

        // 加载要移除的元素
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法来移除元素
        var removeHelperMethod = typeof(ListRemoveMethod).GetMethod(nameof(RemoveHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, removeHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：从列表中移除元素
    /// </summary>
    public static LangValueType RemoveHelper(List<LangValueType> list, LangValueType itemToRemove)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(itemToRemove))
            {
                var removed = list[i];
                list.RemoveAt(i);
                return removed;
            }
        }

        throw new Exception("找不到要移除的元素");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            var itemToRemove = arguments[0];

            for (var i = 0; i < list.Count; i++)
            {
                if (Equals(list[i], itemToRemove))
                {
                    var removed = list[i];
                    list.RemoveAt(i);
                    return removed;
                }
            }

            throw new Exception("找不到要移除的元素");
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
