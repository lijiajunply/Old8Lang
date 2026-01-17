using System.Globalization;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// 基本类型的扩展方法类
/// 为虚拟机模式和编译器模式中的原生 C# 基本类型(int, double, bool, char)提供 Old8Lang 风格的方法
/// </summary>
public static class PrimitiveExtensions
{
    #region Int Extensions

    extension(int val)
    {
        public int ToInt() => val;
        public double ToDouble() => Convert.ToDouble(val);
        public bool ToBool() => val != 0;

        public char ToChar()
        {
            if (val is >= 0 and <= 65535) return (char)val;
            throw new FormatException($"Integer value {val} is out of valid character range");
        }

        public string ToStr() => val.ToString();
    }

    #endregion

    #region Double Extensions

    extension(double val)
    {
        public int ToInt() => Convert.ToInt32(val);
        public double ToDouble() => val;
        public bool ToBool() => val != 0.0;
        public string ToStr() => val.ToString(CultureInfo.InvariantCulture);
    }

    #endregion

    #region Bool Extensions

    extension(bool val)
    {
        public int ToInt() => val ? 1 : 0;
        public double ToDouble() => val ? 1.0 : 0.0;
        public bool ToBool() => val;
        public string ToStr() => val ? "true" : "false";
    }

    #endregion

    #region Char Extensions

    extension(char val)
    {
        public int ToInt() => Convert.ToInt32(val);
        public char ToChar() => val;
        public string ToStr() => val.ToString();
    }

    #endregion
}
