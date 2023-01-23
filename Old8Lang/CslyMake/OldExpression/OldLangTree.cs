using System.Text;
using sly.lexer;

namespace Old8Lang.CslyMake.OldExpression;

public interface OldLangTree
{
    public static string ListToString<T>(List<T> a)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < a.Count; i++)
        {
            string b = i == 0 && a.Count == 1|| i == a.Count-1 ? "" : ",";
            builder.Append(a[i]+b);
        }
        return builder.ToString();
    }
}