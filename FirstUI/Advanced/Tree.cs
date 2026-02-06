using Avalonia.Controls;
using Avalonia.Media;
using FirstUI.Core;

namespace FirstUI.Advanced;

/// <summary>
/// Tree 树形控件组件
/// 支持层级数据展示、展开折叠、选择等功能
/// </summary>
public class Tree : WidgetBase
{
    /// <summary>
    /// 树根节点列表
    /// </summary>
    public List<TreeNode> Nodes { get; set; } = [];

    /// <summary>
    /// 节点点击回调
    /// </summary>
    public Action<TreeNode>? OnNodeClick { get; set; }

    /// <summary>
    /// 节点展开/折叠回调
    /// </summary>
    public Action<TreeNode, bool>? OnNodeExpanded { get; set; }

    /// <summary>
    /// 当前选中的节点
    /// </summary>
    public TreeNode? SelectedNode { get; set; }

    /// <summary>
    /// 节点高度
    /// </summary>
    public double NodeHeight { get; set; } = 32;

    /// <summary>
    /// 缩进距离
    /// </summary>
    public double IndentSize { get; set; } = 24;

    public override object Build(BuildContext context)
    {
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var treePanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical
        };

        // 构建树节点
        foreach (var node in Nodes)
        {
            var nodeControl = CreateTreeNode(node, 0, context);
            treePanel.Children.Add(nodeControl);
        }

        scrollViewer.Content = treePanel;

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(scrollViewer, this);

        return scrollViewer;
    }

    /// <summary>
    /// 创建树节点控件
    /// </summary>
    private Control CreateTreeNode(TreeNode node, int level, BuildContext context)
    {
        var container = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical
        };

        // 创建节点行
        var nodeRow = CreateNodeRow(node, level, context);
        container.Children.Add(nodeRow);

        // 创建子节点容器
        if (node.Children.Count > 0 && node.IsExpanded)
        {
            var childrenContainer = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
                Margin = new Avalonia.Thickness(IndentSize, 0, 0, 0)
            };

            foreach (var child in node.Children)
            {
                var childControl = CreateTreeNode(child, level + 1, context);
                childrenContainer.Children.Add(childControl);
            }

            container.Children.Add(childrenContainer);
        }

        return container;
    }

    /// <summary>
    /// 创建节点行
    /// </summary>
    private Control CreateNodeRow(TreeNode node, int level, BuildContext context)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(level * IndentSize) }); // 缩进
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });      // 展开/折叠图标
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });      // 节点图标
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });       // 节点文本

        // 展开/折叠图标
        if (node.Children.Count > 0)
        {
            var expandButton = CreateExpandButton(node);
            Grid.SetColumn(expandButton, 1);
            Grid.SetRow(expandButton, 0);
            grid.Children.Add(expandButton);
        }

        // 节点图标
        if (!string.IsNullOrEmpty(node.Icon))
        {
            var iconControl = CreateNodeIcon(node);
            Grid.SetColumn(iconControl, 2);
            Grid.SetRow(iconControl, 0);
            grid.Children.Add(iconControl);
        }

        // 节点文本
        var textControl = CreateNodeText(node);
        Grid.SetColumn(textControl, 3);
        Grid.SetRow(textControl, 0);
        grid.Children.Add(textControl);

        // 设置行高度和背景
        var border = new Border
        {
            Child = grid,
            Height = NodeHeight,
            Background = GetNodeBackground(node)
        };

        // 添加点击事件
        border.PointerPressed += (sender, e) =>
        {
            try
            {
                HandleNodeClick(node);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Tree] Error in node click: {ex.Message}");
            }
        };

        return border;
    }

    /// <summary>
    /// 创建展开/折叠按钮
    /// </summary>
    private Control CreateExpandButton(TreeNode node)
    {
        var button = new Button
        {
            Content = node.IsExpanded ? "▼" : "▶",
            Width = 16,
            Height = 16,
            Background = null,
            BorderThickness = new Avalonia.Thickness(0),
            Padding = new Avalonia.Thickness(0),
            FontSize = 10
        };

        button.Click += (sender, e) =>
        {
            try
            {
                node.IsExpanded = !node.IsExpanded;
                OnNodeExpanded?.Invoke(node, node.IsExpanded);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Tree] Error in expand button: {ex.Message}");
            }
        };

        return button;
    }

    /// <summary>
    /// 创建节点图标
    /// </summary>
    private Control CreateNodeIcon(TreeNode node)
    {
        var iconText = new TextBlock
        {
            Text = node.Icon!,
            FontSize = 16,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        return iconText;
    }

    /// <summary>
    /// 创建节点文本
    /// </summary>
    private Control CreateNodeText(TreeNode node)
    {
        var textBlock = new TextBlock
        {
            Text = node.Text,
            FontSize = 14,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        return textBlock;
    }

    /// <summary>
    /// 处理节点点击
    /// </summary>
    private void HandleNodeClick(TreeNode node)
    {
        OnNodeClick?.Invoke(node);
        SelectedNode = node;
    }

    /// <summary>
    /// 获取节点背景色
    /// </summary>
    private IBrush GetNodeBackground(TreeNode node)
    {
        if (SelectedNode == node)
        {
            return new SolidColorBrush(Color.FromRgb(0, 122, 255)); // 选中蓝色
        }

        return Brushes.Transparent;
    }

    // ======== 链式调用方法 ========

    /// <summary>
    /// 设置节点列表
    /// </summary>
    public Tree SetNodes(List<TreeNode> nodes)
    {
        Nodes = nodes ?? [];
        return this;
    }

    /// <summary>
    /// 添加根节点
    /// </summary>
    public Tree AddNode(string text, string? icon = null, List<TreeNode>? children = null)
    {
        var node = new TreeNode { Text = text, Icon = icon, Children = children ?? [] };
        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// 设置选择回调
    /// </summary>
    public Tree SetOnNodeClick(Action<TreeNode> onNodeClick)
    {
        OnNodeClick = onNodeClick;
        return this;
    }

    /// <summary>
    /// 设置展开回调
    /// </summary>
    public Tree SetOnNodeExpanded(Action<TreeNode, bool> onNodeExpanded)
    {
        OnNodeExpanded = onNodeExpanded;
        return this;
    }

    /// <summary>
    /// 设置尺寸
    /// </summary>
    public Tree SetSizes(double nodeHeight = 32, double indentSize = 24)
    {
        NodeHeight = nodeHeight;
        IndentSize = indentSize;
        return this;
    }
}

/// <summary>
/// 树节点
/// </summary>
public class TreeNode
{
    /// <summary>
    /// 节点文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 节点图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 节点数据
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 是否展开
    /// </summary>
    public bool IsExpanded { get; set; } = false;

    /// <summary>
    /// 子节点列表
    /// </summary>
    public List<TreeNode> Children { get; set; } = [];

    /// <summary>
    /// 父节点
    /// </summary>
    public TreeNode? Parent { get; set; }

    /// <summary>
    /// 节点层级
    /// </summary>
    public int Level => Parent?.Level + 1 ?? 0;

    /// <summary>
    /// 是否有子节点
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// 添加子节点
    /// </summary>
    public TreeNode AddChild(string text, string? icon = null, object? data = null)
    {
        var child = new TreeNode 
        { 
            Text = text, 
            Icon = icon, 
            Data = data,
            Parent = this
        };
        Children.Add(child);
        return child;
    }

    /// <summary>
    /// 移除子节点
    /// </summary>
    public bool RemoveChild(TreeNode child)
    {
        return Children.Remove(child);
    }

    /// <summary>
    /// 展开所有子节点
    /// </summary>
    public void ExpandAll()
    {
        IsExpanded = true;
        foreach (var child in Children)
        {
            child.ExpandAll();
        }
    }

    /// <summary>
    /// 折叠所有子节点
    /// </summary>
    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }

    /// <summary>
    /// 查找节点
    /// </summary>
    public TreeNode? FindNode(string text)
    {
        if (Text == text) return this;
        
        foreach (var child in Children)
        {
            var found = child.FindNode(text);
            if (found != null) return found;
        }
        
        return null;
    }

    public TreeNode()
    {
    }

    public TreeNode(string text, string? icon = null, object? data = null)
    {
        Text = text;
        Icon = icon;
        Data = data;
    }
}