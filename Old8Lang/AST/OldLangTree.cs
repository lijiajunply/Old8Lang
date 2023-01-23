using System.Text;

namespace Old8Lang.AST;

public interface OldLangTree
{
    public static string ListToString<T>(List<T> a)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < a.Count; i++)
        {
            var b = i == 0 && a.Count == 1 || i == a.Count-1 ? "" : ",";
            builder.Append(a[i]+b);
        }
        return builder.ToString();
    }
    public static string ArrayToString<T>(T[] a)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < a.Length; i++)
        {
            var b = i == 0 && a.Length == 1 || i == a.Length-1 ? "" : ",";
            builder.Append(a[i]+b);
        }
        return builder.ToString();
    }
}