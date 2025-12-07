using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class BoolValue(bool value, SourcePosition position = default) : ValueType(position)
{
    public readonly bool Value = value;
    public override string ToString() => Value.ToString();
    public override ValueType Run(VariateManager manager) => this;

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is BoolValue b)
            return Value == b.Value;
        return false;
    }

    public override ValueType Converse(ValueType otherValueType, VariateManager manager)
    {
        if (otherValueType is not TypeValue value) throw new TypeError(this, "TypeValue", otherValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => new IntValue(Value ? 1 : 0),
            "Bool" or "bool" => this,
            "String" or "string" => new StringValue(Value.ToString()),
            "char" or "Char" => new CharValue(Value ? '1' : '0'),
            "Double" or "double" => new DoubleValue(Value ? 1.0 : 0.0),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }
    
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
    }

    public override Type OutputType(LocalManager local) => Value.GetType();
}