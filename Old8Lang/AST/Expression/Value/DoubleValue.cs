using Old8Lang.LangParser;
using System.Globalization;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class DoubleValue(double doubleValue, SourcePosition position = default) : ValueType(position)
{
    public readonly double Value = doubleValue;

    public override ValueType Plus(ValueType otherValueType)
    {
        if (otherValueType is not IntValue and not DoubleValue)
            throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherValueType.GetType().Name}' 的加法操作");
        
        var result = Value + double.Parse(otherValueType.ToString());
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new OverflowError(this, "浮点数加法");
        }
        return new DoubleValue(result);
    }

    public override ValueType Minus(ValueType otherValueType)
    {
        if (otherValueType is not IntValue and not DoubleValue)
            throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherValueType.GetType().Name}' 的减法操作");
        
        var result = Value - double.Parse(otherValueType.ToString());
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new OverflowError(this, "浮点数减法");
        }
        return new DoubleValue(result);
    }

    public override ValueType Times(ValueType otherValueType)
    {
        if (otherValueType is not IntValue and not DoubleValue)
            throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherValueType.GetType().Name}' 的乘法操作");
        
        var result = Value * double.Parse(otherValueType.ToString());
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new OverflowError(this, "浮点数乘法");
        }
        return new DoubleValue(result);
    }

    public override ValueType Divide(ValueType otherValueType) 
    {
        if (otherValueType is IntValue intValue)
        {
            if (intValue.Value == 0)
            {
                throw new ZeroDivisionError(this);
            }
            return new DoubleValue(Value / intValue.Value);
        }
        if (otherValueType is DoubleValue doubleValue)
        {
            if (Math.Abs(doubleValue.Value) < 0.000001)
            {
                throw new ZeroDivisionError(this);
            }
            return new DoubleValue(Value / doubleValue.Value);
        }
        throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherValueType.GetType().Name}' 的除法操作");
    }


    public override bool Less(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value < d.Value;
        if (otherValue is IntValue i)
            return Value < i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool Greater(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value > d.Value;
        if (otherValue is IntValue i)
            return Value > i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool LessEqual(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value <= d.Value;
        if (otherValue is IntValue i)
            return Value <= i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool GreaterEqual(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value >= d.Value;
        if (otherValue is IntValue i)
            return Value >= i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is DoubleValue b)
            return Math.Abs(Value - b.Value) < 0.03;
        return false;
    }

    public override object GetValue() => Value;

    public override ValueType Converse(ValueType otherValueType, VariateManager manager)
    {
        if (otherValueType is not TypeValue value) throw new TypeError(this, "TypeValue", otherValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => new IntValue((int)Value),
            "Bool" or "bool" => new BoolValue(Value > 0),
            "String" or "string" => new StringValue(Value.ToString(CultureInfo.InvariantCulture)),
            "char" or "Char" => throw new TypeError(this, "char", $"无法将 double 转换为 char"),
            "Double" or "double" => this,
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }
    
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldc_R8, Value);
    }

    public override Type OutputType(LocalManager local) => Value.GetType();
}