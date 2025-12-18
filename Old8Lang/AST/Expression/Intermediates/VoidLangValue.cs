using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 静态值，不可访问
/// </summary>
/// <param name="position">位置信息</param>
public class VoidLangValue(SourcePosition position = default) : LangValueType(position)
{

    public override object GetValue() => throw new InvalidOperationError(this, "尝试访问无效值（VoidValue）");

    public override LangValueType Run(VariateManager manager) => this;

    public override string ToString() => ""; // VoidValue 转换为字符串时返回空字符串，而不是抛出异常

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