namespace Old8LangLib;

/// <summary>
/// 数学函数库
/// </summary>
public static class MathLib
{
    /// <summary>
    /// 平方根 - 只接受非负数
    /// </summary>
    public static double Sqrt(double value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "平方根函数的参数必须是非负数");
        }
        return Math.Sqrt(value);
    }

    /// <summary>
    /// 自然对数 - 只接受正数
    /// </summary>
    public static double Log(double value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "对数函数的参数必须是正数");
        }
        return Math.Log(value);
    }

    /// <summary>
    /// 以指定底数的对数 - 底数和值都必须是正数
    /// </summary>
    public static double LogBase(double value, double baseValue)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "对数函数的参数必须是正数");
        }
        if (baseValue <= 0 || baseValue == 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseValue),
                baseValue,
                "对数底数必须是大于0且不等于1的数");
        }
        return Math.Log(value, baseValue);
    }

    /// <summary>
    /// 阶乘 - 只接受非负整数
    /// </summary>
    public static long Factorial(int n)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(n),
                n,
                "阶乘函数的参数必须是非负整数");
        }
        if (n > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(n),
                n,
                "阶乘参数过大（> 20），结果会溢出");
        }

        long result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }

    /// <summary>
    /// 幂运算
    /// </summary>
    public static double Pow(double baseValue, double exponent)
    {
        return Math.Pow(baseValue, exponent);
    }

    /// <summary>
    /// 绝对值
    /// </summary>
    public static double Abs(double value)
    {
        return Math.Abs(value);
    }

    /// <summary>
    /// 向上取整
    /// </summary>
    public static double Ceil(double value)
    {
        return Math.Ceiling(value);
    }

    /// <summary>
    /// 向下取整
    /// </summary>
    public static double Floor(double value)
    {
        return Math.Floor(value);
    }

    /// <summary>
    /// 四舍五入
    /// </summary>
    public static double Round(double value)
    {
        return Math.Round(value);
    }

    /// <summary>
    /// 最大值
    /// </summary>
    public static double Max(double a, double b)
    {
        return Math.Max(a, b);
    }

    /// <summary>
    /// 最小值
    /// </summary>
    public static double Min(double a, double b)
    {
        return Math.Min(a, b);
    }

    // ===== 三角函数 =====

    /// <summary>
    /// 正弦函数（弧度）
    /// </summary>
    public static double Sin(double radians)
    {
        return Math.Sin(radians);
    }

    /// <summary>
    /// 余弦函数（弧度）
    /// </summary>
    public static double Cos(double radians)
    {
        return Math.Cos(radians);
    }

    /// <summary>
    /// 正切函数（弧度）
    /// </summary>
    public static double Tan(double radians)
    {
        return Math.Tan(radians);
    }

    /// <summary>
    /// 反正弦函数 - 返回弧度，参数范围 [-1, 1]
    /// </summary>
    public static double Asin(double value)
    {
        if (value < -1 || value > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "反正弦函数的参数必须在 [-1, 1] 范围内");
        }
        return Math.Asin(value);
    }

    /// <summary>
    /// 反余弦函数 - 返回弧度，参数范围 [-1, 1]
    /// </summary>
    public static double Acos(double value)
    {
        if (value < -1 || value > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "反余弦函数的参数必须在 [-1, 1] 范围内");
        }
        return Math.Acos(value);
    }

    /// <summary>
    /// 反正切函数 - 返回弧度
    /// </summary>
    public static double Atan(double value)
    {
        return Math.Atan(value);
    }

    /// <summary>
    /// 两参数反正切函数 - 返回弧度，用于计算点 (x, y) 的角度
    /// </summary>
    public static double Atan2(double y, double x)
    {
        return Math.Atan2(y, x);
    }

    // ===== 指数和对数函数 =====

    /// <summary>
    /// 自然指数函数 e^x
    /// </summary>
    public static double Exp(double value)
    {
        var result = Math.Exp(value);
        if (double.IsInfinity(result))
        {
            throw new OverflowException($"指数函数结果溢出: Exp({value})");
        }
        return result;
    }

    /// <summary>
    /// 以 10 为底的对数
    /// </summary>
    public static double Log10(double value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Log10 函数的参数必须是正数");
        }
        return Math.Log10(value);
    }

    // ===== 其他实用函数 =====

    /// <summary>
    /// 符号函数 - 返回 -1, 0, 或 1
    /// </summary>
    public static int Sign(double value)
    {
        return Math.Sign(value);
    }

    /// <summary>
    /// 截断小数部分，返回整数部分
    /// </summary>
    public static double Trunc(double value)
    {
        return Math.Truncate(value);
    }

    // ===== 数学常数 =====

    /// <summary>
    /// 获取圆周率 π ≈ 3.14159265358979323846
    /// </summary>
    public static double GetPi()
    {
        return Math.PI;
    }

    /// <summary>
    /// 获取自然对数的底 e ≈ 2.71828182845904523536
    /// </summary>
    public static double GetE()
    {
        return Math.E;
    }
}