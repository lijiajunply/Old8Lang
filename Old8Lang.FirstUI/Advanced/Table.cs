using Avalonia.Controls;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Table 表格组件
/// 支持排序、筛选、分页等功能
/// </summary>
public class Table : WidgetBase
{
    /// <summary>
    /// 表格列定义
    /// </summary>
    public List<TableColumn> Columns { get; set; } = [];

    /// <summary>
    /// 表格数据
    /// </summary>
    public List<TableRow> Data { get; set; } = [];

    /// <summary>
    /// 行点击回调
    /// </summary>
    public Action<TableRow, int>? OnRowClick { get; set; }

    /// <summary>
    /// 排序变化回调
    /// </summary>
    public Action<string, bool>? OnSortChanged { get; set; }

    /// <summary>
    /// 筛选变化回调
    /// </summary>
    public Action<string?, string?>? OnFilterChanged { get; set; }

    /// <summary>
    /// 页码变化回调
    /// </summary>
    public Action<int>? OnPageChanged { get; set; }

    /// <summary>
    /// 当前排序列
    /// </summary>
    public string? SortColumn { get; set; }

    /// <summary>
    /// 排序方向（true=升序，false=降序）
    /// </summary>
    public bool SortAscending { get; set; } = true;

    /// <summary>
    /// 当前筛选值
    /// </summary>
    public string? FilterValue { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// 每页显示数量
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 是否显示分页
    /// </summary>
    public bool ShowPagination { get; set; } = true;

    /// <summary>
    /// 是否显示筛选
    /// </summary>
    public bool ShowFilter { get; set; } = true;

    /// <summary>
    /// 是否显示排序
    /// </summary>
    public bool ShowSorting { get; set; } = true;

    /// <summary>
    /// 是否显示行号
    /// </summary>
    public bool ShowRowNumbers { get; set; } = false;

    /// <summary>
    /// 是否启用行选择
    /// </summary>
    public bool RowSelection { get; set; } = false;

    /// <summary>
    /// 选中的行索引
    /// </summary>
    public HashSet<int> SelectedRows { get; set; } = [];

    /// <summary>
    /// 表格高度
    /// </summary>
    public double TableHeight { get; set; } = 400;

    /// <summary>
    /// 行高
    /// </summary>
    public double RowHeight { get; set; } = 40;

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 头部背景色
    /// </summary>
    public string? HeaderBackgroundColor { get; set; }

    /// <summary>
    /// 斑马纹颜色
    /// </summary>
    public string? StripedColor { get; set; }

    /// <summary>
    /// 选中行背景色
    /// </summary>
    public string? SelectedRowColor { get; set; }

    /// <summary>
    /// 悬停行背景色
    /// </summary>
    public string? HoverRowColor { get; set; }

    public override object Build(BuildContext context)
    {
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 筛选区域
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star }); // 表格区域
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 分页区域

        // 创建筛选区域
        if (ShowFilter)
        {
            var filterControl = CreateFilterArea();
            Grid.SetRow(filterControl, 0);
            mainGrid.Children.Add(filterControl);
        }

        // 创建表格区域
        var tableControl = CreateTableArea(context);
        Grid.SetRow(tableControl, 1);
        mainGrid.Children.Add(tableControl);

        // 创建分页区域
        if (ShowPagination)
        {
            var paginationControl = CreatePaginationArea();
            Grid.SetRow(paginationControl, 2);
            mainGrid.Children.Add(paginationControl);
        }

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(mainGrid, this);

        return mainGrid;
    }

    /// <summary>
    /// 创建筛选区域
    /// </summary>
    private Control CreateFilterArea()
    {
        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 8)
        };

        var filterInput = new TextBox
        {
            Watermark = "搜索...",
            Width = 200,
            Text = FilterValue ?? string.Empty
        };

        var searchButton = new Button
        {
            Content = "搜索",
            Width = 80
        };

        // 注册搜索事件
        searchButton.Click += (sender, e) =>
        {
            try
            {
                FilterValue = filterInput.Text;
                OnFilterChanged?.Invoke(filterInput.Text, null);
                CurrentPage = 1; // 重置到第一页
                OnPageChanged?.Invoke(CurrentPage);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Table] Error in filter: {ex.Message}");
            }
        };

        // Enter 键搜索
        filterInput.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                searchButton.Command?.Execute(null);
            }
        };

        stackPanel.Children.Add(filterInput);
        stackPanel.Children.Add(searchButton);

        return stackPanel;
    }

    /// <summary>
    /// 创建表格区域
    /// </summary>
    private Control CreateTableArea(BuildContext context)
    {
        var border = new Border
        {
            Height = TableHeight,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        // 设置边框颜色
        if (!string.IsNullOrEmpty(BorderColor))
        {
            border.BorderBrush = LayoutHelper.ParseColorBrush(BorderColor);
        }

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var grid = CreateTableGrid();
        scrollViewer.Content = grid;
        border.Child = scrollViewer;

        return border;
    }

    /// <summary>
    /// 创建表格网格
    /// </summary>
    private Grid CreateTableGrid()
    {
        var grid = new Grid();

        // 添加行号列（如果需要）
        if (ShowRowNumbers)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        }

        // 添加数据列
        foreach (var column in Columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = column.Width ?? GridLength.Star
            });
        }

        // 添加表头行
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var filteredData = FilterData();
        var sortedData = SortData(filteredData);

        // 创建表头
        for (int col = 0; col < GetEffectiveColumnCount(); col++)
        {
            var columnHeader = CreateHeaderCell(col);
            Grid.SetColumn(columnHeader, col);
            Grid.SetRow(columnHeader, 0);
            grid.Children.Add(columnHeader);
        }

        // 添加数据行
        var startIndex = (CurrentPage - 1) * PageSize;
        var endIndex = Math.Min(startIndex + PageSize, sortedData.Count);

        for (int row = 0; row < endIndex - startIndex; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });

            var rowData = sortedData[startIndex + row];
            var rowIndex = startIndex + row;

            // 创建行号单元格
            var cellIndex = 0;
            if (ShowRowNumbers)
            {
                var rowNumberCell = CreateRowNumberCell(rowIndex + 1);
                Grid.SetColumn(rowNumberCell, cellIndex++);
                Grid.SetRow(rowNumberCell, row + 1);
                grid.Children.Add(rowNumberCell);
            }

            // 创建数据单元格
            for (int col = 0; col < Columns.Count; col++)
            {
                var column = Columns[col];
                var cellValue = rowData.GetData(column.Key);
                var dataCell = CreateDataCell(cellValue, rowData, rowIndex, col);

                Grid.SetColumn(dataCell, cellIndex++);
                Grid.SetRow(dataCell, row + 1);
                grid.Children.Add(dataCell);
            }
        }

        return grid;
    }

    /// <summary>
    /// 创建表头单元格
    /// </summary>
    private Control CreateHeaderCell(int columnIndex)
    {
        var effectiveColumnIndex = ShowRowNumbers ? columnIndex - 1 : columnIndex;

        if (effectiveColumnIndex < 0 || effectiveColumnIndex >= Columns.Count)
        {
            // 行号表头
            return new Border
            {
                Child = new TextBlock { Text = "#", FontWeight = Avalonia.Media.FontWeight.Bold },
                Background = GetHeaderBrush(),
                Padding = new Avalonia.Thickness(8, 8, 8, 8)
            };
        }

        var column = Columns[effectiveColumnIndex];
        var border = new Border
        {
            Background = GetHeaderBrush(),
            Padding = new Avalonia.Thickness(8, 8, 8, 8)
        };

        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 4
        };

        var titleText = new TextBlock
        {
            Text = column.Title,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        stackPanel.Children.Add(titleText);

        // 添加排序按钮
        if (ShowSorting && column.Sortable)
        {
            var sortButton = CreateSortButton(column.Key);
            stackPanel.Children.Add(sortButton);
        }

        border.Child = stackPanel;
        return border;
    }

    /// <summary>
    /// 创建排序按钮
    /// </summary>
    private Control CreateSortButton(string columnKey)
    {
        var button = new Button
        {
            Content = GetSortSymbol(columnKey),
            Background = null,
            BorderThickness = new Avalonia.Thickness(0),
            Padding = new Avalonia.Thickness(4, 2),
            Width = 20,
            Height = 20
        };

        button.Click += (sender, e) =>
        {
            try
            {
                if (SortColumn == columnKey)
                {
                    SortAscending = !SortAscending;
                }
                else
                {
                    SortColumn = columnKey;
                    SortAscending = true;
                }

                OnSortChanged?.Invoke(columnKey, SortAscending);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Table] Error in sort: {ex.Message}");
            }
        };

        return button;
    }

    /// <summary>
    /// 获取排序符号
    /// </summary>
    private string GetSortSymbol(string columnKey)
    {
        if (SortColumn != columnKey) return "↕";
        return SortAscending ? "↑" : "↓";
    }

    /// <summary>
    /// 创建行号单元格
    /// </summary>
    private Control CreateRowNumberCell(int rowNumber)
    {
        return new Border
        {
            Child = new TextBlock
            {
                Text = rowNumber.ToString(),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            },
            Background = GetRowBackground(rowNumber - 1, true),
            Padding = new Avalonia.Thickness(8, 8, 8, 8)
        };
    }

    /// <summary>
    /// 创建数据单元格
    /// </summary>
    private Control CreateDataCell(object? cellValue, TableRow rowData, int rowIndex, int columnIndex)
    {
        var border = new Border
        {
            Child = new TextBlock
            {
                Text = cellValue?.ToString() ?? string.Empty,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(4, 0, 4, 0)
            },
            Background = GetRowBackground(rowIndex, false),
            Padding = new Avalonia.Thickness(8, 4, 8, 4)
        };

        // 添加点击事件
        if (OnRowClick != null || RowSelection)
        {
            var gestureButton = new Button
            {
                Content = border,
                Background = null,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };

            gestureButton.Click += (sender, e) =>
            {
                try
                {
                    // 处理行选择
                    if (RowSelection)
                    {
                        if (SelectedRows.Contains(rowIndex))
                        {
                            SelectedRows.Remove(rowIndex);
                        }
                        else
                        {
                            SelectedRows.Add(rowIndex);
                        }
                    }

                    // 触发行点击回调
                    OnRowClick?.Invoke(rowData, rowIndex);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Table] Error in row click: {ex.Message}");
                }
            };

            return gestureButton;
        }

        return border;
    }

    /// <summary>
    /// 创建分页区域
    /// </summary>
    private Control CreatePaginationArea()
    {
        var filteredData = FilterData();
        var totalItems = filteredData.Count;
        var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 8, 0, 8)
        };

        // 上一页按钮
        var prevButton = new Button
        {
            Content = "上一页",
            IsEnabled = CurrentPage > 1
        };

        prevButton.Click += (sender, e) =>
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                OnPageChanged?.Invoke(CurrentPage);
            }
        };

        // 页码显示
        var pageText = new TextBlock
        {
            Text = $"第 {CurrentPage} 页，共 {totalPages} 页（总计 {totalItems} 条）",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        // 下一页按钮
        var nextButton = new Button
        {
            Content = "下一页",
            IsEnabled = CurrentPage < totalPages
        };

        nextButton.Click += (sender, e) =>
        {
            if (CurrentPage < totalPages)
            {
                CurrentPage++;
                OnPageChanged?.Invoke(CurrentPage);
            }
        };

        stackPanel.Children.Add(prevButton);
        stackPanel.Children.Add(pageText);
        stackPanel.Children.Add(nextButton);

        return stackPanel;
    }

    /// <summary>
    /// 筛选数据
    /// </summary>
    private List<TableRow> FilterData()
    {
        if (string.IsNullOrEmpty(FilterValue))
        {
            return Data;
        }

        return Data.Where(row =>
        {
            return Columns.Any(column =>
            {
                var cellValue = row.GetData(column.Key);
                return cellValue?.ToString().IndexOf(FilterValue!, StringComparison.OrdinalIgnoreCase) >= 0;
            });
        }).ToList();
    }

    /// <summary>
    /// 排序数据
    /// </summary>
    private List<TableRow> SortData(List<TableRow> data)
    {
        if (string.IsNullOrEmpty(SortColumn))
        {
            return data;
        }

        var column = Columns.FirstOrDefault(c => c.Key == SortColumn);
        if (column == null)
        {
            return data;
        }

        return data.OrderBy(row =>
        {
            var value = row.GetData(SortColumn);
            return value?.ToString() ?? string.Empty;
        }).ToList();
    }

    /// <summary>
    /// 获取有效列数
    /// </summary>
    private int GetEffectiveColumnCount()
    {
        return Columns.Count + (ShowRowNumbers ? 1 : 0);
    }

    /// <summary>
    /// 获取行背景色
    /// </summary>
    private IBrush GetRowBackground(int rowIndex, bool isRowNumber)
    {
        // 选中行背景色
        if (!isRowNumber && SelectedRows.Contains(rowIndex))
        {
            return GetSelectedRowBrush();
        }

        // 斑马纹
        if (!isRowNumber && !string.IsNullOrEmpty(StripedColor) && rowIndex % 2 == 1)
        {
            return GetStripedBrush();
        }

        return Brushes.Transparent;
    }

    /// <summary>
    /// 获取头部背景画刷
    /// </summary>
    private IBrush GetHeaderBrush()
    {
        if (!string.IsNullOrEmpty(HeaderBackgroundColor))
        {
            return LayoutHelper.ParseColorBrush(HeaderBackgroundColor);
        }

        return new SolidColorBrush(Avalonia.Media.Color.FromRgb(240, 240, 240)); // 默认浅灰色
    }

    /// <summary>
    /// 获取斑马纹画刷
    /// </summary>
    private IBrush GetStripedBrush()
    {
        if (!string.IsNullOrEmpty(StripedColor))
        {
            return LayoutHelper.ParseColorBrush(StripedColor);
        }

        return new SolidColorBrush(Color.FromRgb(248, 248, 248)); // 默认极浅灰色
    }

    /// <summary>
    /// 获取选中行画刷
    /// </summary>
    private IBrush GetSelectedRowBrush()
    {
        if (!string.IsNullOrEmpty(SelectedRowColor))
        {
            return LayoutHelper.ParseColorBrush(SelectedRowColor);
        }

        return new SolidColorBrush(Color.FromRgb(0, 122, 255)); // 默认蓝色
    }

    // ======== 链式调用方法 ========

    /// <summary>
    /// 设置列定义
    /// </summary>
    public Table SetColumns(List<TableColumn> columns)
    {
        Columns = columns ?? [];
        return this;
    }

    /// <summary>
    /// 添加列
    /// </summary>
    public Table AddColumn(string key, string title, GridLength? width = null, bool sortable = true)
    {
        Columns.Add(new TableColumn { Key = key, Title = title, Width = width, Sortable = sortable });
        return this;
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public Table SetData(List<TableRow> data)
    {
        Data = data ?? [];
        return this;
    }

    /// <summary>
    /// 设置排序回调
    /// </summary>
    public Table SetOnSortChanged(Action<string, bool> onSortChanged)
    {
        OnSortChanged = onSortChanged;
        return this;
    }

    /// <summary>
    /// 设置行点击回调
    /// </summary>
    public Table SetOnRowClick(Action<TableRow, int> onRowClick)
    {
        OnRowClick = onRowClick;
        return this;
    }

    /// <summary>
    /// 设置分页参数
    /// </summary>
    public Table SetPagination(int pageSize = 10, bool show = true, Action<int>? onPageChanged = null)
    {
        PageSize = pageSize;
        ShowPagination = show;
        OnPageChanged = onPageChanged;
        return this;
    }

    /// <summary>
    /// 设置筛选参数
    /// </summary>
    public Table SetFilter(bool show = true, Action<string?, string?>? onFilterChanged = null)
    {
        ShowFilter = show;
        OnFilterChanged = onFilterChanged;
        return this;
    }

    /// <summary>
    /// 设置外观
    /// </summary>
    public Table SetAppearance(double height = 400, double rowHeight = 40,
        bool showRowNumbers = false, bool rowSelection = false)
    {
        TableHeight = height;
        RowHeight = rowHeight;
        ShowRowNumbers = showRowNumbers;
        RowSelection = rowSelection;
        return this;
    }

    /// <summary>
    /// 设置颜色主题
    /// </summary>
    public Table SetColors(string? borderColor = null, string? headerBg = null,
        string? stripedColor = null, string? selectedRowColor = null)
    {
        BorderColor = borderColor;
        HeaderBackgroundColor = headerBg;
        StripedColor = stripedColor;
        SelectedRowColor = selectedRowColor;
        return this;
    }
}

/// <summary>
/// 表格列定义
/// </summary>
public class TableColumn
{
    /// <summary>
    /// 列键名（对应数据中的key）
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 列标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 列宽
    /// </summary>
    public GridLength? Width { get; set; }

    /// <summary>
    /// 是否可排序
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// 对齐方式
    /// </summary>
    public Avalonia.Layout.HorizontalAlignment Alignment { get; set; } = Avalonia.Layout.HorizontalAlignment.Left;

    public TableColumn()
    {
    }

    public TableColumn(string key, string title, GridLength? width = null, bool sortable = true)
    {
        Key = key;
        Title = title;
        Width = width;
        Sortable = sortable;
    }
}

/// <summary>
/// 表格行数据
/// </summary>
public class TableRow
{
    private readonly Dictionary<string, object?> _data = [];

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(string key, object? value)
    {
        _data[key] = value;
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    public object? GetData(string key)
    {
        return _data.GetValueOrDefault(key);
    }

    /// <summary>
    /// 获取所有数据
    /// </summary>
    public Dictionary<string, object?> GetAllData()
    {
        return new Dictionary<string, object?>(_data);
    }

    /// <summary>
    /// 从字典创建行数据
    /// </summary>
    public static TableRow FromDictionary(Dictionary<string, object?> data)
    {
        var row = new TableRow();
        foreach (var kvp in data)
        {
            row.SetData(kvp.Key, kvp.Value);
        }

        return row;
    }

    /// <summary>
    /// 从对象创建行数据
    /// </summary>
    public static TableRow FromObject<T>(T obj)
    {
        var row = new TableRow();
        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            row.SetData(prop.Name, prop.GetValue(obj));
        }

        return row;
    }
}