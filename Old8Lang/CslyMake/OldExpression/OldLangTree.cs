using System.Text;
using sly.lexer;

namespace Old8Lang.CslyMake.OldExpression;

public interface OldLangTree
{
    public static string ListToString<T>(List<T> a)
    {
        StringBuilder builder = new StringBuilder();
        foreach (var expr in a)
            builder.Append(expr+" ");
        return builder.ToString();
    }
}