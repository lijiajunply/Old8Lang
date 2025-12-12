using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 静态值，不可访问
/// </summary>
/// <param name="position">位置信息</param>
public class VoidLangValue(SourcePosition position = default) : LangValueType(position)
{

    public override object GetValue() => throw new InvalidOperationError(this, "尝试访问无效值（VoidValue）");

    public override LangValueType Run(LangParser.VariateManager manager) => this;

    public override string ToString() =>
        throw new InvalidOperationError(this, "尝试将无效值（VoidValue）转换为字符串");

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // void 类型不需要加载任何值到栈上
        // 什么都不做
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(void);
    }
}