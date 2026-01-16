using System.Globalization;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// 基本类型的扩展方法类
/// 为虚拟机模式和编译器模式中的原生 C# 基本类型(int, double, bool, char)提供 Old8Lang 风格的方法
/// </summary>
public static class PrimitiveExtensions
{
    #region Int Extensions

    public static int ToInt(this int val) => val;
    public static double ToDouble(this int val) => Convert.ToDouble(val);
    public static bool ToBool(this int val) => val != 0;
    public static char ToChar(this int val)
    {
        if (val >= 0 && val <= 65535) return (char)val;
        throw new FormatException($"Integer value {val} is out of valid character range");
    }
    public static string ToStr(this int val) => val.ToString();

    #endregion

    #region Double Extensions

    public static int ToInt(this double val) => Convert.ToInt32(val);
    public static double ToDouble(this double val) => val;
    public static bool ToBool(this double val) => val != 0.0;
    public static string ToStr(this double val) => val.ToString(CultureInfo.InvariantCulture);

    #endregion

    #region Bool Extensions

    public static int ToInt(this bool val) => val ? 1 : 0;
    public static double ToDouble(this bool val) => val ? 1.0 : 0.0;
    public static bool ToBool(this bool val) => val;
    public static string ToStr(this bool val) => val ? "true" : "false";

    #endregion

    #region Char Extensions

    public static int ToInt(this char val) => Convert.ToInt32(val);
    public static char ToChar(this char val) => val;
    public static string ToStr(this char val) => val.ToString();

    #endregion
}
