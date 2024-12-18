using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST;

public class OldExpr : OldLangTree
{
    public virtual ValueType Run(VariateManager Manager) => new VoidValue();

    public virtual void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }

    public virtual void SetValueToIL(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        LoadILValue(ilGenerator, local);
        var type = OutputType(local);
        if (type == null) return;
        var b = local.GetLocalVar(idName);
        var valueLocal = ilGenerator.DeclareLocal(type);
        if (b != null)
        {
            if (b.LocalType != type)
            {
                local.RemoveLocalVar(idName);
                local.AddLocalVar(idName, valueLocal);
                ilGenerator.Emit(OpCodes.Stloc, valueLocal.LocalIndex);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Stloc, b.LocalIndex);
            }
            return;
        }
        ilGenerator.Emit(OpCodes.Stloc, valueLocal.LocalIndex);
        local.AddLocalVar(idName, valueLocal);
    }

    public virtual Type? OutputType(LocalManager local)
    {
        throw new NotImplementedException();
    }
}