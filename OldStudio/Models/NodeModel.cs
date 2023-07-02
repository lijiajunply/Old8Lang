using sly.parser.generator.visitor.dotgraph;

namespace OldStudio.Models;

public class NodeModel
{
    private DotNode DotNode { get; set; }
 
    public NodeModel(DotNode dotNode)
    {
        DotNode = dotNode;
    }
 
    public override bool Equals(object? obj)
    {
        if (obj is not NodeModel node) return false;
        return node.DotNode == DotNode;
    }
 
    protected bool Equals(NodeModel other)
    {
        return DotNode.Equals(other.DotNode);
    }
 
    public override string ToString() => DotNode.Label;
}