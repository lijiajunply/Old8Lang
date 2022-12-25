namespace Old8Lang.OldTrees;

public interface OldTree
{
    public OldTree Child(int i) => Children[i];
    public int ChildrenNum() => Children.Count;
    public List<OldTree>? Children { get; set; }
    public string Location() => "";
    public List<OldTree>? GetChildren() => Children;
}