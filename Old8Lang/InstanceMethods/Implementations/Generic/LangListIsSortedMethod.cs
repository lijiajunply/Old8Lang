using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Implementations.Array;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.IsSorted() - 检查是否已排序
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListIsSortedMethod : BaseLangListMethod
{
    public override string[] Names => ["IsSorted", "isSorted"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count <= 1)
        {
            return BoolLangValue.Create(true, position);
        }

        for (int i = 0; i < items.Count - 1; i++)
        {
            if (items[i].Greater(items[i + 1]))
            {
                return BoolLangValue.Create(false, position);
            }
        }

        return BoolLangValue.Create(true, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListIsSortedMethod).GetMethod(nameof(IsSortedHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool IsSortedHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();

        if (items.Count <= 1)
        {
            return true;
        }

        for (int i = 0; i < items.Count - 1; i++)
        {
            if (items[i].Greater(items[i + 1]))
            {
                return false;
            }
        }

        return true;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            return IsSortedHelper(langList);
        }

        // VM 模式下数组字面量是原生 object?[]、列表是 List<object?>，都不实现 ILangList；
        // 用 GetItemsForVM 兜底（与 LangListCountMethod 的 ICollection 兜底同一模式），
        // 使 Array.IsSorted()/List.IsSorted() 在 VM 下可用。
        var items = GetItemsForVM(instance);

        for (var i = 0; i < items.Count - 1; i++)
        {
            if (ArraySortVmSupport.Compare(items[i], items[i + 1]) > 0)
            {
                return false;
            }
        }

        return true;
    }
}
