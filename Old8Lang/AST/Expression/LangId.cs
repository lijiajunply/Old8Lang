using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression;

public class LangId(string name, string assumptionType = "", SourcePosition position = default) : OldExpr(position)
{
    public readonly string IdName = name;
    public override string ToString() => IdName;
    public string AssumptionType { get; } = assumptionType;

    public override bool Equals(object? obj)
    {
        var a = obj as LangId;
        return a?.IdName == IdName;
    }

    public override int GetHashCode()
    {
        return IdName.GetHashCode();
    }

    public override LangValueType Run(LangParser.VariateManager manager) 
    {
        if (IdName == "this")
        {
            // 直接从变量储存器中获取名为"this"的变量
            var thisValue = manager.GetValue(new LangId("this"));
            if (thisValue != null)
            {
                return thisValue;
            }
            
            // 如果没有找到，抛出NameError异常，因为this关键字只能在类的方法中使用
            throw new NameError(this, "this");
        }
        return manager.GetValue(this) ?? throw new NameError(this, IdName);
    }

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