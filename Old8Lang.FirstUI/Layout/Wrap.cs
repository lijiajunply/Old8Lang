using Avalonia.Controls;
using Avalonia.Layout;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Layout;

/// <summary>
/// Wrap 流式布局组件
/// 支持自动换行/换列，子组件按顺序排列
/// </summary>
public class Wrap : WidgetBase
{
    /// <summary>
    /// 子组件列表
    /// </summary>
    public List<WidgetBase> Children { get; set; } = [];

    /// <summary>
    /// 主轴方向（水平或垂直）
    /// </summary>
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>
    /// 主轴对齐方式
    /// </summary>
    public WrapAlignment MainAxisAlignment { get; set; } = WrapAlignment.Start;

    /// <summary>
    /// 交叉轴对齐方式
    /// </summary>
    public WrapAlignment CrossAxisAlignment { get; set; } = WrapAlignment.Start;

    /// <summary>
    /// 子组件间距
    /// </summary>
    public double Spacing { get; set; } = 4;

    /// <summary>
    /// 行间距（当 Orientation 为 Horizontal 时）
    /// </summary>
    public double RunSpacing { get; set; } = 4;

    /// <summary>
    /// 是否拉伸子组件填满交叉轴
    /// </summary>
    public bool StretchChildren { get; set; } = false;

    public override object Build(BuildContext context)
    {
        var wrapPanel = new WrapPanel
        {
            Orientation = Orientation,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(wrapPanel, this);

        // WrapPanel 本身不支持间距设置，需要通过子组件的 Margin 来实现
        // 这里我们将在添加子组件时处理间距

        // 设置对齐方式
        SetAlignment(wrapPanel);

        // 添加子组件
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var childControl = child.Build(context);
            if (childControl is Control control)
            {
                // 设置间距（除了第一个元素）
                if (i > 0)
                {
                    double horizontalSpacing = Orientation == Orientation.Horizontal ? Spacing : RunSpacing;
                    double verticalSpacing = Orientation == Orientation.Vertical ? Spacing : RunSpacing;
                    
                    control.Margin = new Avalonia.Thickness(horizontalSpacing, verticalSpacing, 0, 0);
                }

                // 如果需要拉伸子组件
                if (StretchChildren)
                {
                    control.HorizontalAlignment = HorizontalAlignment.Stretch;
                    control.VerticalAlignment = VerticalAlignment.Stretch;
                }

                wrapPanel.Children.Add(control);
            }
        }

        return wrapPanel;
    }

    /// <summary>
    /// 设置对齐方式
    /// </summary>
    private void SetAlignment(WrapPanel wrapPanel)
    {
        // WrapPanel 的对齐方式通过 HorizontalAlignment 和 VerticalAlignment 控制
        // 这里需要根据 WrapAlignment 来调整
        wrapPanel.HorizontalAlignment = MainAxisAlignment switch
        {
            WrapAlignment.Start => HorizontalAlignment.Left,
            WrapAlignment.Center => HorizontalAlignment.Center,
            WrapAlignment.End => HorizontalAlignment.Right,
            WrapAlignment.SpaceBetween => HorizontalAlignment.Stretch,
            WrapAlignment.SpaceAround => HorizontalAlignment.Stretch,
            WrapAlignment.SpaceEvenly => HorizontalAlignment.Stretch,
            _ => HorizontalAlignment.Left
        };

        // 垂直对齐
        wrapPanel.VerticalAlignment = CrossAxisAlignment switch
        {
            WrapAlignment.Start => VerticalAlignment.Top,
            WrapAlignment.Center => VerticalAlignment.Center,
            WrapAlignment.End => VerticalAlignment.Bottom,
            WrapAlignment.Stretch => VerticalAlignment.Stretch,
            _ => VerticalAlignment.Top
        };
    }

    /// <summary>
    /// 链式调用：添加子组件
    /// </summary>
    public Wrap AddChild(WidgetBase child)
    {
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// 链式调用：添加多个子组件
    /// </summary>
    public Wrap AddChildren(IEnumerable<WidgetBase> children)
    {
        Children.AddRange(children);
        return this;
    }

    /// <summary>
    /// 链式调用：设置子组件列表
    /// </summary>
    public Wrap SetChildren(List<WidgetBase> children)
    {
        Children = children;
        return this;
    }

    /// <summary>
    /// 链式调用：设置方向
    /// </summary>
    public Wrap SetOrientation(Orientation orientation)
    {
        Orientation = orientation;
        return this;
    }

    /// <summary>
    /// 链式调用：设置主轴对齐
    /// </summary>
    public Wrap SetMainAxisAlignment(WrapAlignment alignment)
    {
        MainAxisAlignment = alignment;
        return this;
    }

    /// <summary>
    /// 链式调用：设置交叉轴对齐
    /// </summary>
    public Wrap SetCrossAxisAlignment(WrapAlignment alignment)
    {
        CrossAxisAlignment = alignment;
        return this;
    }

    /// <summary>
    /// 链式调用：设置间距
    /// </summary>
    public Wrap SetSpacing(double spacing, double? runSpacing = null)
    {
        Spacing = spacing;
        if (runSpacing.HasValue)
        {
            RunSpacing = runSpacing.Value;
        }
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否拉伸子组件
    /// </summary>
    public Wrap SetStretchChildren(bool stretch)
    {
        StretchChildren = stretch;
        return this;
    }
}

/// <summary>
/// Wrap 布局对齐方式
/// </summary>
public enum WrapAlignment
{
    /// <summary>
    /// 起始对齐
    /// </summary>
    Start,

    /// <summary>
    /// 居中对齐
    /// </summary>
    Center,

    /// <summary>
    /// 结束对齐
    /// </summary>
    End,

    /// <summary>
    /// 两端对齐，项目之间的间隔都相等
    /// </summary>
    SpaceBetween,

    /// <summary>
    /// 每个项目两侧的间隔相等
    /// </summary>
    SpaceAround,

    /// <summary>
    /// 每个项目之间的间隔都相等
    /// </summary>
    SpaceEvenly,

    /// <summary>
    /// 拉伸填满
    /// </summary>
    Stretch
}