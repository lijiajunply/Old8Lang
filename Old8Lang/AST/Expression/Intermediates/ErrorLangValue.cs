using System.Text;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 错误类
/// </summary>
/// <param name="value"></param>
public class ErrorLangValue(Old8Exception value) : LangValueType
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override string ToString()
    {
        const string reset = "\u001b[0m";
        const string red = "\u001b[31m";
        const string yellow = "\u001b[33m";
        StringBuilder sb = new();

        sb.AppendLine($"{red}Old8Lang Error: {value.ErrorCode}{reset}");
        sb.AppendLine($"{yellow}{value.Position}{reset}");

        return sb.ToString();
    }
}