using Avalonia.Controls;
using Avalonia.Layout;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Layout;

/// <summary>
/// Row 水平布局组件
/// 将子组件水平排列
/// </summary>
public class Row : WidgetBase
{
    /// <summary>
    /// 子组件列表
    /// </summary>
    public List<WidgetBase> Children { get; set; } = new();

    /// <summary>
    /// 主轴对齐方式（水平方向）
    /// </summary>
    public MainAxisAlignment MainAxisAlignment { get; set; } = MainAxisAlignment.Start;

    /// <summary>
    /// 交叉轴对齐方式（垂直方向）
    /// </summary>
    public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

    /// <summary>
    /// 子组件间距
    /// </summary>
    public double Spacing { get; set; }

    public Row(List<WidgetBase>? children = null)
    {
        if (children != null)
            Children = children;
    }

    public override object Build(BuildContext context)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Spacing
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(panel, this);

        // 设置主轴对齐
        panel.HorizontalAlignment = MainAxisAlignment switch
        {
            MainAxisAlignment.Start => HorizontalAlignment.Left,
            MainAxisAlignment.Center => HorizontalAlignment.Center,
            MainAxisAlignment.End => HorizontalAlignment.Right,
            MainAxisAlignment.SpaceBetween => HorizontalAlignment.Stretch,
            _ => HorizontalAlignment.Left
        };

        // 构建子组件
        foreach (var child in Children)
        {
            var childControl = child.Build(context);
            if (childControl is Control control)
            {
                // 设置交叉轴对齐
                control.VerticalAlignment = CrossAxisAlignment switch
                {
                    CrossAxisAlignment.Start => VerticalAlignment.Top,
                    CrossAxisAlignment.Center => VerticalAlignment.Center,
                    CrossAxisAlignment.End => VerticalAlignment.Bottom,
                    CrossAxisAlignment.Stretch => VerticalAlignment.Stretch,
                    _ => VerticalAlignment.Top
                };

                panel.Children.Add(control);
            }
        }

        return panel;
    }

    /// <summary>
    /// 链式调用：添加子组件
    /// </summary>
    public Row AddChild(WidgetBase child)
    {
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// 链式调用：设置间距
    /// </summary>
    public Row SetSpacing(double spacing)
    {
        Spacing = spacing;
        return this;
    }

    /// <summary>
    /// 链式调用：设置主轴对齐
    /// </summary>
    public Row SetMainAxisAlignment(MainAxisAlignment alignment)
    {
        MainAxisAlignment = alignment;
        return this;
    }

    /// <summary>
    /// 链式调用：设置交叉轴对齐
    /// </summary>
    public Row SetCrossAxisAlignment(CrossAxisAlignment alignment)
    {
        CrossAxisAlignment = alignment;
        return this;
    }
}

/// <summary>
/// Column 垂直布局组件
/// 将子组件垂直排列
/// </summary>
public class Column : WidgetBase
{
    /// <summary>
    /// 子组件列表
    /// </summary>
    public List<WidgetBase> Children { get; set; } = new();

    /// <summary>
    /// 主轴对齐方式（垂直方向）
    /// </summary>
    public MainAxisAlignment MainAxisAlignment { get; set; } = MainAxisAlignment.Start;

    /// <summary>
    /// 交叉轴对齐方式（水平方向）
    /// </summary>
    public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

    /// <summary>
    /// 子组件间距
    /// </summary>
    public double Spacing { get; set; }

    public Column(List<WidgetBase>? children = null)
    {
        if (children != null)
            Children = children;
    }

    public override object Build(BuildContext context)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = Spacing
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(panel, this);

        // 设置主轴对齐
        panel.VerticalAlignment = MainAxisAlignment switch
        {
            MainAxisAlignment.Start => VerticalAlignment.Top,
            MainAxisAlignment.Center => VerticalAlignment.Center,
            MainAxisAlignment.End => VerticalAlignment.Bottom,
            MainAxisAlignment.SpaceBetween => VerticalAlignment.Stretch,
            _ => VerticalAlignment.Top
        };

        // 构建子组件
        foreach (var child in Children)
        {
            var childControl = child.Build(context);
            if (childControl is Control control)
            {
                // 设置交叉轴对齐
                control.HorizontalAlignment = CrossAxisAlignment switch
                {
                    CrossAxisAlignment.Start => HorizontalAlignment.Left,
                    CrossAxisAlignment.Center => HorizontalAlignment.Center,
                    CrossAxisAlignment.End => HorizontalAlignment.Right,
                    CrossAxisAlignment.Stretch => HorizontalAlignment.Stretch,
                    _ => HorizontalAlignment.Left
                };

                panel.Children.Add(control);
            }
        }

        return panel;
    }

    /// <summary>
    /// 链式调用：添加子组件
    /// </summary>
    public Column AddChild(WidgetBase child)
    {
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// 链式调用：设置间距
    /// </summary>
    public Column SetSpacing(double spacing)
    {
        Spacing = spacing;
        return this;
    }

    /// <summary>
    /// 链式调用：设置主轴对齐
    /// </summary>
    public Column SetMainAxisAlignment(MainAxisAlignment alignment)
    {
        MainAxisAlignment = alignment;
        return this;
    }

    /// <summary>
    /// 链式调用：设置交叉轴对齐
    /// </summary>
    public Column SetCrossAxisAlignment(CrossAxisAlignment alignment)
    {
        CrossAxisAlignment = alignment;
        return this;
    }
}

/// <summary>
/// VStack (Column 的别名，类似 SwiftUI)
/// </summary>
public class VStack : Column
{
    public VStack(List<WidgetBase>? children = null) : base(children) { }
}

/// <summary>
/// HStack (Row 的别名，类似 SwiftUI)
/// </summary>
public class HStack : Row
{
    public HStack(List<WidgetBase>? children = null) : base(children) { }
}

/// <summary>
/// 主轴对齐方式
/// </summary>
public enum MainAxisAlignment
{
    /// <summary>开始位置</summary>
    Start,
    /// <summary>居中</summary>
    Center,
    /// <summary>结束位置</summary>
    End,
    /// <summary>均匀分布（两端对齐）</summary>
    SpaceBetween,
    /// <summary>均匀分布（周围留白）</summary>
    SpaceAround,
    /// <summary>均匀分布（等距）</summary>
    SpaceEvenly
}

/// <summary>
/// 交叉轴对齐方式
/// </summary>
public enum CrossAxisAlignment
{
    /// <summary>开始位置</summary>
    Start,
    /// <summary>居中</summary>
    Center,
    /// <summary>结束位置</summary>
    End,
    /// <summary>拉伸填充</summary>
    Stretch
}
