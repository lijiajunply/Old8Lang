using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Layout;

/// <summary>
/// 容器组件
/// </summary>
public class Container : WidgetBase
{
    public WidgetBase? Child { get; set; }

    public Container() { }

    public override object Build(BuildContext context)
    {
        var panel = new Panel
        {
            Opacity = Opacity,
            IsVisible = IsVisible
        };

        // 设置基本属性
        if (Width.HasValue) panel.Width = Width.Value;
        if (Height.HasValue) panel.Height = Height.Value;

        // 设置边距
        panel.Margin = new Avalonia.Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom);

        // 设置背景色
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            try
            {
                if (BackgroundColor.StartsWith("#"))
                {
                    panel.Background = new SolidColorBrush(Avalonia.Media.Color.Parse(BackgroundColor));
                }
                else
                {
                    // 简化处理：只支持十六进制颜色
                    panel.Background = Brushes.Gray;
                }
            }
            catch
            {
                panel.Background = Brushes.Gray;
            }
        }

        // 添加子组件
        if (Child != null)
        {
            var childControl = Child.Build(context) as Control;
            if (childControl != null)
            {
                panel.Children.Add(childControl);
            }
        }

        return panel;
    }
}

/// <summary>
/// 垂直布局组件
/// </summary>
public class Column : WidgetBase
{
    public List<WidgetBase> Children { get; set; } = new();
    public double Spacing { get; set; }

    public Column() { }

    public override object Build(BuildContext context)
    {
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Opacity = Opacity,
            IsVisible = IsVisible
        };

        // 设置基本属性
        if (Width.HasValue) stackPanel.Width = Width.Value;
        if (Height.HasValue) stackPanel.Height = Height.Value;

        // 设置边距
        stackPanel.Margin = new Avalonia.Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom);

        // 设置背景色
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            try
            {
                if (BackgroundColor.StartsWith("#"))
                {
                    stackPanel.Background = new SolidColorBrush(Avalonia.Media.Color.Parse(BackgroundColor));
                }
                else
                {
                    // 简化处理：忽略背景色
                }
            }
            catch
            {
                // 忽略背景色错误
            }
        }

        // 添加子组件
        foreach (var child in Children)
        {
            var childControl = child.Build(context) as Control;
            if (childControl != null)
            {
                // 设置间距
                if (Spacing > 0 && Children.IndexOf(child) > 0)
                {
                    childControl.Margin = new Avalonia.Thickness(0, Spacing, 0, 0);
                }

                stackPanel.Children.Add(childControl);
            }
        }

        return stackPanel;
    }
}