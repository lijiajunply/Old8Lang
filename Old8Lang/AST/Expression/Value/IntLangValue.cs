using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// int 整数型
/// </summary>
/// <param name="intValue">int数据</param>
/// <param name="position">位置</param>
public class IntLangValue(int intValue = 0, SourcePosition position = default) : LangValueType(position), IPoolable
{
    public int Value = intValue;

    public override string ToString() => Value.ToString();

    public override LangValueType Plus(LangValueType otherLangValueType)
    {
        if (otherLangValueType is StringLangValue s)
            return StringLangValue.Create(Value + s.Value);
        if (otherLangValueType is CharLangValue c)
            return CharLangValue.Create(Convert.ToChar(Value + c.Value));
        if (otherLangValueType is DoubleLangValue)
            return otherLangValueType.Plus(this);
        if (otherLangValueType is IntLangValue otherInt)
        {
            try
            {
                checked
                {
                    return Create(Value + otherInt.Value);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowError(this, "整数加法");
            }
        }

        throw new InvalidOperationError(this, $"不支持整数与类型 '{otherLangValueType.GetType().Name}' 的加法操作");
    }

    public override LangValueType Minus(LangValueType otherLangValueType)
    {
        if (otherLangValueType is DoubleLangValue)
            return otherLangValueType.Minus(this);
        if (otherLangValueType is IntLangValue otherInt)
        {
            try
            {
                checked
                {
                    return Create(Value - otherInt.Value);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowError(this, "整数减法");
            }
        }

        throw new InvalidOperationError(this, $"不支持整数与类型 '{otherLangValueType.GetType().Name}' 的减法操作");
    }

    public override LangValueType Times(LangValueType otherLangValueType)
    {
        if (otherLangValueType is StringLangValue)
            return otherLangValueType.Times(this);
        if (otherLangValueType is CharLangValue)
            return otherLangValueType.Times(this);
        if (otherLangValueType is DoubleLangValue)
            return otherLangValueType.Times(this);
        if (otherLangValueType is IntLangValue otherInt)
        {
            try
            {
                checked
                {
                    return Create(Value * otherInt.Value);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowError(this, "整数乘法");
            }
        }

        throw new InvalidOperationError(this, $"不支持整数与类型 '{otherLangValueType.GetType().Name}' 的乘法操作");
    }

    public override LangValueType Divide(LangValueType otherLangValueType)
    {
        if (otherLangValueType is DoubleLangValue)
            return otherLangValueType.Divide(this);
        if (otherLangValueType is IntLangValue otherInt)
        {
            if (otherInt.Value == 0)
            {
                // 抛出自定义的ZeroDivisionError
                throw new ZeroDivisionError(this);
            }

            return Create(Value / otherInt.Value);
        }

        throw new InvalidOperationError(this, $"不支持整数与类型 '{otherLangValueType.GetType().Name}' 的除法操作");
    }

    public override LangValueType Mod(LangValueType otherLangValueType)
    {
        if (otherLangValueType is DoubleLangValue)
            return otherLangValueType.Mod(this);
        if (otherLangValueType is IntLangValue otherInt)
        {
            if (otherInt.Value == 0)
            {
                // 抛出自定义的ZeroDivisionError
                throw new ZeroDivisionError(this);
            }

            return Create(Value % otherInt.Value);
        }

        throw new InvalidOperationError(this, $"不支持整数与类型 '{otherLangValueType.GetType().Name}' 的取模操作");
    }

    public override LangValueType Power(LangValueType otherLangValueType)
    {
        if (otherLangValueType is DoubleLangValue doubleValue)
        {
            // 直接计算，不调用 doubleValue.Power(this)，避免顺序颠倒
            var result = Math.Pow(Value, doubleValue.Value);
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowError(this, "整数幂运算");
            }

            return DoubleLangValue.Create(result);
        }

        if (otherLangValueType is IntLangValue otherInt)
        {
            try
            {
                // 使用 Math.Pow 计算，然后根据结果类型返回
                var result = Math.Pow(Value, otherInt.Value);
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    throw new OverflowError(this, "整数幂运算");
                }

                // 如果结果是整数，返回 IntLangValue，否则返回 DoubleLangValue
                if (Math.Abs(result - Math.Floor(result)) < 0.01)
                {
                    return Create((int)result);
                }

                return DoubleLangValue.Create(result);
            }
            catch (OverflowException)
            {
                throw new OverflowError(this, "整数幂运算");
            }
        }

        throw new InvalidOperationError(this, $"不支持整数与类型 '{otherLangValueType.GetType().Name}' 的幂运算");
    }

    public override bool Less(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value < d.Value;
        if (otherValue is IntLangValue i)
            return Value < i.Value;
        if (otherValue is CharLangValue c)
            return Value < c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool Greater(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value > d.Value;
        if (otherValue is IntLangValue i)
            return Value > i.Value;
        if (otherValue is CharLangValue c)
            return Value > c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool LessEqual(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value <= d.Value;
        if (otherValue is IntLangValue i)
            return Value <= i.Value;
        if (otherValue is CharLangValue c)
            return Value <= c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool GreaterEqual(LangValueType? otherValue)
    {
        if (otherValue is DoubleLangValue d)
            return Value >= d.Value;
        if (otherValue is IntLangValue i)
            return Value >= i.Value;
        if (otherValue is CharLangValue c)
            return Value >= c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is IntLangValue b)
            return Value == b.Value;
        if (otherValueType is DoubleLangValue d)
            return Value == d.Value;
        return false;
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => this,
            "Bool" or "bool" => BoolLangValue.Create(Value > 0),
            "String" or "string" => StringLangValue.Create(Value.ToString()),
            "char" or "Char" => CharLangValue.Create(Convert.ToChar(Value)),
            "Double" or "double" => DoubleLangValue.Create(Value),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }

    public override object GetValue() => Value;

    public override Type OutputType(LocalManager local) => Value.GetType();

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, Value);
    }
    
    /// <summary>
    /// 重置对象状态，使其可以被复用
    /// </summary>
    public void Reset()
    {
        Value = 0;
        // Position是只读属性，无法修改
    }
    
    /// <summary>
    /// 从对象池获取IntLangValue实例
    /// </summary>
    /// <param name="value">整数值</param>
    /// <param name="position">源码位置</param>
    /// <returns>IntLangValue实例</returns>
    public static IntLangValue Create(int value, SourcePosition position = default)
    {
        var instance = ObjectPoolManager.Instance.IntPool.Get();
        instance.Value = value;
        instance.Position = position;
        return instance;
    }
    
    /// <summary>
    /// 将实例归还到对象池
    /// </summary>
    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.IntPool.Return(this);
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