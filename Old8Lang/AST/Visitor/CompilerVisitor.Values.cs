using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Value 节点的 Visit 方法实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 IntLangValue 节点
    /// </summary>
    public object? VisitIntLangValue(IntLangValue node)
    {
        // 迁移自 IntLangValue.LoadIlValue()
        _ilGenerator.Emit(OpCodes.Ldc_I4, node.Value);
        return null;
    }

    /// <summary>
    /// 访问 DoubleLangValue 节点
    /// </summary>
    public object? VisitDoubleLangValue(DoubleLangValue node)
    {
        // 迁移自 DoubleLangValue.LoadIlValue()
        _ilGenerator.Emit(OpCodes.Ldc_R8, node.Value);
        return null;
    }

    /// <summary>
    /// 访问 StringLangValue 节点
    /// </summary>
    public object? VisitStringLangValue(StringLangValue node)
    {
        // 迁移自 StringLangValue.LoadIlValue()
        _ilGenerator.Emit(OpCodes.Ldstr, node.Value);
        return null;
    }

    /// <summary>
    /// 访问 BoolLangValue 节点
    /// </summary>
    public object? VisitBoolLangValue(BoolLangValue node)
    {
        // 迁移自 BoolLangValue.LoadIlValue()
        _ilGenerator.Emit(node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        return null;
    }

    /// <summary>
    /// 访问 CharLangValue 节点
    /// </summary>
    public object? VisitCharLangValue(CharLangValue node)
    {
        // 迁移自 CharLangValue.LoadIlValue()
        _ilGenerator.Emit(OpCodes.Ldc_I4, (int)node.Value);
        return null;
    }

    /// <summary>
    /// 访问 NullLangValue 节点
    /// </summary>
    public object? VisitNullLangValue(NullLangValue node)
    {
        // 迁移自 NullLangValue.LoadIlValue()
        _ilGenerator.Emit(OpCodes.Ldnull);
        return null;
    }

    /// <summary>
    /// 访问 VoidLangValue 节点
    /// </summary>
    public object? VisitVoidLangValue(VoidLangValue node)
    {
        // Void值在编译器模式下不需要加载任何值到栈上
        return null;
    }
}
