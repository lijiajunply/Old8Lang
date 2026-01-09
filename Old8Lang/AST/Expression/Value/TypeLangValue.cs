using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 类型值
/// </summary>
public partial class TypeLangValue : LangValueType
{
    private readonly LangExpression? Expression;
    public string? Value { get; private set; }

    public TypeLangValue(LangExpression expression) => Expression = expression;
    public TypeLangValue(string value) => Value = value;

    public override LangValueType Run(VariateManager manager)
    {
        var result = Expression?.Run(manager);
        if (result is null) throw new InvalidOperationError(this, "类型表达式求值失败");
        Value = result.TypeToString();
        return this;
    }

    public override string ToString() => Value ?? "";
    public override object GetValue() => Value ?? "";

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载类型字符串值
        ilGenerator.Emit(OpCodes.Ldstr, Value ?? "");
    }

    public override Type OutputType(LocalManager local)
    {
        // TypeValue 表示类型信息，输出为字符串类型
        return typeof(string);
    }
}