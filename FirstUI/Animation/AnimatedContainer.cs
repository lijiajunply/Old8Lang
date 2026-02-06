using FirstUI.Core;
using FirstUI.Layout;

namespace FirstUI.Animation;

/// <summary>
/// AnimatedContainer 动画容器组件
/// 属性变化时自动添加过渡动画
/// </summary>
public class AnimatedContainer : WidgetBase
{
    private Container? _currentContainer;
    private double _currentWidth;
    private double _currentHeight;
    private double _currentOpacity;
    private string? _currentBackgroundColor;
    private readonly Thickness _currentPadding;
    private readonly Thickness _currentMargin;

    // 动画实例
    private Animation<double>? _widthAnimation;
    private Animation<double>? _heightAnimation;
    private Animation<double>? _opacityAnimation;
    private Animation<string>? _colorAnimation;

    /// <summary>
    /// 子组件
    /// </summary>
    public WidgetBase? Child { get; set; }

    /// <summary>
    /// 动画时长（毫秒）
    /// </summary>
    public int AnimationDuration { get; set; } = 300;

    /// <summary>
    /// 缓动函数
    /// </summary>
    public EasingFunction AnimationEasing { get; set; } = Easing.EaseInOutQuad;

    /// <summary>
    /// 目标宽度
    /// </summary>
    public new double? Width
    {
        get => base.Width;
        set
        {
            if (base.Width != value && value.HasValue)
            {
                AnimateWidth(value.Value);
            }

            base.Width = value;
        }
    }

    /// <summary>
    /// 目标高度
    /// </summary>
    public new double? Height
    {
        get => base.Height;
        set
        {
            if (base.Height != value && value.HasValue)
            {
                AnimateHeight(value.Value);
            }

            base.Height = value;
        }
    }

    /// <summary>
    /// 目标不透明度
    /// </summary>
    public new double Opacity
    {
        get => base.Opacity;
        set
        {
            if (Math.Abs(base.Opacity - value) > 0.001)
            {
                AnimateOpacity(value);
            }

            base.Opacity = value;
        }
    }

    /// <summary>
    /// 目标背景色
    /// </summary>
    public new string? BackgroundColor
    {
        get => base.BackgroundColor;
        set
        {
            if (base.BackgroundColor != value && value != null)
            {
                AnimateBackgroundColor(value);
            }

            base.BackgroundColor = value;
        }
    }

    public AnimatedContainer(WidgetBase? child = null)
    {
        Child = child;
        _currentWidth = Width ?? 100;
        _currentHeight = Height ?? 100;
        _currentOpacity = Opacity;
        _currentBackgroundColor = BackgroundColor ?? "#FFFFFF";
        _currentPadding = Padding;
        _currentMargin = Margin;
    }

    public override object Build(BuildContext context)
    {
        _currentContainer = new Container(Child)
        {
            Width = _currentWidth,
            Height = _currentHeight,
            Opacity = _currentOpacity,
            BackgroundColor = _currentBackgroundColor,
            Padding = _currentPadding,
            Margin = _currentMargin,
            IsVisible = IsVisible
        };

        return _currentContainer.Build(context);
    }

    /// <summary>
    /// 宽度动画
    /// </summary>
    private void AnimateWidth(double targetWidth)
    {
        _widthAnimation?.Stop();

        _widthAnimation = new Animation<double>(_currentWidth, targetWidth, Transition.InterpolateDouble)
        {
            Duration = AnimationDuration,
            EasingFunc = AnimationEasing
        };

        _widthAnimation.Updated += (sender, e) =>
        {
            _currentWidth = e.Value;
            if (_currentContainer != null)
            {
                _currentContainer.Width = _currentWidth;
                Rebuild();
            }
        };

        _widthAnimation.Start();
    }

    /// <summary>
    /// 高度动画
    /// </summary>
    private void AnimateHeight(double targetHeight)
    {
        _heightAnimation?.Stop();

        _heightAnimation = new Animation<double>(_currentHeight, targetHeight, Transition.InterpolateDouble)
        {
            Duration = AnimationDuration,
            EasingFunc = AnimationEasing
        };

        _heightAnimation.Updated += (sender, e) =>
        {
            _currentHeight = e.Value;
            if (_currentContainer != null)
            {
                _currentContainer.Height = _currentHeight;
                Rebuild();
            }
        };

        _heightAnimation.Start();
    }

    /// <summary>
    /// 不透明度动画
    /// </summary>
    private void AnimateOpacity(double targetOpacity)
    {
        _opacityAnimation?.Stop();

        _opacityAnimation = new Animation<double>(_currentOpacity, targetOpacity, Transition.InterpolateDouble)
        {
            Duration = AnimationDuration,
            EasingFunc = AnimationEasing
        };

        _opacityAnimation.Updated += (sender, e) =>
        {
            _currentOpacity = e.Value;
            if (_currentContainer != null)
            {
                _currentContainer.Opacity = _currentOpacity;
                Rebuild();
            }
        };

        _opacityAnimation.Start();
    }

    /// <summary>
    /// 背景色动画
    /// </summary>
    private void AnimateBackgroundColor(string targetColor)
    {
        _colorAnimation?.Stop();

        _colorAnimation = new Animation<string>(_currentBackgroundColor ?? "#FFFFFF", targetColor,
            Transition.InterpolateColor)
        {
            Duration = AnimationDuration,
            EasingFunc = AnimationEasing
        };

        _colorAnimation.Updated += (sender, e) =>
        {
            _currentBackgroundColor = e.Value;
            if (_currentContainer != null)
            {
                _currentContainer.BackgroundColor = _currentBackgroundColor;
                Rebuild();
            }
        };

        _colorAnimation.Start();
    }

    /// <summary>
    /// 链式调用：设置子组件
    /// </summary>
    public AnimatedContainer SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    /// <summary>
    /// 链式调用：设置动画时长
    /// </summary>
    public AnimatedContainer SetAnimationDuration(int duration)
    {
        AnimationDuration = duration;
        return this;
    }

    /// <summary>
    /// 链式调用：设置缓动函数
    /// </summary>
    public AnimatedContainer SetAnimationEasing(EasingFunction easing)
    {
        AnimationEasing = easing;
        return this;
    }

    /// <summary>
    /// 链式调用：设置缓动函数（通过名称）
    /// </summary>
    public AnimatedContainer SetAnimationEasing(string easingName)
    {
        AnimationEasing = Easing.GetEasingFunction(easingName);
        return this;
    }
}