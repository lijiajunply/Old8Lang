using Old8Lang.Lexer;

namespace Old8Lang.OldTrees;

public class OldLeaf : OldTree
{
    private static List<OldTree> empty = new List<OldTree>();
    private static Token _token;
    public OldLeaf(Token t)
    {
        _token = t;
    }
    OldTree OldTree.Child(int i) => null;
    int OldTree.ChildrenNum() { return 0; }
    public new List<OldTree>? Children { get; set; } = null;
    public override string ToString() => _token.ToString();
    String OldTree.Location() =>  $"at location{ _token.GetLineNum()}";
    public Token token() => _token; 
}