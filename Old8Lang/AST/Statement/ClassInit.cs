using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class OldClassInit : OldStatement
{
    public OldAny Any { get; set; }
    public OldClassInit(OldAny any)
    {
        Any = any;
    }
    public override void Run(ref VariateManager Manager) => Manager.AddClassAndFunc(Any.Id,Any);

    public override string ToString() => Any.ToString();
}