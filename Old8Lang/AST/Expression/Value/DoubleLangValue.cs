using Old8Lang.LangParser;
using System.Globalization;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class DoubleLangValue(double doubleValue, SourcePosition position = default) : LangValueType(position)
{
    public readonly double Value = doubleValue;

    public override LangValueType Plus(LangValueType otherLangValueType)
    {
        if (otherLangValueType is not IntLangValue and not DoubleLangValue)
            throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherLangValueType.GetType().Name}' 的加法操作");
        
        var result = Value + double.Parse(otherLangValueType.ToString());
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new OverflowError(this, "浮点数加法");
        }
        return new DoubleLangValue(result);
    }

    public override LangValueType Minus(LangValueType otherLangValueType)
    {
        if (otherLangValueType is not IntLangValue and not DoubleLangValue)
            throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherLangValueType.GetType().Name}' 的减法操作");
        
        var result = Value - double.Parse(otherLangValueType.ToString());
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new OverflowError(this, "浮点数减法");
        }
        return new DoubleLangValue(result);
    }

    public override LangValueType Times(LangValueType otherLangValueType)
    {
        if (otherLangValueType is not IntLangValue and not DoubleLangValue)
            throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherLangValueType.GetType().Name}' 的乘法操作");
        
        var result = Value * double.Parse(otherLangValueType.ToString());
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new OverflowError(this, "浮点数乘法");
        }
        return new DoubleLangValue(result);
    }

    public override LangValueType Divide(LangValueType otherLangValueType) 
    {
        if (otherLangValueType is IntLangValue intValue)
        {
            if (intValue.Value == 0)
            {
                throw new ZeroDivisionError(this);
            }
            return new DoubleLangValue(Value / intValue.Value);
        }
        if (otherLangValueType is DoubleLangValue doubleValue)
        {
            if (Math.Abs(doubleValue.Value) < 0.000001)
            {
                throw new ZeroDivisionError(this);
            }
            return new DoubleLangValue(Value / doubleValue.Value);
        }
        throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherLangValueType.GetType().Name}' 的除法操作");
    }

    public override LangValueType Mod(LangValueType otherLangValueType)
    {
        double divisor;
        if (otherLangValueType is IntLangValue intValue)
        {
            if (intValue.Value == 0)
            {
                throw new ZeroDivisionError(this);
            }
            divisor = intValue.Value;
        }
        else if (otherLangValueType is DoubleLangValue doubleValue)
        {
            if (Math.Abs(doubleValue.Value) < 0.000001)
            {
                throw new ZeroDivisionError(this);
            }
            divisor = doubleValue.Value;
        }
        else
        {
            throw new InvalidOperationError(this, $"不支持浮点数与类型 '{otherLangValueType.GetType().Name}' 的取模操作");
        }
        
        var result = Value % divisor;
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new OverflowError(this, "浮点数取模");
        }
        return new DoubleLangValue(result);
    }


    public override bool Less(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value < d.Value;
        if (otherValue is IntLangValue i)
            return Value < i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool Greater(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value > d.Value;
        if (otherValue is IntLangValue i)
            return Value > i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool LessEqual(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value <= d.Value;
        if (otherValue is IntLangValue i)
            return Value <= i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool GreaterEqual(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value >= d.Value;
        if (otherValue is IntLangValue i)
            return Value >= i.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is DoubleLangValue b)
            return Math.Abs(Value - b.Value) < 0.03;
        return false;
    }

    public override object GetValue() => Value;

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value) throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => new IntLangValue((int)Value),
            "Bool" or "bool" => new BoolLangValue(Value > 0),
            "String" or "string" => new StringLangValue(Value.ToString(CultureInfo.InvariantCulture)),
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