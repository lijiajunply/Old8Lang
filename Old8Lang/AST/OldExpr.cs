using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.LangParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST;

public class OldExpr : IOldLangTree
{
    /// <inheritdoc />
    public SourcePosition Position { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    public OldExpr(SourcePosition position = default)
    {
        Position = position;
    }
    
    public virtual ValueType Run(VariateManager manager) => new VoidValue();

    public virtual void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }

    public virtual void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        LoadIlValue(ilGenerator, local);
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