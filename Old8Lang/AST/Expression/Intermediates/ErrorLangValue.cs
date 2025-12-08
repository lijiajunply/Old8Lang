using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

public class ErrorLangValue(Old8Exception value) : LangValueType
{
    public override string ToString() => $"{value.Message}";
}