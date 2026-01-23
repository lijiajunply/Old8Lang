using Avalonia.Controls;
using Avalonia.Layout;
using Old8Lang.FirstUI.Core;
using LayoutHelper = Old8Lang.FirstUI.Utils.LayoutHelper;

namespace Old8Lang.FirstUI.Layout;

/// <summary>
/// Stack 层叠布局组件
/// 将子组件层叠放置，后面的组件覆盖在前面的组件上
/// </summary>
public class Stack : WidgetBase
{
    /// <summary>
    /// 子组件列表
    /// </summary>
    public List<WidgetBase> Children { get; set; } = [];

    /// <summary>
    /// 水平对齐方式
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;

    /// <summary>
    /// 垂直对齐方式
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;

    public Stack(List<WidgetBase>? children = null)
    {
        if (children != null)
            Children = children;
    }

    public override object Build(BuildContext context)
    {
        var panel = new Panel();

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(panel, this);

        // 构建子组件
        foreach (var child in Children)
        {
            var childControl = child.Build(context);
            if (childControl is Control control)
            {
                // 设置对齐方式
                control.HorizontalAlignment = HorizontalAlignment switch
                {
                    HorizontalAlignment.Left => HorizontalAlignment.Left,
                    HorizontalAlignment.Center => HorizontalAlignment.Center,
                    HorizontalAlignment.Right => HorizontalAlignment.Right,
                    HorizontalAlignment.Stretch => HorizontalAlignment.Stretch,
                    _ => HorizontalAlignment.Center
                };

                control.VerticalAlignment = VerticalAlignment switch
                {
                    VerticalAlignment.Top => VerticalAlignment.Top,
                    VerticalAlignment.Center => VerticalAlignment.Center,
                    VerticalAlignment.Bottom => VerticalAlignment.Bottom,
                    VerticalAlignment.Stretch => VerticalAlignment.Stretch,
                    _ => VerticalAlignment.Center
                };

                panel.Children.Add(control);
            }
        }

        return panel;
    }

    /// <summary>
    /// 链式调用：添加子组件
    /// </summary>
    public Stack AddChild(WidgetBase child)
    {
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// 链式调用：设置对齐方式
    /// </summary>
    public Stack SetAlignment(HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        HorizontalAlignment = horizontal;
        VerticalAlignment = vertical;
        return this;
    }
}

/// <summary>
/// ZStack (Stack 的别名，类似 SwiftUI)
/// </summary>
public class ZStack(List<WidgetBase>? children = null) : Stack(children);
