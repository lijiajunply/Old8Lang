using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Old8Lang.FirstUI.Core;
using LayoutHelper = Old8Lang.FirstUI.Utils.LayoutHelper;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// ScrollView 滚动容器组件
/// </summary>
public class ScrollView(WidgetBase? child = null) : WidgetBase
{
    /// <summary>
    /// 子组件
    /// </summary>
    public WidgetBase? Child { get; set; } = child;

    /// <summary>
    /// 滚动方向
    /// </summary>
    public ScrollDirection Direction { get; set; } = ScrollDirection.Vertical;

    /// <summary>
    /// 是否显示滚动条
    /// </summary>
    public bool ShowScrollBar { get; set; } = true;

    /// <summary>
    /// 是否允许惯性滚动
    /// </summary>
    public bool IsInertiaEnabled { get; set; } = true;

    /// <summary>
    /// 滚动条可见性
    /// </summary>
    public ScrollBarVisibility VerticalScrollBarVisibility { get; set; } = ScrollBarVisibility.Auto;

    /// <summary>
    /// 滚动条可见性
    /// </summary>
    public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; } = ScrollBarVisibility.Auto;

    public override object Build(BuildContext context)
    {
        var scrollViewer = new ScrollViewer();

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(scrollViewer, this);

        // 设置滚动方向
        switch (Direction)
        {
            case ScrollDirection.Vertical:
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
                break;
            case ScrollDirection.Horizontal:
                scrollViewer.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                break;
            case ScrollDirection.Both:
                scrollViewer.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
                scrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
                break;
        }

        // 设置滚动条可见性
        if (!ShowScrollBar)
        {
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        // 构建子组件
        if (Child != null)
        {
            var childControl = Child.Build(context);
            if (childControl is Control control)
            {
                scrollViewer.Content = control;
            }
        }

        return scrollViewer;
    }

    /// <summary>
    /// 链式调用：设置子组件
    /// </summary>
    public ScrollView SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    /// <summary>
    /// 链式调用：设置滚动方向
    /// </summary>
    public ScrollView SetDirection(ScrollDirection direction)
    {
        Direction = direction;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示滚动条
    /// </summary>
    public ScrollView SetShowScrollBar(bool show)
    {
        ShowScrollBar = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置垂直滚动条可见性
    /// </summary>
    public ScrollView SetVerticalScrollBarVisibility(ScrollBarVisibility visibility)
    {
        VerticalScrollBarVisibility = visibility;
        return this;
    }

    /// <summary>
    /// 链式调用：设置水平滚动条可见性
    /// </summary>
    public ScrollView SetHorizontalScrollBarVisibility(ScrollBarVisibility visibility)
    {
        HorizontalScrollBarVisibility = visibility;
        return this;
    }
}

/// <summary>
/// 滚动方向枚举
/// </summary>
public enum ScrollDirection
{
    Vertical,
    Horizontal,
    Both
}
