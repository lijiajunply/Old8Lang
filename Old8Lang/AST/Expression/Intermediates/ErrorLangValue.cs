using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 错误类
/// </summary>
/// <param name="value"></param>
public class ErrorLangValue(Old8Exception value) : LangValueType
{
    public override string ToString() => $"{value.Message}";
}