using Avalonia.Media;

namespace FirstUI.Animation;

/// <summary>
/// Transition 过渡动画类
/// 提供常见的过渡动画效果
/// </summary>
public static class Transition
{
    // ============ 数值插值器 ============

    /// <summary>
    /// Double 类型插值
    /// </summary>
    public static double InterpolateDouble(double from, double to, double progress)
    {
        return from + (to - from) * progress;
    }

    /// <summary>
    /// Int 类型插值
    /// </summary>
    public static int InterpolateInt(int from, int to, double progress)
    {
        return (int)(from + (to - from) * progress);
    }

    // ============ 颜色插值器 ============

    /// <summary>
    /// 颜色插值（字符串格式）
    /// </summary>
    public static string InterpolateColor(string from, string to, double progress)
    {
        var fromColor = ParseColor(from);
        var toColor = ParseColor(to);

        var a = (byte)(fromColor.A + (toColor.A - fromColor.A) * progress);
        var r = (byte)(fromColor.R + (toColor.R - fromColor.R) * progress);
        var g = (byte)(fromColor.G + (toColor.G - fromColor.G) * progress);
        var b = (byte)(fromColor.B + (toColor.B - fromColor.B) * progress);

        return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// 颜色插值（Color 类型）
    /// </summary>
    public static Color InterpolateColor(Color from, Color to, double progress)
    {
        var a = (byte)(from.A + (to.A - from.A) * progress);
        var r = (byte)(from.R + (to.R - from.R) * progress);
        var g = (byte)(from.G + (to.G - from.G) * progress);
        var b = (byte)(from.B + (to.B - from.B) * progress);

        return Color.FromArgb(a, r, g, b);
    }

    // ============ 预设动画 ============

    /// <summary>
    /// 淡入淡出动画
    /// </summary>
    /// <param name="from">起始透明度 (0.0 - 1.0)</param>
    /// <param name="to">结束透明度 (0.0 - 1.0)</param>
    /// <param name="duration">时长（毫秒）</param>
    public static Animation<double> Fade(double from, double to, int duration = 300)
    {
        return new Animation<double>(from, to, InterpolateDouble)
        {
            Duration = duration,
            EasingFunc = Easing.EaseInOutQuad
        };
    }

    /// <summary>
    /// 淡入动画
    /// </summary>
    public static Animation<double> FadeIn(int duration = 300)
    {
        return Fade(0, 1, duration);
    }

    /// <summary>
    /// 淡出动画
    /// </summary>
    public static Animation<double> FadeOut(int duration = 300)
    {
        return Fade(1, 0, duration);
    }

    /// <summary>
    /// 缩放动画
    /// </summary>
    /// <param name="from">起始缩放比例</param>
    /// <param name="to">结束缩放比例</param>
    /// <param name="duration">时长（毫秒）</param>
    public static Animation<double> Scale(double from, double to, int duration = 300)
    {
        return new Animation<double>(from, to, InterpolateDouble)
        {
            Duration = duration,
            EasingFunc = Easing.EaseOutBack
        };
    }

    /// <summary>
    /// 放大动画
    /// </summary>
    public static Animation<double> ScaleIn(int duration = 300)
    {
        return Scale(0, 1, duration);
    }

    /// <summary>
    /// 缩小动画
    /// </summary>
    public static Animation<double> ScaleOut(int duration = 300)
    {
        return Scale(1, 0, duration);
    }

    /// <summary>
    /// 滑动动画
    /// </summary>
    /// <param name="from">起始位置</param>
    /// <param name="to">结束位置</param>
    /// <param name="duration">时长（毫秒）</param>
    public static Animation<double> Slide(double from, double to, int duration = 300)
    {
        return new Animation<double>(from, to, InterpolateDouble)
        {
            Duration = duration,
            EasingFunc = Easing.EaseInOutCubic
        };
    }

    /// <summary>
    /// 从左滑入动画
    /// </summary>
    /// <param name="distance">滑动距离</param>
    public static Animation<double> SlideInLeft(double distance, int duration = 300)
    {
        return Slide(-distance, 0, duration);
    }

    /// <summary>
    /// 从右滑入动画
    /// </summary>
    public static Animation<double> SlideInRight(double distance, int duration = 300)
    {
        return Slide(distance, 0, duration);
    }

    /// <summary>
    /// 从上滑入动画
    /// </summary>
    public static Animation<double> SlideInTop(double distance, int duration = 300)
    {
        return Slide(-distance, 0, duration);
    }

    /// <summary>
    /// 从下滑入动画
    /// </summary>
    public static Animation<double> SlideInBottom(double distance, int duration = 300)
    {
        return Slide(distance, 0, duration);
    }

    /// <summary>
    /// 向左滑出动画
    /// </summary>
    public static Animation<double> SlideOutLeft(double distance, int duration = 300)
    {
        return Slide(0, -distance, duration);
    }

    /// <summary>
    /// 向右滑出动画
    /// </summary>
    public static Animation<double> SlideOutRight(double distance, int duration = 300)
    {
        return Slide(0, distance, duration);
    }

    /// <summary>
    /// 旋转动画
    /// </summary>
    /// <param name="from">起始角度（度）</param>
    /// <param name="to">结束角度（度）</param>
    /// <param name="duration">时长（毫秒）</param>
    public static Animation<double> Rotate(double from, double to, int duration = 300)
    {
        return new Animation<double>(from, to, InterpolateDouble)
        {
            Duration = duration,
            EasingFunc = Easing.EaseInOutQuad
        };
    }

    /// <summary>
    /// 旋转进入动画
    /// </summary>
    public static Animation<double> RotateIn(int duration = 500)
    {
        return Rotate(-180, 0, duration)
            .SetEasing(Easing.EaseOutBack);
    }

    /// <summary>
    /// 旋转退出动画
    /// </summary>
    public static Animation<double> RotateOut(int duration = 500)
    {
        return Rotate(0, 180, duration)
            .SetEasing(Easing.EaseInBack);
    }

    /// <summary>
    /// 颜色过渡动画
    /// </summary>
    public static Animation<string> ColorTransition(string from, string to, int duration = 300)
    {
        return new Animation<string>(from, to, (f, t, p) => InterpolateColor(f, t, p))
        {
            Duration = duration,
            EasingFunc = Easing.Linear
        };
    }

    /// <summary>
    /// 弹跳动画
    /// </summary>
    public static Animation<double> Bounce(double from, double to, int duration = 600)
    {
        return new Animation<double>(from, to, InterpolateDouble)
        {
            Duration = duration,
            EasingFunc = Easing.EaseOutBounce
        };
    }

    /// <summary>
    /// 弹性动画
    /// </summary>
    public static Animation<double> Elastic(double from, double to, int duration = 600)
    {
        return new Animation<double>(from, to, InterpolateDouble)
        {
            Duration = duration,
            EasingFunc = Easing.EaseOutElastic
        };
    }

    /// <summary>
    /// 脉冲动画（放大后缩小）
    /// </summary>
    public static Animation<double> Pulse(int duration = 600)
    {
        return Scale(1, 1.1, duration)
            .SetAutoReverse(true)
            .SetLoop(true, 1);
    }

    /// <summary>
    /// 摇晃动画
    /// </summary>
    public static Animation<double> Shake(double intensity = 10, int duration = 500)
    {
        var animation = new Animation<double>(0, intensity, InterpolateDouble)
        {
            Duration = duration / 4,
            EasingFunc = Easing.Linear,
            Loop = true,
            LoopCount = 4,
            AutoReverse = true
        };
        return animation;
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 解析颜色字符串
    /// </summary>
    private static Color ParseColor(string colorString)
    {
        colorString = colorString.TrimStart('#');

        if (colorString.Length == 6)
        {
            // RGB
            var r = Convert.ToByte(colorString.Substring(0, 2), 16);
            var g = Convert.ToByte(colorString.Substring(2, 2), 16);
            var b = Convert.ToByte(colorString.Substring(4, 2), 16);
            return Color.FromRgb(r, g, b);
        }
        else if (colorString.Length == 8)
        {
            // ARGB
            var a = Convert.ToByte(colorString.Substring(0, 2), 16);
            var r = Convert.ToByte(colorString.Substring(2, 2), 16);
            var g = Convert.ToByte(colorString.Substring(4, 2), 16);
            var b = Convert.ToByte(colorString.Substring(6, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }

        return Colors.Black;
    }
}
