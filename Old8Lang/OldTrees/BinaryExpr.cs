namespace Old8Lang.OldTrees;

/// <summary>
/// 
/// </summary>
public class BinaryExpr : OldList {
    public BinaryExpr(List<OldTree> c) : base(c) { }
    public OldTree left() { return Child(0); }
    public String Operator() => ((OldLeaf)Child(1)).token().ToString();

    public OldTree right() { return Child(2); }
}