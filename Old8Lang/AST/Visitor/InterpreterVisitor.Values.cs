using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// InterpreterVisitor - Value 节点的 Visit 方法实现
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>
    /// 访问 IntLangValue 节点
    /// </summary>
    public LangValueType VisitIntLangValue(IntLangValue node)
    {
        // 值类型节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 DoubleLangValue 节点
    /// </summary>
    public LangValueType VisitDoubleLangValue(DoubleLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 StringLangValue 节点
    /// </summary>
    public LangValueType VisitStringLangValue(StringLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 BoolLangValue 节点
    /// </summary>
    public LangValueType VisitBoolLangValue(BoolLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 CharLangValue 节点
    /// </summary>
    public LangValueType VisitCharLangValue(CharLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 NullLangValue 节点
    /// </summary>
    public LangValueType VisitNullLangValue(NullLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 VoidLangValue 节点
    /// </summary>
    public LangValueType VisitVoidLangValue(VoidLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 ArrayLangValue 节点
    /// </summary>
    public LangValueType VisitArrayLangValue(ArrayLangValue node)
    {
        // 迁移自 ArrayLangValue.Run()
        // 执行数组中的表达式并返回自身
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 ListLangValue 节点
    /// </summary>
    public LangValueType VisitListLangValue(ListLangValue node)
    {
        // 迁移自 ListLangValue.Run()
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 DictionaryLangValue 节点
    /// </summary>
    public LangValueType VisitDictionaryLangValue(DictionaryLangValue node)
    {
        // 迁移自 DictionaryLangValue.Run()
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 TupleLangValue 节点
    /// </summary>
    public LangValueType VisitTupleLangValue(TupleLangValue node)
    {
        // 迁移自 TupleLangValue.Run()
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 RangeLangValue 节点
    /// </summary>
    public LangValueType VisitRangeLangValue(RangeLangValue node)
    {
        // 迁移自 RangeLangValue.Run()
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 SliceLangValue 节点
    /// </summary>
    public LangValueType VisitSliceLangValue(SliceLangValue node)
    {
        // 迁移自 SliceLangValue.Run()
        return node.Run(_manager);
    }
}
