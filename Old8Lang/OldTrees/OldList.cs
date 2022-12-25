using System.Text;

namespace Old8Lang.OldTrees;

public class OldList : OldTree
{
    private string _location;
    public new List<OldTree>? Children { get; set; }
    public OldList(List<OldTree> list)
    {
        Children = list;
    }
    public new OldTree Child(int i) =>  Children[i];
    public new int ChildrenNum() => Children.Count();

    public override String ToString() {
        StringBuilder builder = new StringBuilder();
        builder.Append('(');
        String sep = "";
        foreach (var VARIABLE in Children)
        {
            builder.Append(sep);
            sep = " ";
            builder.Append(VARIABLE.ToString());
        }
        return builder.Append(')').ToString();
    }
    public new String Location() {
        foreach (var t in Children) {
            String s = t.Location();
            if (s != null)
                return s;
        }
        return null;
    }
}