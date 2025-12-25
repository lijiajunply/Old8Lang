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
}
