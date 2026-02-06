using Avalonia.Controls;
using Avalonia.Layout;
using FirstUI.Core;
using LayoutHelper = FirstUI.Utils.LayoutHelper;
using LayoutHelper = FirstUI.Utils.LayoutHelper;

namespace FirstUI.Advanced;

/// <summary>
/// GridView 网格视图组件
/// </summary>
public class GridView : WidgetBase
{
    /// <summary>
    /// 数据源
    /// </summary>
    public List<object>? Items { get; set; }

    /// <summary>
    /// 列数
    /// </summary>
    public int Columns { get; set; } = 3;

    /// <summary>
    /// 网格项构建器函数
    /// </summary>
    public Func<object, int, WidgetBase>? ItemBuilder { get; set; }

    /// <summary>
    /// 网格项点击回调
    /// </summary>
    public Action<object, int>? OnItemClick { get; set; }

    /// <summary>
    /// 横向间距
    /// </summary>
    public double HorizontalSpacing { get; set; } = 8;

    /// <summary>
    /// 纵向间距
    /// </summary>
    public double VerticalSpacing { get; set; } = 8;

    /// <summary>
    /// 网格项宽度模式
    /// </summary>
    public GridItemWidthMode WidthMode { get; set; } = GridItemWidthMode.Auto;

    /// <summary>
    /// 固定宽度（当 WidthMode 为 Fixed 时使用）
    /// </summary>
    public double ItemWidth { get; set; } = 150;

    /// <summary>
    /// 网格项高度
    /// </summary>
    public double ItemHeight { get; set; } = 150;

    public override object Build(BuildContext context)
    {
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(scrollViewer, this);

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = VerticalSpacing
        };

        if (Items != null && ItemBuilder != null && Items.Count > 0)
        {
            var rows = (int)Math.Ceiling((double)Items.Count / Columns);

            for (int row = 0; row < rows; row++)
            {
                var rowPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = HorizontalSpacing
                };

                for (int col = 0; col < Columns; col++)
                {
                    var index = row * Columns + col;
                    if (index >= Items.Count)
                        break;

                    var item = Items[index];
                    var itemIndex = index;

                    // 使用 ItemBuilder 构建网格项
                    var itemWidget = ItemBuilder(item, itemIndex);
                    var itemControl = itemWidget.Build(context);

                    if (itemControl is Control control)
                    {
                        // 设置网格项容器
                        var itemContainer = new Border
                        {
                            Child = control
                        };

                        // 设置宽度
                        if (WidthMode == GridItemWidthMode.Fixed)
                        {
                            itemContainer.Width = ItemWidth;
                        }
                        else
                        {
                            itemContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
                        }

                        // 设置高度
                        if (ItemHeight > 0)
                        {
                            itemContainer.Height = ItemHeight;
                        }

                        // 添加点击事件
                        if (OnItemClick != null)
                        {
                            itemContainer.PointerPressed += (sender, e) =>
                            {
                                OnItemClick?.Invoke(item, itemIndex);
                            };
                            itemContainer.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                        }

                        rowPanel.Children.Add(itemContainer);
                    }
                }

                mainPanel.Children.Add(rowPanel);
            }
        }

        scrollViewer.Content = mainPanel;
        return scrollViewer;
    }

    /// <summary>
    /// 链式调用：设置数据源
    /// </summary>
    public GridView SetItems(List<object> items)
    {
        Items = items;
        return this;
    }

    /// <summary>
    /// 链式调用：设置列数
    /// </summary>
    public GridView SetColumns(int columns)
    {
        Columns = columns;
        return this;
    }

    /// <summary>
    /// 链式调用：设置网格项构建器
    /// </summary>
    public GridView SetItemBuilder(Func<object, int, WidgetBase> builder)
    {
        ItemBuilder = builder;
        return this;
    }

    /// <summary>
    /// 链式调用：设置网格项点击回调
    /// </summary>
    public GridView SetOnItemClick(Action<object, int> onClick)
    {
        OnItemClick = onClick;
        return this;
    }

    /// <summary>
    /// 链式调用：设置间距
    /// </summary>
    public GridView SetSpacing(double horizontal, double vertical)
    {
        HorizontalSpacing = horizontal;
        VerticalSpacing = vertical;
        return this;
    }

    /// <summary>
    /// 链式调用：设置网格项大小
    /// </summary>
    public GridView SetItemSize(double width, double height)
    {
        ItemWidth = width;
        ItemHeight = height;
        WidthMode = GridItemWidthMode.Fixed;
        return this;
    }

    /// <summary>
    /// 链式调用：设置宽度模式
    /// </summary>
    public GridView SetWidthMode(GridItemWidthMode mode)
    {
        WidthMode = mode;
        return this;
    }
}

/// <summary>
/// 网格项宽度模式枚举
/// </summary>
public enum GridItemWidthMode
{
    Auto,
    Fixed
}
