using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class IntValue(int intValue) : ValueType
{
    public int Value { get; set; } = intValue;
    public override string ToString() => Value.ToString();

    public override ValueType Plus(ValueType otherValueType)
    {
        if (otherValueType is StringValue s)
            return new StringValue(Value + s.Value);
        if (otherValueType is CharValue c)
            return new CharValue(Convert.ToChar(Value + c.Value));
        if (otherValueType is DoubleValue)
            return otherValueType.Plus(this);
        if (otherValueType is IntValue)
            return new IntValue(Value + int.Parse(otherValueType.ToString()));

        return new VoidValue();
    }

    public override ValueType Minus(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue)
            return otherValueType.Minus(this);
        if (otherValueType is IntValue)
            return new IntValue(Value - int.Parse(otherValueType.ToString()));
        return new VoidValue();
    }

    public override ValueType Times(ValueType otherValueType)
    {
        if (otherValueType is StringValue)
            return otherValueType.Times(this);
        if (otherValueType is CharValue)
            return otherValueType.Times(this);
        if (otherValueType is DoubleValue)
            return otherValueType.Times(this);
        if (otherValueType is IntValue)
            return new IntValue(Value * int.Parse(otherValueType.ToString()));
        return new VoidValue();
    }

    public override ValueType Divide(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue)
            return otherValueType.Divide(this);
        if (otherValueType is IntValue)
            return new IntValue(Value / int.Parse(otherValueType.ToString()));
        return new VoidValue();
    }

    public override bool Less(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value < d.Value;
        if (otherValue is IntValue i)
            return Value < i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
    }

    public override bool Greater(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value > d.Value;
        if (otherValue is IntValue i)
            return Value > i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
    }

    public override bool LessEqual(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value <= d.Value;
        if (otherValue is IntValue i)
            return Value <= i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
    }

    public override bool GreaterEqual(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value >= d.Value;
        if (otherValue is IntValue i)
            return Value >= i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
    }

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is IntValue b)
            return Value == b.Value;
        return false;
    }

    public override ValueType Converse(ValueType otherValueType, VariateManager _)
    {
        if (otherValueType is not TypeValue value) throw new Exception("the value is not a type");

        return value.Value switch
        {
            "Int" or "int" => this,
            "Bool" or "bool" => new BoolValue(Value > 0),
            "String" or "string" => new StringValue(Value.ToString()),
            "char" or "Char" => new CharValue(Convert.ToChar(Value)),
            "Double" or "double" => new DoubleValue(Value),
            _ => throw new Exception("not fount the type: " + value.Value)
        };
    }

    public override object GetValue() => Value;

    public override Type OutputType(LocalManager local) => Value.GetType();

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, Value);
    }
}

public static class IntOperatorStatic
{
    public static int Plus(int i, int j) => i + j;
    public static double Plus(int i, double j) => i + j;
    public static string Plus(int i, string j) => i + j;
    public static int Minus(int i, int j) => i - j;
    public static double Minus(int i, double j) => i - j;
    public static int Times(int i, int j) => i * j;
    public static double Times(int i, double j) => i * j;

    public static string Times(int i, string j)
    {
        var sb = new StringBuilder();
        for (var k = 0; k < j.Length; k++)
            sb.Append(i);
        return sb.ToString();
    }

    public static int Divide(int i, int j) => i / j;
    public static double Divide(int i, double j) => i / j;
    public static bool Greater(int i, int j) => i > j;
    public static bool Greater(int i, double j) => i > j;
    public static bool Less(int i, int j) => i < j;
    public static bool Less(int i, double j) => i < j;
    public static bool Equal(int i, int j) => i == j;
    public static bool Different(int i, int j) => i != j;
    public static bool LessEqual(int i, int j) => i <= j;
    public static bool LessEqual(int i, double j) => i <= j;
    public static bool GreaterEqual(int i, int j) => i >= j;
    public static bool GreaterEqual(int i, double j) => i >= j;
    public static bool And(bool i, bool j) => i && j;
    public static bool Or(bool i, bool j) => i || j;
    public static bool Xor(bool i, bool j) => i ^ j;
}