using Avalonia.Controls;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// ProgressBar 进度条组件
/// 用于显示进度或加载状态
/// </summary>
public class ProgressBar(double value = 0) : WidgetBase
{
    /// <summary>
    /// 进度值 (0.0 - 1.0)
    /// </summary>
    public double Value { get; set; } = Math.Max(0, Math.Min(1, value));

    /// <summary>
    /// 是否为不确定进度（无限循环动画）
    /// </summary>
    public bool IsIndeterminate { get; set; }

    /// <summary>
    /// 进度条颜色
    /// </summary>
    public string? ProgressColor { get; set; }

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColorOverride { get; set; }

    /// <summary>
    /// 是否显示百分比文本
    /// </summary>
    public bool ShowPercentage { get; set; } = false;

    /// <summary>
    /// 进度条高度
    /// </summary>
    public double ProgressHeight { get; set; } = 8;

    /// <summary>
    /// 圆角半径
    /// </summary>
    public double CornerRadius { get; set; } = 4;

    /// <summary>
    /// 进度条样式变体
    /// </summary>
    public ProgressBarVariant Variant { get; set; } = ProgressBarVariant.Linear;

    /// <summary>
    /// 圆形进度条大小（仅适用于 Circular 变体）
    /// </summary>
    public double CircularSize { get; set; } = 60;

    /// <summary>
    /// 圆形进度条宽度（仅适用于 Circular 变体）
    /// </summary>
    public double CircularThickness { get; set; } = 6;

    public override object Build(BuildContext context)
    {
        if (Variant == ProgressBarVariant.Linear)
        {
            return BuildLinearProgressBar();
        }
        else
        {
            return BuildCircularProgressBar();
        }
    }

    /// <summary>
    /// 构建线性进度条
    /// </summary>
    private Control BuildLinearProgressBar()
    {
        var progressBar = new Avalonia.Controls.ProgressBar
        {
            Value = IsIndeterminate ? 0 : Value * 100,
            IsIndeterminate = IsIndeterminate,
            Height = ProgressHeight
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(progressBar, this);

        // 应用进度条样式
        ApplyProgressBarStyles(progressBar);

        // 如果需要显示百分比，创建包装容器
        if (ShowPercentage && !IsIndeterminate)
        {
            return CreateProgressBarWithText(progressBar);
        }

        return progressBar;
    }

    /// <summary>
    /// 构建圆形进度条（简化实现）
    /// </summary>
    private Control BuildCircularProgressBar()
    {
        var grid = new Grid
        {
            Width = CircularSize,
            Height = CircularSize,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        // 创建简单的圆形进度条外观
        var border = new Border
        {
            Width = CircularSize,
            Height = CircularSize,
            Background = Brushes.Transparent,
            BorderBrush = GetBackgroundBrush(),
            BorderThickness = new Avalonia.Thickness(CircularThickness),
            CornerRadius = new Avalonia.CornerRadius(CircularSize / 2)
        };

        grid.Children.Add(border);

        // 添加百分比文本
        if (ShowPercentage && !IsIndeterminate)
        {
            var percentText = new TextBlock
            {
                Text = $"{(Value * 100):F0}%",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = CircularSize / 6,
                FontWeight = FontWeight.Bold
            };

            if (!string.IsNullOrEmpty(ProgressColor))
            {
                percentText.Foreground = LayoutHelper.ParseColorBrush(ProgressColor);
            }

            grid.Children.Add(percentText);
        }

        return grid;
    }

    /// <summary>
    /// 创建带文本的进度条
    /// </summary>
    private Control CreateProgressBarWithText(Avalonia.Controls.ProgressBar progressBar)
    {
        var percentText = new TextBlock
        {
            Text = $"{(Value * 100):F0}%",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(10, 0, 0, 0),
            MinWidth = 40
        };

        if (!string.IsNullOrEmpty(ProgressColor))
        {
            percentText.Foreground = LayoutHelper.ParseColorBrush(ProgressColor);
        }

        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        stackPanel.Children.Add(progressBar);
        stackPanel.Children.Add(percentText);

        return stackPanel;
    }

    /// <summary>
    /// 应用进度条样式
    /// </summary>
    private void ApplyProgressBarStyles(Avalonia.Controls.ProgressBar progressBar)
    {
        // 设置圆角
        progressBar.CornerRadius = new Avalonia.CornerRadius(CornerRadius);

        // 设置颜色
        if (!string.IsNullOrEmpty(ProgressColor))
        {
            progressBar.Foreground = LayoutHelper.ParseColorBrush(ProgressColor);
        }

        if (!string.IsNullOrEmpty(BackgroundColorOverride))
        {
            progressBar.Background = LayoutHelper.ParseColorBrush(BackgroundColorOverride);
        }
    }

    /// <summary>
    /// 获取进度画刷
    /// </summary>
    private IBrush GetProgressBrush()
    {
        if (!string.IsNullOrEmpty(ProgressColor))
        {
            return LayoutHelper.ParseColorBrush(ProgressColor);
        }

        return Brushes.DodgerBlue; // 默认颜色
    }

    /// <summary>
    /// 获取背景画刷
    /// </summary>
    private IBrush GetBackgroundBrush()
    {
        if (!string.IsNullOrEmpty(BackgroundColorOverride))
        {
            return LayoutHelper.ParseColorBrush(BackgroundColorOverride);
        }

        return Brushes.LightGray; // 默认背景色
    }

    /// <summary>
    /// 链式调用：设置进度值
    /// </summary>
    public ProgressBar SetValue(double value)
    {
        Value = Math.Max(0, Math.Min(1, value));
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否为不确定进度
    /// </summary>
    public ProgressBar SetIndeterminate(bool indeterminate)
    {
        IsIndeterminate = indeterminate;
        return this;
    }

    /// <summary>
    /// 链式调用：设置进度颜色
    /// </summary>
    public ProgressBar SetProgressColor(string color)
    {
        ProgressColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置背景颜色
    /// </summary>
    public ProgressBar SetBackgroundColorOverride(string color)
    {
        BackgroundColorOverride = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示百分比
    /// </summary>
    public ProgressBar SetShowPercentage(bool show)
    {
        ShowPercentage = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置进度条高度
    /// </summary>
    public ProgressBar SetProgressHeight(double height)
    {
        ProgressHeight = height;
        return this;
    }

    /// <summary>
    /// 链式调用：设置圆角半径
    /// </summary>
    public ProgressBar SetCornerRadius(double radius)
    {
        CornerRadius = radius;
        return this;
    }

    /// <summary>
    /// 链式调用：设置进度条变体
    /// </summary>
    public ProgressBar SetVariant(ProgressBarVariant variant)
    {
        Variant = variant;
        return this;
    }

    /// <summary>
    /// 链式调用：设置圆形进度条尺寸
    /// </summary>
    public ProgressBar SetCircularSize(double size)
    {
        CircularSize = size;
        return this;
    }

    /// <summary>
    /// 链式调用：设置圆形进度条宽度
    /// </summary>
    public ProgressBar SetCircularThickness(double thickness)
    {
        CircularThickness = thickness;
        return this;
    }
}

/// <summary>
/// 进度条样式变体
/// </summary>
public enum ProgressBarVariant
{
    /// <summary>
    /// 线性进度条
    /// </summary>
    Linear,

    /// <summary>
    /// 圆形进度条
    /// </summary>
    Circular
}