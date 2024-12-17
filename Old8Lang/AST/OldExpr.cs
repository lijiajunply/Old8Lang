using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST;

public class OldExpr : OldLangTree
{
    public virtual ValueType Run(VariateManager Manager) => new VoidValue();

    public virtual void GenerateILValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }

    public virtual void SetValueToIL(ILGenerator ilGenerator, LocalManager local,string idName)
    {
        throw new NotImplementedException();
    }
    
    public virtual Type? OutputType(LocalManager local)
    {
        return null;
    }
}