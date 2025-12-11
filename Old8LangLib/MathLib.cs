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

        if (baseValue <= 0 || Math.Abs(baseValue - 1) < 0.001)
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

    // ===== 双曲三角函数 =====

    /// <summary>
    /// 双曲正弦函数
    /// </summary>
    public static double Sinh(double value)
    {
        return Math.Sinh(value);
    }

    /// <summary>
    /// 双曲余弦函数
    /// </summary>
    public static double Cosh(double value)
    {
        return Math.Cosh(value);
    }

    /// <summary>
    /// 双曲正切函数
    /// </summary>
    public static double Tanh(double value)
    {
        return Math.Tanh(value);
    }

    // ===== 特殊函数 =====

    /// <summary>
    /// Gamma函数 - 阶乘函数在实数域上的扩展
    /// </summary>
    public static double Gamma(double value)
    {
        if (value <= 0 && Math.Abs(Math.Floor(value) - value) < 0.001)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Gamma函数在负整数处无定义");
        }

        // 对于正整数，Gamma(n) = (n-1)!
        if (Math.Abs(value - Math.Floor(value)) < 0.001 && value is > 0 and <= 20)
        {
            return Factorial((int)value - 1);
        }

        // 使用Stirling近似公式计算Gamma函数
        // Gamma(z) ≈ sqrt(2π/z) * (z/e)^z
        if (value > 10)
        {
            return Math.Sqrt(2 * Math.PI / value) * Math.Pow(value / Math.E, value);
        }

        // 使用递推关系 Gamma(z+1) = z * Gamma(z)
        // 将参数调整到可以使用近似公式的范围
        if (value < 1)
        {
            return Gamma(value + 1) / value;
        }
        else
        {
            double result = 1.0;
            double z = value;
            while (z > 10)
            {
                z--;
                result *= z;
            }

            // 使用Stirling近似计算Gamma(z)
            double stirling = Math.Sqrt(2 * Math.PI / z) * Math.Pow(z / Math.E, z);
            return stirling * result;
        }
    }

    /// <summary>
    /// Beta函数 - 用于计算Beta分布和不完全Beta函数
    /// </summary>
    public static double Beta(double x, double y)
    {
        if (x <= 0 || y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Beta函数的参数必须大于0");
        }

        // 使用Gamma函数计算Beta函数: Beta(x, y) = Gamma(x) * Gamma(y) / Gamma(x + y)
        return Gamma(x) * Gamma(y) / Gamma(x + y);
    }

    // ===== 随机数函数 =====

    private static readonly Random RandomNum = new();

    /// <summary>
    /// 生成一个介于 0.0（包含）和 1.0（不包含）之间的随机浮点数
    /// </summary>
    public static double Random()
    {
        return RandomNum.NextDouble();
    }

    /// <summary>
    /// 生成一个介于 minValue（包含）和 maxValue（不包含）之间的随机整数
    /// </summary>
    public static int RandomInt(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue 必须大于 minValue");
        }

        return RandomNum.Next(minValue, maxValue);
    }

    // ===== 角度和弧度转换 =====

    /// <summary>
    /// 将角度转换为弧度
    /// </summary>
    public static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// 将弧度转换为角度
    /// </summary>
    public static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    // ===== 常用单位换算 =====

    /// <summary>
    /// 摄氏度转华氏度
    /// </summary>
    public static double CelsiusToFahrenheit(double celsius)
    {
        return celsius * 9.0 / 5.0 + 32.0;
    }

    /// <summary>
    /// 华氏度转摄氏度
    /// </summary>
    public static double FahrenheitToCelsius(double fahrenheit)
    {
        return (fahrenheit - 32.0) * 5.0 / 9.0;
    }

    /// <summary>
    /// 米转英尺
    /// </summary>
    public static double MetersToFeet(double meters)
    {
        return meters * 3.28084;
    }

    /// <summary>
    /// 英尺转米
    /// </summary>
    public static double FeetToMeters(double feet)
    {
        return feet / 3.28084;
    }

    /// <summary>
    /// 千克转磅
    /// </summary>
    public static double KilogramsToPounds(double kilograms)
    {
        return kilograms * 2.20462;
    }

    /// <summary>
    /// 磅转千克
    /// </summary>
    public static double PoundsToKilograms(double pounds)
    {
        return pounds / 2.20462;
    }

    // ===== 基础向量操作 =====

    /// <summary>
    /// 计算向量的长度（模）
    /// </summary>
    public static double VectorMagnitude(params double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double sum = 0;
        foreach (double component in vector)
        {
            sum += component * component;
        }

        return Math.Sqrt(sum);
    }

    /// <summary>
    /// 计算两个向量的点积
    /// </summary>
    public static double VectorDotProduct(double[] vector1, double[] vector2)
    {
        if (vector1 == null || vector2 == null)
        {
            throw new ArgumentNullException(nameof(vector1), "向量不能为空");
        }

        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        if (vector1.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素");
        }

        double result = 0;
        for (int i = 0; i < vector1.Length; i++)
        {
            result += vector1[i] * vector2[i];
        }

        return result;
    }

    /// <summary>
    /// 计算向量的和
    /// </summary>
    public static double[] VectorAdd(params double[][] vectors)
    {
        if (vectors == null || vectors.Length == 0)
        {
            throw new ArgumentException("至少需要一个向量", nameof(vectors));
        }

        if (vectors[0] == null || vectors[0].Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素");
        }

        int dimension = vectors[0].Length;
        double[] result = new double[dimension];

        foreach (double[] vector in vectors)
        {
            if (vector == null || vector.Length != dimension)
            {
                throw new ArgumentException("所有向量必须具有相同的维度");
            }

            for (int i = 0; i < dimension; i++)
            {
                result[i] += vector[i];
            }
        }

        return result;
    }

    /// <summary>
    /// 计算向量的差（vector1 - vector2）
    /// </summary>
    public static double[] VectorSubtract(double[] vector1, double[] vector2)
    {
        if (vector1 == null || vector2 == null)
        {
            throw new ArgumentNullException(nameof(vector1), "向量不能为空");
        }

        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        if (vector1.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素");
        }

        double[] result = new double[vector1.Length];
        for (int i = 0; i < vector1.Length; i++)
        {
            result[i] = vector1[i] - vector2[i];
        }

        return result;
    }

    /// <summary>
    /// 向量与标量相乘
    /// </summary>
    public static double[] VectorMultiply(double[] vector, double scalar)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = vector[i] * scalar;
        }

        return result;
    }

    /// <summary>
    /// 单位化向量（归一化）
    /// </summary>
    public static double[] VectorNormalize(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double magnitude = VectorMagnitude(vector);
        if (magnitude == 0)
        {
            throw new DivideByZeroException("零向量无法单位化");
        }

        return VectorMultiply(vector, 1.0 / magnitude);
    }

    /// <summary>
    /// 计算两个向量之间的夹角（弧度）
    /// </summary>
    public static double VectorAngle(double[] vector1, double[] vector2)
    {
        double dotProduct = VectorDotProduct(vector1, vector2);
        double magnitude1 = VectorMagnitude(vector1);
        double magnitude2 = VectorMagnitude(vector2);

        double cosTheta = dotProduct / (magnitude1 * magnitude2);

        // 确保cosTheta在[-1, 1]范围内，避免浮点误差
        cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));

        return Math.Acos(cosTheta);
    }

    // ===== 向量数学函数 =====

    /// <summary>
    /// 对向量中的每个元素应用正弦函数
    /// </summary>
    public static double[] VectorSin(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = Math.Sin(vector[i]);
        }

        return result;
    }

    /// <summary>
    /// 对向量中的每个元素应用余弦函数
    /// </summary>
    public static double[] VectorCos(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = Math.Cos(vector[i]);
        }

        return result;
    }

    /// <summary>
    /// 对向量中的每个元素应用正切函数
    /// </summary>
    public static double[] VectorTan(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = Math.Tan(vector[i]);
        }

        return result;
    }

    /// <summary>
    /// 对向量中的每个元素应用指数函数
    /// </summary>
    public static double[] VectorExp(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = Math.Exp(vector[i]);
        }

        return result;
    }

    /// <summary>
    /// 对向量中的每个元素应用自然对数函数
    /// </summary>
    public static double[] VectorLog(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = Math.Log(vector[i]);
        }

        return result;
    }

    /// <summary>
    /// 对向量中的每个元素应用绝对值函数
    /// </summary>
    public static double[] VectorAbs(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = Math.Abs(vector[i]);
        }

        return result;
    }

    /// <summary>
    /// 对向量中的每个元素应用平方根函数
    /// </summary>
    public static double[] VectorSqrt(double[] vector)
    {
        if (vector == null || vector.Length == 0)
        {
            throw new ArgumentException("向量不能为空且至少包含一个元素", nameof(vector));
        }

        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = Math.Sqrt(vector[i]);
        }

        return result;
    }
}