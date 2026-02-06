namespace FirstUI.Animation;

/// <summary>
/// 缓动函数委托
/// </summary>
/// <param name="t">时间进度 (0.0 - 1.0)</param>
/// <returns>缓动后的值 (通常为 0.0 - 1.0)</returns>
public delegate double EasingFunction(double t);

/// <summary>
/// 缓动函数库
/// 提供各种常用的缓动函数（Easing Functions）
/// </summary>
public static class Easing
{
    // ============ 线性 ============

    /// <summary>
    /// 线性（无缓动）
    /// </summary>
    public static double Linear(double t) => t;

    // ============ 四次方 (Quad) ============

    /// <summary>
    /// 四次方缓入
    /// </summary>
    public static double EaseInQuad(double t) => t * t;

    /// <summary>
    /// 四次方缓出
    /// </summary>
    public static double EaseOutQuad(double t) => t * (2 - t);

    /// <summary>
    /// 四次方缓入缓出
    /// </summary>
    public static double EaseInOutQuad(double t)
    {
        return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    }

    // ============ 三次方 (Cubic) ============

    /// <summary>
    /// 三次方缓入
    /// </summary>
    public static double EaseInCubic(double t) => t * t * t;

    /// <summary>
    /// 三次方缓出
    /// </summary>
    public static double EaseOutCubic(double t)
    {
        var t1 = t - 1;
        return t1 * t1 * t1 + 1;
    }

    /// <summary>
    /// 三次方缓入缓出
    /// </summary>
    public static double EaseInOutCubic(double t)
    {
        return t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
    }

    // ============ 四次幂 (Quart) ============

    /// <summary>
    /// 四次幂缓入
    /// </summary>
    public static double EaseInQuart(double t) => t * t * t * t;

    /// <summary>
    /// 四次幂缓出
    /// </summary>
    public static double EaseOutQuart(double t)
    {
        var t1 = t - 1;
        return 1 - t1 * t1 * t1 * t1;
    }

    /// <summary>
    /// 四次幂缓入缓出
    /// </summary>
    public static double EaseInOutQuart(double t)
    {
        if (t < 0.5)
            return 8 * t * t * t * t;
        var t1 = t - 1;
        return 1 - 8 * t1 * t1 * t1 * t1;
    }

    // ============ 五次幂 (Quint) ============

    /// <summary>
    /// 五次幂缓入
    /// </summary>
    public static double EaseInQuint(double t) => t * t * t * t * t;

    /// <summary>
    /// 五次幂缓出
    /// </summary>
    public static double EaseOutQuint(double t)
    {
        var t1 = t - 1;
        return 1 + t1 * t1 * t1 * t1 * t1;
    }

    /// <summary>
    /// 五次幂缓入缓出
    /// </summary>
    public static double EaseInOutQuint(double t)
    {
        if (t < 0.5)
            return 16 * t * t * t * t * t;
        var t1 = t - 1;
        return 1 + 16 * t1 * t1 * t1 * t1 * t1;
    }

    // ============ 正弦 (Sine) ============

    /// <summary>
    /// 正弦缓入
    /// </summary>
    public static double EaseInSine(double t)
    {
        return 1 - Math.Cos(t * Math.PI / 2);
    }

    /// <summary>
    /// 正弦缓出
    /// </summary>
    public static double EaseOutSine(double t)
    {
        return Math.Sin(t * Math.PI / 2);
    }

    /// <summary>
    /// 正弦缓入缓出
    /// </summary>
    public static double EaseInOutSine(double t)
    {
        return -(Math.Cos(Math.PI * t) - 1) / 2;
    }

    // ============ 指数 (Expo) ============

    /// <summary>
    /// 指数缓入
    /// </summary>
    public static double EaseInExpo(double t)
    {
        return t == 0 ? 0 : Math.Pow(2, 10 * (t - 1));
    }

    /// <summary>
    /// 指数缓出
    /// </summary>
    public static double EaseOutExpo(double t)
    {
        return t == 1 ? 1 : 1 - Math.Pow(2, -10 * t);
    }

    /// <summary>
    /// 指数缓入缓出
    /// </summary>
    public static double EaseInOutExpo(double t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        if (t < 0.5)
            return Math.Pow(2, 20 * t - 10) / 2;
        return (2 - Math.Pow(2, -20 * t + 10)) / 2;
    }

    // ============ 圆形 (Circ) ============

    /// <summary>
    /// 圆形缓入
    /// </summary>
    public static double EaseInCirc(double t)
    {
        return 1 - Math.Sqrt(1 - t * t);
    }

    /// <summary>
    /// 圆形缓出
    /// </summary>
    public static double EaseOutCirc(double t)
    {
        var t1 = t - 1;
        return Math.Sqrt(1 - t1 * t1);
    }

    /// <summary>
    /// 圆形缓入缓出
    /// </summary>
    public static double EaseInOutCirc(double t)
    {
        if (t < 0.5)
            return (1 - Math.Sqrt(1 - 4 * t * t)) / 2;
        var t1 = -2 * t + 2;
        return (Math.Sqrt(1 - t1 * t1) + 1) / 2;
    }

    // ============ 回弹 (Back) ============

    private const double BackConstant = 1.70158;

    /// <summary>
    /// 回弹缓入
    /// </summary>
    public static double EaseInBack(double t)
    {
        var c = BackConstant + 1;
        return c * t * t * t - BackConstant * t * t;
    }

    /// <summary>
    /// 回弹缓出
    /// </summary>
    public static double EaseOutBack(double t)
    {
        var c = BackConstant + 1;
        var t1 = t - 1;
        return 1 + c * t1 * t1 * t1 + BackConstant * t1 * t1;
    }

    /// <summary>
    /// 回弹缓入缓出
    /// </summary>
    public static double EaseInOutBack(double t)
    {
        var c1 = BackConstant;
        var c2 = c1 * 1.525;

        if (t < 0.5)
        {
            var t2 = 2 * t;
            return (t2 * t2 * ((c2 + 1) * 2 * t - c2)) / 2;
        }
        else
        {
            var t2 = 2 * t - 2;
            return (t2 * t2 * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }
    }

    // ============ 弹性 (Elastic) ============

    /// <summary>
    /// 弹性缓入
    /// </summary>
    public static double EaseInElastic(double t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;

        var c = (2 * Math.PI) / 3;
        return -Math.Pow(2, 10 * t - 10) * Math.Sin((t * 10 - 10.75) * c);
    }

    /// <summary>
    /// 弹性缓出
    /// </summary>
    public static double EaseOutElastic(double t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;

        var c = (2 * Math.PI) / 3;
        return Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1;
    }

    /// <summary>
    /// 弹性缓入缓出
    /// </summary>
    public static double EaseInOutElastic(double t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;

        var c = (2 * Math.PI) / 4.5;

        if (t < 0.5)
            return -(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125) * c)) / 2;

        return (Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125) * c)) / 2 + 1;
    }

    // ============ 弹跳 (Bounce) ============

    /// <summary>
    /// 弹跳缓出
    /// </summary>
    public static double EaseOutBounce(double t)
    {
        const double n1 = 7.5625;
        const double d1 = 2.75;

        if (t < 1 / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2 / d1)
        {
            var t2 = t - 1.5 / d1;
            return n1 * t2 * t2 + 0.75;
        }
        else if (t < 2.5 / d1)
        {
            var t2 = t - 2.25 / d1;
            return n1 * t2 * t2 + 0.9375;
        }
        else
        {
            var t2 = t - 2.625 / d1;
            return n1 * t2 * t2 + 0.984375;
        }
    }

    /// <summary>
    /// 弹跳缓入
    /// </summary>
    public static double EaseInBounce(double t)
    {
        return 1 - EaseOutBounce(1 - t);
    }

    /// <summary>
    /// 弹跳缓入缓出
    /// </summary>
    public static double EaseInOutBounce(double t)
    {
        return t < 0.5
            ? (1 - EaseOutBounce(1 - 2 * t)) / 2
            : (1 + EaseOutBounce(2 * t - 1)) / 2;
    }

    // ============ Spring (弹簧效果) ============

    /// <summary>
    /// 弹簧效果
    /// </summary>
    /// <param name="t">进度</param>
    /// <param name="tension">张力（默认 0.3）</param>
    /// <param name="friction">摩擦力（默认 0.6）</param>
    public static double Spring(double t, double tension = 0.3, double friction = 0.6)
    {
        var omega = 2 * Math.PI / tension;
        var zeta = friction / (2 * Math.Sqrt(tension));
        var envelope = Math.Exp(-zeta * omega * t);
        var oscillation = Math.Cos(omega * t * Math.Sqrt(1 - zeta * zeta));
        return 1 - envelope * oscillation;
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 根据名称获取缓动函数
    /// </summary>
    public static EasingFunction GetEasingFunction(string name)
    {
        return name.ToLower() switch
        {
            "linear" => Linear,
            "easeinquad" => EaseInQuad,
            "easeoutquad" => EaseOutQuad,
            "easeinoutquad" => EaseInOutQuad,
            "easeincubic" => EaseInCubic,
            "easeoutcubic" => EaseOutCubic,
            "easeinoutcubic" => EaseInOutCubic,
            "easeinquart" => EaseInQuart,
            "easeoutquart" => EaseOutQuart,
            "easeinoutquart" => EaseInOutQuart,
            "easeinquint" => EaseInQuint,
            "easeoutquint" => EaseOutQuint,
            "easeinoutquint" => EaseInOutQuint,
            "easeinsine" => EaseInSine,
            "easeoutsine" => EaseOutSine,
            "easeinoutsine" => EaseInOutSine,
            "easeinexpo" => EaseInExpo,
            "easeoutexpo" => EaseOutExpo,
            "easeinoutexpo" => EaseInOutExpo,
            "easeincirc" => EaseInCirc,
            "easeoutcirc" => EaseOutCirc,
            "easeinoutcirc" => EaseInOutCirc,
            "easeinback" => EaseInBack,
            "easeoutback" => EaseOutBack,
            "easeinoutback" => EaseInOutBack,
            "easeinelastic" => EaseInElastic,
            "easeoutelastic" => EaseOutElastic,
            "easeinoutelastic" => EaseInOutElastic,
            "easeinbounce" => EaseInBounce,
            "easeoutbounce" => EaseOutBounce,
            "easeinoutbounce" => EaseInOutBounce,
            _ => Linear
        };
    }
}
