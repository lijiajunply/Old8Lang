using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class BoolValue(bool value) : ValueType
{
    public readonly bool Value = value;
    public override string ToString() => Value.ToString();
    public override ValueType Run(VariateManager Manager) => this;

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is BoolValue b)
            return Value == b.Value;
        return false;
    }

    public override ValueType Converse(ValueType otherValueType, VariateManager _)
    {
        if (otherValueType is not TypeValue value) throw new Exception("the value is not a type");

        return value.Value switch
        {
            "Int" or "int" => new IntValue(Value ? 1 : 0),
            "Bool" or "bool" => this,
            "String" or "string" => new StringValue(Value.ToString()),
            "char" or "Char" => new CharValue(Value ? '1' : '0'),
            "Double" or "double" => new DoubleValue(Value ? 1.0 : 0.0),
            _ => throw new Exception("not fount the type: " + value.Value)
        };
    }

    public override void SetValueToIL(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        var valueLocal = ilGenerator.DeclareLocal(typeof(bool));
        ilGenerator.Emit(Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, valueLocal);
        local.AddLocalVar(idName, valueLocal);
    }
}