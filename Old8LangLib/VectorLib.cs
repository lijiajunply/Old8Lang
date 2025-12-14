namespace Old8LangLib;

/// <summary>
/// 向量库，提供各种维度的向量类和丰富的向量操作
/// </summary>
public static class VectorLib
{
    // 向量库的公共方法和常量可以在这里定义
}

/// <summary>
/// 2D 向量类，提供丰富的二维向量操作
/// </summary>
public class Vector2
{
    /// <summary>
    /// X 分量
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 分量
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 默认构造函数，创建零向量
    /// </summary>
    public Vector2() : this(0, 0)
    {
    }

    /// <summary>
    /// 构造函数，使用指定的 X 和 Y 分量创建向量
    /// </summary>
    /// <param name="x">X 分量</param>
    /// <param name="y">Y 分量</param>
    public Vector2(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// 获取向量的长度（模）
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    /// 获取向量的平方长度（模的平方），避免开平方运算，提高性能
    /// </summary>
    public double SqrMagnitude => X * X + Y * Y;

    /// <summary>
    /// 获取归一化（单位）向量
    /// </summary>
    public Vector2 Normalized
    {
        get
        {
            double magnitude = Magnitude;
            if (magnitude == 0)
            {
                throw new DivideByZeroException("零向量无法归一化");
            }

            return new Vector2(X / magnitude, Y / magnitude);
        }
    }

    /// <summary>
    /// 将向量归一化（原地修改）
    /// </summary>
    public void Normalize()
    {
        double magnitude = Magnitude;
        if (magnitude == 0)
        {
            throw new DivideByZeroException("零向量无法归一化");
        }

        X /= magnitude;
        Y /= magnitude;
    }

    /// <summary>
    /// 计算两个向量的点积
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>点积结果</returns>
    public double Dot(Vector2 other)
    {
        return X * other.X + Y * other.Y;
    }

    /// <summary>
    /// 计算两个向量的叉积（返回标量值，垂直于平面的分量）
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>叉积结果</returns>
    public double Cross(Vector2 other)
    {
        return X * other.Y - Y * other.X;
    }

    /// <summary>
    /// 计算两个向量之间的夹角（弧度）
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>夹角（弧度）</returns>
    public double Angle(Vector2 other)
    {
        double dot = Dot(other);
        double mag1 = Magnitude;
        double mag2 = other.Magnitude;
        double cosTheta = dot / (mag1 * mag2);

        // 确保 cosTheta 在 [-1, 1] 范围内，避免浮点误差
        cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));

        return Math.Acos(cosTheta);
    }

    /// <summary>
    /// 计算两个向量之间的距离
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>距离</returns>
    public double Distance(Vector2 other)
    {
        double dx = other.X - X;
        double dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// 计算向量与标量的乘法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public Vector2 Multiply(double scalar)
    {
        return new Vector2(X * scalar, Y * scalar);
    }

    /// <summary>
    /// 计算向量与标量的除法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public Vector2 Divide(double scalar)
    {
        if (scalar == 0)
        {
            throw new DivideByZeroException("无法除以零");
        }

        return new Vector2(X / scalar, Y / scalar);
    }

    /// <summary>
    /// 线性插值（Lerp）到另一个向量
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <param name="t">插值参数，范围 [0, 1]</param>
    /// <returns>插值结果向量</returns>
    public Vector2 Lerp(Vector2 other, double t)
    {
        // 确保 t 在 [0, 1] 范围内
        t = Math.Max(0.0, Math.Min(1.0, t));
        return new Vector2(
            X + (other.X - X) * t,
            Y + (other.Y - Y) * t
        );
    }

    /// <summary>
    /// 将向量投影到另一个向量上
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <returns>投影结果向量</returns>
    public Vector2 Project(Vector2 other)
    {
        double dot = Dot(other);
        double otherMagSq = other.SqrMagnitude;
        if (otherMagSq == 0)
        {
            return new Vector2(0, 0);
        }

        double scalar = dot / otherMagSq;
        return new Vector2(other.X * scalar, other.Y * scalar);
    }

    /// <summary>
    /// 反射向量
    /// </summary>
    /// <param name="normal">法线向量</param>
    /// <returns>反射后的向量</returns>
    public Vector2 Reflect(Vector2 normal)
    {
        double dot = Dot(normal);
        return new Vector2(
            X - 2 * dot * normal.X,
            Y - 2 * dot * normal.Y
        );
    }

    /// <summary>
    /// 向量加法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>和向量</returns>
    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>
    /// 向量减法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>差向量</returns>
    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>
    /// 向量乘法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>乘积向量</returns>
    public static Vector2 operator *(Vector2 vector, double scalar)
    {
        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量乘法运算符（与标量，反向）
    /// </summary>
    /// <param name="scalar">标量</param>
    /// <param name="vector">向量</param>
    /// <returns>乘积向量</returns>
    public static Vector2 operator *(double scalar, Vector2 vector)
    {
        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量除法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>除法结果向量</returns>
    public static Vector2 operator /(Vector2 vector, double scalar)
    {
        return vector.Divide(scalar);
    }

    /// <summary>
    /// 向量取反运算符
    /// </summary>
    /// <param name="vector">向量</param>
    /// <returns>取反后的向量</returns>
    public static Vector2 operator -(Vector2 vector)
    {
        return new Vector2(-vector.X, -vector.Y);
    }

    /// <summary>
    /// 向量相等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否相等</returns>
    public static bool operator ==(Vector2? a, Vector2? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return Math.Abs(a.X - b.X) < 1e-10 && Math.Abs(a.Y - b.Y) < 1e-10;
    }

    /// <summary>
    /// 向量不等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否不等</returns>
    public static bool operator !=(Vector2 a, Vector2 b)
    {
        return !(a == b);
    }

    /// <summary>
    /// 重写 Equals 方法
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is Vector2 other)
        {
            return this == other;
        }

        return false;
    }

    /// <summary>
    /// 重写 GetHashCode 方法
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X.GetHashCode(), Y.GetHashCode());
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    /// <summary>
    /// 创建零向量
    /// </summary>
    public static Vector2 Zero => new Vector2(0, 0);

    /// <summary>
    /// 创建单位 X 向量
    /// </summary>
    public static Vector2 UnitX => new Vector2(1, 0);

    /// <summary>
    /// 创建单位 Y 向量
    /// </summary>
    public static Vector2 UnitY => new Vector2(0, 1);

    /// <summary>
    /// 创建全 1 向量
    /// </summary>
    public static Vector2 One => new Vector2(1, 1);
}

/// <summary>
/// 3D 向量类，提供丰富的三维向量操作
/// </summary>
public class Vector3
{
    /// <summary>
    /// X 分量
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 分量
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z 分量
    /// </summary>
    public double Z { get; set; }

    /// <summary>
    /// 默认构造函数，创建零向量
    /// </summary>
    public Vector3() : this(0, 0, 0)
    {
    }

    /// <summary>
    /// 构造函数，使用指定的 X、Y 和 Z 分量创建向量
    /// </summary>
    /// <param name="x">X 分量</param>
    /// <param name="y">Y 分量</param>
    /// <param name="z">Z 分量</param>
    public Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// 获取向量的长度（模）
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// 获取向量的平方长度（模的平方），避免开平方运算，提高性能
    /// </summary>
    public double SqrMagnitude => X * X + Y * Y + Z * Z;

    /// <summary>
    /// 获取归一化（单位）向量
    /// </summary>
    public Vector3 Normalized
    {
        get
        {
            double magnitude = Magnitude;
            if (magnitude == 0)
            {
                throw new DivideByZeroException("零向量无法归一化");
            }

            return new Vector3(X / magnitude, Y / magnitude, Z / magnitude);
        }
    }

    /// <summary>
    /// 将向量归一化（原地修改）
    /// </summary>
    public void Normalize()
    {
        double magnitude = Magnitude;
        if (magnitude == 0)
        {
            throw new DivideByZeroException("零向量无法归一化");
        }

        X /= magnitude;
        Y /= magnitude;
        Z /= magnitude;
    }

    /// <summary>
    /// 计算两个向量的点积
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>点积结果</returns>
    public double Dot(Vector3 other)
    {
        return X * other.X + Y * other.Y + Z * other.Z;
    }

    /// <summary>
    /// 计算两个向量的叉积
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>叉积结果向量</returns>
    public Vector3 Cross(Vector3 other)
    {
        return new Vector3(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X
        );
    }

    /// <summary>
    /// 计算两个向量之间的夹角（弧度）
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>夹角（弧度）</returns>
    public double Angle(Vector3 other)
    {
        double dot = Dot(other);
        double mag1 = Magnitude;
        double mag2 = other.Magnitude;
        double cosTheta = dot / (mag1 * mag2);

        // 确保 cosTheta 在 [-1, 1] 范围内，避免浮点误差
        cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));

        return Math.Acos(cosTheta);
    }

    /// <summary>
    /// 计算两个向量之间的距离
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>距离</returns>
    public double Distance(Vector3 other)
    {
        double dx = other.X - X;
        double dy = other.Y - Y;
        double dz = other.Z - Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// 计算向量与标量的乘法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public Vector3 Multiply(double scalar)
    {
        return new Vector3(X * scalar, Y * scalar, Z * scalar);
    }

    /// <summary>
    /// 计算向量与标量的除法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public Vector3 Divide(double scalar)
    {
        if (scalar == 0)
        {
            throw new DivideByZeroException("无法除以零");
        }

        return new Vector3(X / scalar, Y / scalar, Z / scalar);
    }

    /// <summary>
    /// 线性插值（Lerp）到另一个向量
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <param name="t">插值参数，范围 [0, 1]</param>
    /// <returns>插值结果向量</returns>
    public Vector3 Lerp(Vector3 other, double t)
    {
        // 确保 t 在 [0, 1] 范围内
        t = Math.Max(0.0, Math.Min(1.0, t));
        return new Vector3(
            X + (other.X - X) * t,
            Y + (other.Y - Y) * t,
            Z + (other.Z - Z) * t
        );
    }

    /// <summary>
    /// 将向量投影到另一个向量上
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <returns>投影结果向量</returns>
    public Vector3 Project(Vector3 other)
    {
        double dot = Dot(other);
        double otherMagSq = other.SqrMagnitude;
        if (otherMagSq == 0)
        {
            return new Vector3(0, 0, 0);
        }

        double scalar = dot / otherMagSq;
        return new Vector3(other.X * scalar, other.Y * scalar, other.Z * scalar);
    }

    /// <summary>
    /// 反射向量
    /// </summary>
    /// <param name="normal">法线向量</param>
    /// <returns>反射后的向量</returns>
    public Vector3 Reflect(Vector3 normal)
    {
        double dot = Dot(normal);
        return new Vector3(
            X - 2 * dot * normal.X,
            Y - 2 * dot * normal.Y,
            Z - 2 * dot * normal.Z
        );
    }

    /// <summary>
    /// 向量加法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>和向量</returns>
    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    /// <summary>
    /// 向量减法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>差向量</returns>
    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    /// <summary>
    /// 向量乘法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>乘积向量</returns>
    public static Vector3 operator *(Vector3 vector, double scalar)
    {
        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量乘法运算符（与标量，反向）
    /// </summary>
    /// <param name="scalar">标量</param>
    /// <param name="vector">向量</param>
    /// <returns>乘积向量</returns>
    public static Vector3 operator *(double scalar, Vector3 vector)
    {
        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量除法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>除法结果向量</returns>
    public static Vector3 operator /(Vector3 vector, double scalar)
    {
        return vector.Divide(scalar);
    }

    /// <summary>
    /// 向量取反运算符
    /// </summary>
    /// <param name="vector">向量</param>
    /// <returns>取反后的向量</returns>
    public static Vector3 operator -(Vector3 vector)
    {
        return new Vector3(-vector.X, -vector.Y, -vector.Z);
    }

    /// <summary>
    /// 向量相等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否相等</returns>
    public static bool operator ==(Vector3? a, Vector3? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return Math.Abs(a.X - b.X) < 1e-10 && Math.Abs(a.Y - b.Y) < 1e-10 && Math.Abs(a.Z - b.Z) < 1e-10;
    }

    /// <summary>
    /// 向量不等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否不等</returns>
    public static bool operator !=(Vector3 a, Vector3 b)
    {
        return !(a == b);
    }

    /// <summary>
    /// 重写 Equals 方法
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is Vector3 other)
        {
            return this == other;
        }

        return false;
    }

    /// <summary>
    /// 重写 GetHashCode 方法
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X.GetHashCode(), Y.GetHashCode(), Z.GetHashCode());
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }

    /// <summary>
    /// 创建零向量
    /// </summary>
    public static Vector3 Zero => new Vector3(0, 0, 0);

    /// <summary>
    /// 创建单位 X 向量
    /// </summary>
    public static Vector3 UnitX => new Vector3(1, 0, 0);

    /// <summary>
    /// 创建单位 Y 向量
    /// </summary>
    public static Vector3 UnitY => new Vector3(0, 1, 0);

    /// <summary>
    /// 创建单位 Z 向量
    /// </summary>
    public static Vector3 UnitZ => new Vector3(0, 0, 1);

    /// <summary>
    /// 创建全 1 向量
    /// </summary>
    public static Vector3 One => new Vector3(1, 1, 1);
}

/// <summary>
/// 4D 向量类，提供丰富的四维向量操作
/// </summary>
public class Vector4
{
    /// <summary>
    /// X 分量
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 分量
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z 分量
    /// </summary>
    public double Z { get; set; }

    /// <summary>
    /// W 分量
    /// </summary>
    public double W { get; set; }

    /// <summary>
    /// 默认构造函数，创建零向量
    /// </summary>
    public Vector4() : this(0, 0, 0, 0)
    {
    }

    /// <summary>
    /// 构造函数，使用指定的 X、Y、Z 和 W 分量创建向量
    /// </summary>
    /// <param name="x">X 分量</param>
    /// <param name="y">Y 分量</param>
    /// <param name="z">Z 分量</param>
    /// <param name="w">W 分量</param>
    public Vector4(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// 获取向量的长度（模）
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

    /// <summary>
    /// 获取向量的平方长度（模的平方），避免开平方运算，提高性能
    /// </summary>
    public double SqrMagnitude => X * X + Y * Y + Z * Z + W * W;

    /// <summary>
    /// 获取归一化（单位）向量
    /// </summary>
    public Vector4 Normalized
    {
        get
        {
            double magnitude = Magnitude;
            if (magnitude == 0)
            {
                throw new DivideByZeroException("零向量无法归一化");
            }

            return new Vector4(X / magnitude, Y / magnitude, Z / magnitude, W / magnitude);
        }
    }

    /// <summary>
    /// 将向量归一化（原地修改）
    /// </summary>
    public void Normalize()
    {
        double magnitude = Magnitude;
        if (magnitude == 0)
        {
            throw new DivideByZeroException("零向量无法归一化");
        }

        X /= magnitude;
        Y /= magnitude;
        Z /= magnitude;
        W /= magnitude;
    }

    /// <summary>
    /// 计算两个向量的点积
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>点积结果</returns>
    public double Dot(Vector4 other)
    {
        return X * other.X + Y * other.Y + Z * other.Z + W * other.W;
    }

    /// <summary>
    /// 计算两个向量之间的夹角（弧度）
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>夹角（弧度）</returns>
    public double Angle(Vector4 other)
    {
        double dot = Dot(other);
        double mag1 = Magnitude;
        double mag2 = other.Magnitude;
        double cosTheta = dot / (mag1 * mag2);

        // 确保 cosTheta 在 [-1, 1] 范围内，避免浮点误差
        cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));

        return Math.Acos(cosTheta);
    }

    /// <summary>
    /// 计算两个向量之间的距离
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>距离</returns>
    public double Distance(Vector4 other)
    {
        double dx = other.X - X;
        double dy = other.Y - Y;
        double dz = other.Z - Z;
        double dw = other.W - W;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
    }

    /// <summary>
    /// 计算向量与标量的乘法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public Vector4 Multiply(double scalar)
    {
        return new Vector4(X * scalar, Y * scalar, Z * scalar, W * scalar);
    }

    /// <summary>
    /// 计算向量与标量的除法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public Vector4 Divide(double scalar)
    {
        if (scalar == 0)
        {
            throw new DivideByZeroException("无法除以零");
        }

        return new Vector4(X / scalar, Y / scalar, Z / scalar, W / scalar);
    }

    /// <summary>
    /// 线性插值（Lerp）到另一个向量
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <param name="t">插值参数，范围 [0, 1]</param>
    /// <returns>插值结果向量</returns>
    public Vector4 Lerp(Vector4 other, double t)
    {
        // 确保 t 在 [0, 1] 范围内
        t = Math.Max(0.0, Math.Min(1.0, t));
        return new Vector4(
            X + (other.X - X) * t,
            Y + (other.Y - Y) * t,
            Z + (other.Z - Z) * t,
            W + (other.W - W) * t
        );
    }

    /// <summary>
    /// 将向量投影到另一个向量上
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <returns>投影结果向量</returns>
    public Vector4 Project(Vector4 other)
    {
        double dot = Dot(other);
        double otherMagSq = other.SqrMagnitude;
        if (otherMagSq == 0)
        {
            return new Vector4(0, 0, 0, 0);
        }

        double scalar = dot / otherMagSq;
        return new Vector4(other.X * scalar, other.Y * scalar, other.Z * scalar, other.W * scalar);
    }

    /// <summary>
    /// 反射向量
    /// </summary>
    /// <param name="normal">法线向量</param>
    /// <returns>反射后的向量</returns>
    public Vector4 Reflect(Vector4 normal)
    {
        double dot = Dot(normal);
        return new Vector4(
            X - 2 * dot * normal.X,
            Y - 2 * dot * normal.Y,
            Z - 2 * dot * normal.Z,
            W - 2 * dot * normal.W
        );
    }

    /// <summary>
    /// 向量加法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>和向量</returns>
    public static Vector4 operator +(Vector4 a, Vector4 b)
    {
        return new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    }

    /// <summary>
    /// 向量减法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>差向量</returns>
    public static Vector4 operator -(Vector4 a, Vector4 b)
    {
        return new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    }

    /// <summary>
    /// 向量乘法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>乘积向量</returns>
    public static Vector4 operator *(Vector4 vector, double scalar)
    {
        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量乘法运算符（与标量，反向）
    /// </summary>
    /// <param name="scalar">标量</param>
    /// <param name="vector">向量</param>
    /// <returns>乘积向量</returns>
    public static Vector4 operator *(double scalar, Vector4 vector)
    {
        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量除法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>除法结果向量</returns>
    public static Vector4 operator /(Vector4 vector, double scalar)
    {
        return vector.Divide(scalar);
    }

    /// <summary>
    /// 向量取反运算符
    /// </summary>
    /// <param name="vector">向量</param>
    /// <returns>取反后的向量</returns>
    public static Vector4 operator -(Vector4 vector)
    {
        return new Vector4(-vector.X, -vector.Y, -vector.Z, -vector.W);
    }

    /// <summary>
    /// 向量相等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否相等</returns>
    public static bool operator ==(Vector4? a, Vector4? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return Math.Abs(a.X - b.X) < 1e-10 && Math.Abs(a.Y - b.Y) < 1e-10 && Math.Abs(a.Z - b.Z) < 1e-10 &&
               Math.Abs(a.W - b.W) < 1e-10;
    }

    /// <summary>
    /// 向量不等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否不等</returns>
    public static bool operator !=(Vector4 a, Vector4 b)
    {
        return !(a == b);
    }

    /// <summary>
    /// 重写 Equals 方法
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is Vector4 other)
        {
            return this == other;
        }

        return false;
    }

    /// <summary>
    /// 重写 GetHashCode 方法
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X.GetHashCode(), Y.GetHashCode(), Z.GetHashCode(), W.GetHashCode());
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"({X}, {Y}, {Z}, {W})";
    }

    /// <summary>
    /// 创建零向量
    /// </summary>
    public static Vector4 Zero => new Vector4(0, 0, 0, 0);

    /// <summary>
    /// 创建单位 X 向量
    /// </summary>
    public static Vector4 UnitX => new Vector4(1, 0, 0, 0);

    /// <summary>
    /// 创建单位 Y 向量
    /// </summary>
    public static Vector4 UnitY => new Vector4(0, 1, 0, 0);

    /// <summary>
    /// 创建单位 Z 向量
    /// </summary>
    public static Vector4 UnitZ => new Vector4(0, 0, 1, 0);

    /// <summary>
    /// 创建单位 W 向量
    /// </summary>
    public static Vector4 UnitW => new Vector4(0, 0, 0, 1);

    /// <summary>
    /// 创建全 1 向量
    /// </summary>
    public static Vector4 One => new Vector4(1, 1, 1, 1);
}

/// <summary>
/// 任意维度向量类，提供灵活的高维向量操作
/// </summary>
public class VectorN
{
    /// <summary>
    /// 向量的分量数组
    /// </summary>
    private readonly double[] Components;

    /// <summary>
    /// 获取向量的维度
    /// </summary>
    public int Dimension => Components.Length;

    /// <summary>
    /// 获取或设置向量的分量
    /// </summary>
    /// <param name="index">分量索引</param>
    /// <returns>分量值</returns>
    public double this[int index]
    {
        get
        {
            if (index < 0 || index >= Dimension)
            {
                throw new IndexOutOfRangeException("向量索引超出范围");
            }

            return Components[index];
        }
        set
        {
            if (index < 0 || index >= Dimension)
            {
                throw new IndexOutOfRangeException("向量索引超出范围");
            }

            Components[index] = value;
        }
    }

    /// <summary>
    /// 构造函数，创建指定维度的零向量
    /// </summary>
    /// <param name="dimension">向量维度</param>
    public VectorN(int dimension)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), "向量维度必须大于 0");
        }

        Components = new double[dimension];
    }

    /// <summary>
    /// 构造函数，使用指定的分量数组创建向量
    /// </summary>
    /// <param name="components">分量数组</param>
    public VectorN(params double[] components)
    {
        if (components == null || components.Length == 0)
        {
            throw new ArgumentException("分量数组不能为空且至少包含一个元素", nameof(components));
        }

        Components = (double[])components.Clone();
    }

    /// <summary>
    /// 获取向量的长度（模）
    /// </summary>
    public double Magnitude
    {
        get
        {
            double sum = 0;
            foreach (double component in Components)
            {
                sum += component * component;
            }

            return Math.Sqrt(sum);
        }
    }

    /// <summary>
    /// 获取向量的平方长度（模的平方），避免开平方运算，提高性能
    /// </summary>
    public double SqrMagnitude
    {
        get
        {
            double sum = 0;
            foreach (double component in Components)
            {
                sum += component * component;
            }

            return sum;
        }
    }

    /// <summary>
    /// 获取归一化（单位）向量
    /// </summary>
    public VectorN Normalized
    {
        get
        {
            double magnitude = Magnitude;
            if (magnitude == 0)
            {
                throw new DivideByZeroException("零向量无法归一化");
            }

            double[] normalized = new double[Dimension];
            for (int i = 0; i < Dimension; i++)
            {
                normalized[i] = Components[i] / magnitude;
            }

            return new VectorN(normalized);
        }
    }

    /// <summary>
    /// 将向量归一化（原地修改）
    /// </summary>
    public void Normalize()
    {
        double magnitude = Magnitude;
        if (magnitude == 0)
        {
            throw new DivideByZeroException("零向量无法归一化");
        }

        for (int i = 0; i < Dimension; i++)
        {
            Components[i] /= magnitude;
        }
    }

    /// <summary>
    /// 计算两个向量的点积
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>点积结果</returns>
    public double Dot(VectorN other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Dimension != other.Dimension)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        double result = 0;
        for (int i = 0; i < Dimension; i++)
        {
            result += Components[i] * other.Components[i];
        }

        return result;
    }

    /// <summary>
    /// 计算两个向量之间的夹角（弧度）
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>夹角（弧度）</returns>
    public double Angle(VectorN other)
    {
        double dot = Dot(other);
        double mag1 = Magnitude;
        double mag2 = other.Magnitude;
        double cosTheta = dot / (mag1 * mag2);

        // 确保 cosTheta 在 [-1, 1] 范围内，避免浮点误差
        cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));

        return Math.Acos(cosTheta);
    }

    /// <summary>
    /// 计算两个向量之间的距离
    /// </summary>
    /// <param name="other">另一个向量</param>
    /// <returns>距离</returns>
    public double Distance(VectorN other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Dimension != other.Dimension)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        double sum = 0;
        for (int i = 0; i < Dimension; i++)
        {
            double diff = Components[i] - other.Components[i];
            sum += diff * diff;
        }

        return Math.Sqrt(sum);
    }

    /// <summary>
    /// 计算向量与标量的乘法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public VectorN Multiply(double scalar)
    {
        double[] result = new double[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            result[i] = Components[i] * scalar;
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 计算向量与标量的除法
    /// </summary>
    /// <param name="scalar">标量值</param>
    /// <returns>新的向量</returns>
    public VectorN Divide(double scalar)
    {
        if (scalar == 0)
        {
            throw new DivideByZeroException("无法除以零");
        }

        double[] result = new double[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            result[i] = Components[i] / scalar;
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 线性插值（Lerp）到另一个向量
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <param name="t">插值参数，范围 [0, 1]</param>
    /// <returns>插值结果向量</returns>
    public VectorN Lerp(VectorN other, double t)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Dimension != other.Dimension)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        // 确保 t 在 [0, 1] 范围内
        t = Math.Max(0.0, Math.Min(1.0, t));

        double[] result = new double[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            result[i] = Components[i] + (other.Components[i] - Components[i]) * t;
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 将向量投影到另一个向量上
    /// </summary>
    /// <param name="other">目标向量</param>
    /// <returns>投影结果向量</returns>
    public VectorN Project(VectorN other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Dimension != other.Dimension)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        double dot = Dot(other);
        double otherMagSq = other.SqrMagnitude;
        if (otherMagSq == 0)
        {
            return new VectorN(Dimension);
        }

        double scalar = dot / otherMagSq;
        double[] result = new double[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            result[i] = other.Components[i] * scalar;
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 反射向量
    /// </summary>
    /// <param name="normal">法线向量</param>
    /// <returns>反射后的向量</returns>
    public VectorN Reflect(VectorN normal)
    {
        if (normal == null)
        {
            throw new ArgumentNullException(nameof(normal));
        }

        if (Dimension != normal.Dimension)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        double dot = Dot(normal);
        double[] result = new double[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            result[i] = Components[i] - 2 * dot * normal.Components[i];
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 获取向量的分量数组副本
    /// </summary>
    /// <returns>分量数组副本</returns>
    public double[] ToArray()
    {
        return (double[])Components.Clone();
    }

    /// <summary>
    /// 向量加法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>和向量</returns>
    public static VectorN operator +(VectorN a, VectorN b)
    {
        if (a == null || b == null)
        {
            throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));
        }

        if (a.Dimension != b.Dimension)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        double[] result = new double[a.Dimension];
        for (int i = 0; i < a.Dimension; i++)
        {
            result[i] = a.Components[i] + b.Components[i];
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 向量减法运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>差向量</returns>
    public static VectorN operator -(VectorN a, VectorN b)
    {
        if (a == null || b == null)
        {
            throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));
        }

        if (a.Dimension != b.Dimension)
        {
            throw new ArgumentException("两个向量必须具有相同的维度");
        }

        double[] result = new double[a.Dimension];
        for (int i = 0; i < a.Dimension; i++)
        {
            result[i] = a.Components[i] - b.Components[i];
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 向量乘法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>乘积向量</returns>
    public static VectorN operator *(VectorN vector, double scalar)
    {
        if (vector == null)
        {
            throw new ArgumentNullException(nameof(vector));
        }

        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量乘法运算符（与标量，反向）
    /// </summary>
    /// <param name="scalar">标量</param>
    /// <param name="vector">向量</param>
    /// <returns>乘积向量</returns>
    public static VectorN operator *(double scalar, VectorN vector)
    {
        if (vector == null)
        {
            throw new ArgumentNullException(nameof(vector));
        }

        return vector.Multiply(scalar);
    }

    /// <summary>
    /// 向量除法运算符（与标量）
    /// </summary>
    /// <param name="vector">向量</param>
    /// <param name="scalar">标量</param>
    /// <returns>除法结果向量</returns>
    public static VectorN operator /(VectorN vector, double scalar)
    {
        if (vector == null)
        {
            throw new ArgumentNullException(nameof(vector));
        }

        return vector.Divide(scalar);
    }

    /// <summary>
    /// 向量取反运算符
    /// </summary>
    /// <param name="vector">向量</param>
    /// <returns>取反后的向量</returns>
    public static VectorN operator -(VectorN vector)
    {
        if (vector == null)
        {
            throw new ArgumentNullException(nameof(vector));
        }

        double[] result = new double[vector.Dimension];
        for (int i = 0; i < vector.Dimension; i++)
        {
            result[i] = -vector.Components[i];
        }

        return new VectorN(result);
    }

    /// <summary>
    /// 向量相等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否相等</returns>
    public static bool operator ==(VectorN? a, VectorN? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Dimension != b.Dimension) return false;

        for (int i = 0; i < a.Dimension; i++)
        {
            if (Math.Abs(a.Components[i] - b.Components[i]) >= 1e-10)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 向量不等运算符
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>是否不等</returns>
    public static bool operator !=(VectorN a, VectorN b)
    {
        return !(a == b);
    }

    /// <summary>
    /// 重写 Equals 方法
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is VectorN other)
        {
            return this == other;
        }

        return false;
    }

    /// <summary>
    /// 重写 GetHashCode 方法
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        int hash = Dimension;
        foreach (double component in Components)
        {
            hash = HashCode.Combine(hash, component);
        }

        return hash;
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"({string.Join(", ", Components)})";
    }

    /// <summary>
    /// 创建指定维度的零向量
    /// </summary>
    /// <param name="dimension">向量维度</param>
    /// <returns>零向量</returns>
    public static VectorN Zero(int dimension)
    {
        return new VectorN(dimension);
    }

    /// <summary>
    /// 创建指定维度的单位向量（第 index 个分量为 1，其余为 0）
    /// </summary>
    /// <param name="dimension">向量维度</param>
    /// <param name="index">单位分量索引</param>
    /// <returns>单位向量</returns>
    public static VectorN Unit(int dimension, int index)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), "向量维度必须大于 0");
        }

        if (index < 0 || index >= dimension)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "单位分量索引超出范围");
        }

        double[] components = new double[dimension];
        components[index] = 1;
        return new VectorN(components);
    }
}