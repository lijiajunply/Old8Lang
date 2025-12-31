using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using LayoutHelper = Old8Lang.FirstUI.Utils.LayoutHelper;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Pagination 分页组件
/// </summary>
public class Pagination : WidgetBase
{
    /// <summary>
    /// 当前页码（从1开始）
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; } = 1;

    /// <summary>
    /// 每页显示数量
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 总数据量
    /// </summary>
    public int TotalItems { get; set; } = 0;

    /// <summary>
    /// 页码变化回调
    /// </summary>
    public Action<int>? OnPageChanged { get; set; }

    /// <summary>
    /// 显示的页码按钮数量
    /// </summary>
    public int DisplayPageCount { get; set; } = 7;

    /// <summary>
    /// 是否显示首页/末页按钮
    /// </summary>
    public bool ShowFirstLast { get; set; } = true;

    /// <summary>
    /// 是否显示上一页/下一页按钮
    /// </summary>
    public bool ShowPrevNext { get; set; } = true;

    /// <summary>
    /// 是否显示总数信息
    /// </summary>
    public bool ShowTotalInfo { get; set; } = true;

    public override object Build(BuildContext context)
    {
        // 计算总页数
        if (TotalItems > 0 && PageSize > 0)
        {
            TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        }

        var container = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(container, this);

        // 显示总数信息
        if (ShowTotalInfo && TotalItems > 0)
        {
            var infoText = new TextBlock
            {
                Text = $"共 {TotalItems} 项",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 16, 0),
                Foreground = LayoutHelper.ParseColorBrush("#666666")
            };
            container.Children.Add(infoText);
        }

        // 首页按钮
        if (ShowFirstLast)
        {
            var firstButton = CreatePageButton("首页", 1, CurrentPage == 1);
            container.Children.Add(firstButton);
        }

        // 上一页按钮
        if (ShowPrevNext)
        {
            var prevButton = CreatePageButton("‹", CurrentPage - 1, CurrentPage <= 1);
            container.Children.Add(prevButton);
        }

        // 页码按钮
        var pageButtons = GetPageNumbers();
        foreach (var page in pageButtons)
        {
            if (page == -1)
            {
                // 省略号
                var ellipsis = new TextBlock
                {
                    Text = "...",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(4, 0)
                };
                container.Children.Add(ellipsis);
            }
            else
            {
                var pageButton = CreatePageButton(page.ToString(), page, false, page == CurrentPage);
                container.Children.Add(pageButton);
            }
        }

        // 下一页按钮
        if (ShowPrevNext)
        {
            var nextButton = CreatePageButton("›", CurrentPage + 1, CurrentPage >= TotalPages);
            container.Children.Add(nextButton);
        }

        // 末页按钮
        if (ShowFirstLast)
        {
            var lastButton = CreatePageButton("末页", TotalPages, CurrentPage == TotalPages);
            container.Children.Add(lastButton);
        }

        return container;
    }

    private Control CreatePageButton(string text, int targetPage, bool disabled, bool isActive = false)
    {
        var button = new Avalonia.Controls.Button
        {
            Content = text,
            MinWidth = 32,
            Height = 32,
            Padding = new Avalonia.Thickness(8, 4),
            IsEnabled = !disabled
        };

        // 设置样式
        if (isActive)
        {
            button.Background = LayoutHelper.ParseColorBrush("#2196F3");
            button.Foreground = LayoutHelper.ParseColorBrush("#FFFFFF");
            button.BorderThickness = new Avalonia.Thickness(0);
        }
        else
        {
            button.Background = LayoutHelper.ParseColorBrush("#F5F5F5");
            button.Foreground = LayoutHelper.ParseColorBrush("#333333");
            button.BorderThickness = new Avalonia.Thickness(1);
            button.BorderBrush = LayoutHelper.ParseColorBrush("#E0E0E0");
        }

        if (disabled)
        {
            button.Opacity = 0.5;
        }

        // 添加点击事件
        button.Click += (s, e) =>
        {
            if (targetPage >= 1 && targetPage <= TotalPages && targetPage != CurrentPage)
            {
                CurrentPage = targetPage;
                OnPageChanged?.Invoke(CurrentPage);
            }
        };

        return button;
    }

    private List<int> GetPageNumbers()
    {
        var pages = new List<int>();
        var half = DisplayPageCount / 2;

        if (TotalPages <= DisplayPageCount)
        {
            // 总页数小于显示数量，显示所有页码
            for (int i = 1; i <= TotalPages; i++)
            {
                pages.Add(i);
            }
        }
        else
        {
            // 计算显示范围
            int start = Math.Max(1, CurrentPage - half);
            int end = Math.Min(TotalPages, start + DisplayPageCount - 1);

            if (end - start < DisplayPageCount - 1)
            {
                start = Math.Max(1, end - DisplayPageCount + 1);
            }

            // 添加页码
            if (start > 1)
            {
                pages.Add(1);
                if (start > 2)
                {
                    pages.Add(-1); // 省略号
                }
            }

            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }

            if (end < TotalPages)
            {
                if (end < TotalPages - 1)
                {
                    pages.Add(-1); // 省略号
                }
                pages.Add(TotalPages);
            }
        }

        return pages;
    }

    /// <summary>
    /// 链式调用：设置当前页码
    /// </summary>
    public Pagination SetCurrentPage(int page)
    {
        CurrentPage = page;
        return this;
    }

    /// <summary>
    /// 链式调用：设置总页数
    /// </summary>
    public Pagination SetTotalPages(int total)
    {
        TotalPages = total;
        return this;
    }

    /// <summary>
    /// 链式调用：设置每页数量
    /// </summary>
    public Pagination SetPageSize(int size)
    {
        PageSize = size;
        return this;
    }

    /// <summary>
    /// 链式调用：设置总数据量
    /// </summary>
    public Pagination SetTotalItems(int total)
    {
        TotalItems = total;
        return this;
    }

    /// <summary>
    /// 链式调用：设置页码变化回调
    /// </summary>
    public Pagination SetOnPageChanged(Action<int> callback)
    {
        OnPageChanged = callback;
        return this;
    }

    /// <summary>
    /// 链式调用：设置显示的页码按钮数量
    /// </summary>
    public Pagination SetDisplayPageCount(int count)
    {
        DisplayPageCount = count;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示首页/末页按钮
    /// </summary>
    public Pagination SetShowFirstLast(bool show)
    {
        ShowFirstLast = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示上一页/下一页按钮
    /// </summary>
    public Pagination SetShowPrevNext(bool show)
    {
        ShowPrevNext = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示总数信息
    /// </summary>
    public Pagination SetShowTotalInfo(bool show)
    {
        ShowTotalInfo = show;
        return this;
    }
}
