using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang;

namespace Old8Lang.AST.Expression;

public class OldId(string name,string assumptionType = "", SourcePosition position = default) : OldExpr(position)
{
    public readonly string IdName = name;
    public override string ToString() => IdName;
    private string AssumptionType { get; } = assumptionType;

    public override bool Equals(object? obj)
    {
        var a = obj as OldId;
        return a?.IdName == IdName;
    }

    public override int GetHashCode()
    {
        return IdName.GetHashCode();
    }

    public override ValueType Run(LangParser.VariateManager manager) => manager.GetValue(this) ?? new VoidValue();

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        var value = local.GetLocalVar(IdName);
        if (value is null) return;
        ilGenerator.Emit(OpCodes.Ldloc, value.LocalIndex);
    }

    public override Type OutputType(LocalManager local)
    {
        if (!string.IsNullOrEmpty(AssumptionType))
        {
            return AssumptionType switch
            {
                "int" => typeof(int),
                "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "void" => typeof(void),
                _ => typeof(object)
            };
        }

        if (local.InClassEnv != null && IdName == "this")
        {
            return local.InClassEnv;
        }
        var value = local.GetLocalVar(IdName);
        return value?.LocalType ?? typeof(object);
    }
}