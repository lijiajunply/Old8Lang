using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class OldID(string name) : OldExpr
{
    public readonly string IdName = name;
    public override string ToString() => IdName;

    public override bool Equals(object? obj)
    {
        var a = obj as OldID;
        return a?.IdName == IdName;
    }

    public override int GetHashCode()
    {
        return IdName.GetHashCode();
    }

    public override ValueType Run(VariateManager Manager) => Manager.GetValue(this) ?? new VoidValue();

    public override void SetValueToIL(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        var value = local.GetLocalVar(idName);
        if (value is null) return;
        ilGenerator.Emit(OpCodes.Ldloc, value.LocalIndex); // 加载第一个本地变量的值
        var localVariable = ilGenerator.DeclareLocal(value.LocalType);
        ilGenerator.Emit(OpCodes.Stloc, localVariable); // 将值存储到第二个本地变量
    }

    public override void GenerateILValue(ILGenerator ilGenerator, LocalManager local)
    {
        var value = local.GetLocalVar(IdName);
        if (value is null) return;
        ilGenerator.Emit(OpCodes.Ldloc, value.LocalIndex);
    }

    public override Type? OutputType(LocalManager local)
    {
        var value = local.GetLocalVar(IdName);
        return value?.LocalType;
    }
}