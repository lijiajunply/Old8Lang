using Avalonia.Controls;
using FirstUI.Core;
using FirstUI.Utils;

namespace FirstUI.Basic;

/// <summary>
/// Slider 滑块组件
/// 用于在指定范围内选择数值
/// </summary>
public class Slider(double value = 0) : WidgetBase
{
    /// <summary>
    /// 当前值
    /// </summary>
    public double Value { get; set; } = value;

    /// <summary>
    /// 最小值
    /// </summary>
    public double MinValue { get; set; } = 0;

    /// <summary>
    /// 最大值
    /// </summary>
    public double MaxValue { get; set; } = 100;

    /// <summary>
    /// 步长
    /// </summary>
    public double Step { get; set; } = 1;

    /// <summary>
    /// 是否显示数值标签
    /// </summary>
    public bool ShowValue { get; set; } = true;

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 活跃轨道颜色
    /// </summary>
    public string? ActiveTrackColor { get; set; }

    /// <summary>
    /// 非活跃轨道颜色
    /// </summary>
    public string? InactiveTrackColor { get; set; }

    /// <summary>
    /// 滑块颜色
    /// </summary>
    public string? ThumbColor { get; set; }

    /// <summary>
    /// 滑块大小
    /// </summary>
    public double ThumbSize { get; set; } = 20;

    /// <summary>
    /// 轨道高度
    /// </summary>
    public double TrackHeight { get; set; } = 4;

    /// <summary>
    /// 值变化回调
    /// </summary>
    public Action<double>? OnChanged { get; set; }

    /// <summary>
    /// 值变化完成回调（拖拽结束）
    /// </summary>
    public Action<double>? OnChangedEnd { get; set; }

    public override object Build(BuildContext context)
    {
        var slider = new Avalonia.Controls.Slider
        {
            Value = Value,
            Minimum = MinValue,
            Maximum = MaxValue,
            TickFrequency = Step,
            IsSnapToTickEnabled = Step > 0,
            IsEnabled = !IsDisabled
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(slider, this);

        // 设置滑块尺寸（如果未设置默认尺寸）
        if (Height == 0)
        {
            slider.Height = ThumbSize;
        }

        // 应用自定义样式
        ApplySliderStyles(slider);

        // 注册值变化事件
        if (OnChanged != null)
        {
            slider.PropertyChanged += (sender, e) =>
            {
                if (e.Property == Avalonia.Controls.Slider.ValueProperty)
                {
                    try
                    {
                        OnChanged?.Invoke(slider.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Slider] Error in change handler: {ex.Message}");
                    }
                }
            };
        }

        // 如果需要显示数值，创建容器包装
        if (ShowValue)
        {
            return CreateSliderWithValue(slider);
        }

        return slider;
    }

    /// <summary>
    /// 创建带数值显示的滑块
    /// </summary>
    private Control CreateSliderWithValue(Avalonia.Controls.Slider slider)
    {
        var valueText = new TextBlock
        {
            Text = Value.ToString("F0"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            MinWidth = 50,
            TextAlignment = Avalonia.Media.TextAlignment.Center
        };

        // 设置文本颜色（根据主题）
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            valueText.Foreground = LayoutHelper.ParseColorBrush(BackgroundColor);
        }

        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        stackPanel.Children.Add(slider);
        stackPanel.Children.Add(valueText);

        // 更新数值显示
        if (OnChanged != null)
        {
            slider.PropertyChanged += (sender, e) =>
            {
                if (e.Property == Avalonia.Controls.Slider.ValueProperty)
                {
                    valueText.Text = slider.Value.ToString("F0");
                }
            };
        }

        return stackPanel;
    }

    /// <summary>
    /// 应用滑块样式
    /// </summary>
    private void ApplySliderStyles(Avalonia.Controls.Slider slider)
    {
        // Avalonia Slider 的样式定制比较复杂
        // 这里提供基础支持，完整样式需要通过资源字典定义

        // 可以通过设置某些属性来影响外观
        if (!string.IsNullOrEmpty(ActiveTrackColor))
        {
            // 尝试使用 Foreground 作为活跃颜色
            slider.Foreground = LayoutHelper.ParseColorBrush(ActiveTrackColor);
        }
    }

    /// <summary>
    /// 链式调用：设置当前值
    /// </summary>
    public Slider SetValue(double value)
    {
        Value = Math.Max(MinValue, Math.Min(MaxValue, value));
        return this;
    }

    /// <summary>
    /// 链式调用：设置范围
    /// </summary>
    public Slider SetRange(double min, double max)
    {
        MinValue = min;
        MaxValue = max;
        Value = Math.Max(min, Math.Min(max, Value));
        return this;
    }

    /// <summary>
    /// 链式调用：设置步长
    /// </summary>
    public Slider SetStep(double step)
    {
        Step = step;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示数值
    /// </summary>
    public Slider SetShowValue(bool show)
    {
        ShowValue = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置禁用状态
    /// </summary>
    public Slider SetDisabled(bool disabled)
    {
        IsDisabled = disabled;
        return this;
    }

    /// <summary>
    /// 链式调用：设置活跃轨道颜色
    /// </summary>
    public Slider SetActiveTrackColor(string color)
    {
        ActiveTrackColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置非活跃轨道颜色
    /// </summary>
    public Slider SetInactiveTrackColor(string color)
    {
        InactiveTrackColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置滑块颜色
    /// </summary>
    public Slider SetThumbColor(string color)
    {
        ThumbColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置滑块尺寸
    /// </summary>
    public Slider SetThumbSize(double size)
    {
        ThumbSize = size;
        return this;
    }

    /// <summary>
    /// 链式调用：设置轨道高度
    /// </summary>
    public Slider SetTrackHeight(double height)
    {
        TrackHeight = height;
        return this;
    }

    /// <summary>
    /// 链式调用：设置值变化回调
    /// </summary>
    public Slider SetOnChanged(Action<double> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置值变化完成回调
    /// </summary>
    public Slider SetOnChangedEnd(Action<double> onChangedEnd)
    {
        OnChangedEnd = onChangedEnd;
        return this;
    }
}