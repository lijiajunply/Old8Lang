using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;

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
    
    public virtual LangValueType Run(VariateManager manager) => throw new InvalidOperationError(this, "表达式未实现Run方法");

    public virtual void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new InvalidOperationError(this, "表达式未实现LoadIlValue方法", "请在子类中实现LoadIlValue方法");
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
        throw new InvalidOperationError(this, "表达式未实现LoadIlValue方法", "请在子类中实现LoadIlValue方法");
    }
}