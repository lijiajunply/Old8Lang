using Avalonia.Controls;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// FilePicker 文件选择器组件
/// 提供文件和文件夹选择功能
/// </summary>
public class FilePicker : WidgetBase
{
    /// <summary>
    /// 当前选中的文件路径
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 文件选择回调
    /// </summary>
    public Action<string?>? OnFileSelected { get; set; }

    /// <summary>
    /// 文件变化回调
    /// </summary>
    public Action<string?>? OnChanged { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 对话框标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 选择器宽度
    /// </summary>
    public double PickerWidth { get; set; } = 500;

    /// <summary>
    /// 选择器高度
    /// </summary>
    public double PickerHeight { get; set; } = 400;

    /// <summary>
    /// 是否显示当前路径
    /// </summary>
    public bool ShowCurrentPath { get; set; } = true;

    /// <summary>
    /// 初始目录
    /// </summary>
    public string? InitialDirectory { get; set; }

    /// <summary>
    /// 默认文件扩展名
    /// </summary>
    public string? DefaultExtension { get; set; }

    public override object Build(BuildContext context)
    {
        var container = CreateFilePickerControl();

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(container, this);

        return container;
    }

    /// <summary>
    /// 创建文件选择器控件
    /// </summary>
    private Control CreateFilePickerControl()
    {
        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Spacing = 8
        };

        // 创建文件选择按钮
        var selectButton = CreateSelectButton();
        stackPanel.Children.Add(selectButton);

        // 创建当前路径显示
        if (ShowCurrentPath)
        {
            var pathDisplay = CreatePathDisplay();
            stackPanel.Children.Add(pathDisplay);
        }

        // 设置容器尺寸
        if (PickerWidth > 0) stackPanel.Width = PickerWidth;
        if (PickerHeight > 0) stackPanel.Height = PickerHeight;

        return stackPanel;
    }

    /// <summary>
    /// 创建选择按钮
    /// </summary>
    private Control CreateSelectButton()
    {
        var button = new Avalonia.Controls.Button
        {
            Content = Title ?? "选择文件",
            Width = 200,
            Height = 40
        };

        // 注册点击事件
        button.Click += async (sender, e) =>
        {
            await ShowOpenFileDialog();
        };

        return button;
    }

    /// <summary>
    /// 创建路径显示
    /// </summary>
    private Control CreatePathDisplay()
    {
        var border = new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(245, 245, 245)),
            Padding = new Avalonia.Thickness(12, 8, 12, 8),
            CornerRadius = new Avalonia.CornerRadius(4),
            MinHeight = 32
        };

        var textBlock = new TextBlock
        {
            Text = Value ?? "未选择文件",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };

        border.Child = textBlock;
        return border;
    }

    /// <summary>
    /// 显示文件选择对话框
    /// </summary>
    private async System.Threading.Tasks.Task ShowOpenFileDialog()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = Title ?? "选择文件"
            };

            // 设置初始目录
            if (!string.IsNullOrEmpty(InitialDirectory))
            {
                dialog.Directory = InitialDirectory;
            }

            // 设置对话框属性
            if (!string.IsNullOrEmpty(Title))
            {
                dialog.Title = Title;
            }

            // 设置默认扩展名
            // 在 OpenFileDialog 中设置 DefaultExtension 可能需要特殊处理
            // 这里提供基础实现

            // 显示对话框
            var result = await dialog.ShowAsync(new Window());

            // 处理结果
            HandleDialogResult(result);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FilePicker] Error showing dialog: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理对话框结果
    /// </summary>
    private void HandleDialogResult(string[]? result)
    {
        try
        {
            if (result != null && result.Length > 0)
            {
                // 选择第一个文件
                Value = result[0];
                OnFileSelected?.Invoke(Value);
                OnChanged?.Invoke(Value);
            }
            else
            {
                // 用户取消选择
                Value = null;
                OnFileSelected?.Invoke(null);
                OnChanged?.Invoke(null);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FilePicker] Error handling dialog result: {ex.Message}");
        }
    }

    // ======== 链式调用方法 ========

    /// <summary>
    /// 设置文件路径
    /// </summary>
    public FilePicker SetValue(string? value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 设置文件选择回调
    /// </summary>
    public FilePicker SetOnFileSelected(Action<string?> onFileSelected)
    {
        OnFileSelected = onFileSelected;
        return this;
    }

    /// <summary>
    /// 设置变化回调
    /// </summary>
    public FilePicker SetOnChanged(Action<string?> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 设置对话框属性
    /// </summary>
    public FilePicker SetDialogProperties(string? title = null, string? defaultExt = null, 
        string? initialDir = null, bool showCurrentPath = true)
    {
        Title = title;
        DefaultExtension = defaultExt;
        InitialDirectory = initialDir;
        ShowCurrentPath = showCurrentPath;
        return this;
    }

    /// <summary>
    /// 设置尺寸
    /// </summary>
    public FilePicker SetSize(double width = 500, double height = 400)
    {
        PickerWidth = width;
        PickerHeight = height;
        return this;
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public FilePicker SetStates(bool isDisabled = false)
    {
        IsDisabled = isDisabled;
        return this;
    }

    // ======== 辅助方法 ========

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    public bool FileExists()
    {
        return !string.IsNullOrEmpty(Value) && System.IO.File.Exists(Value);
    }

    /// <summary>
    /// 获取文件扩展名
    /// </summary>
    public string GetFileExtension()
    {
        if (string.IsNullOrEmpty(Value))
            return string.Empty;

        return System.IO.Path.GetExtension(Value);
    }

    /// <summary>
    /// 获取文件名（不包含路径）
    /// </summary>
    public string GetFileName()
    {
        if (string.IsNullOrEmpty(Value))
            return string.Empty;

        return System.IO.Path.GetFileName(Value);
    }

    /// <summary>
    /// 获取目录路径
    /// </summary>
    public string GetDirectory()
    {
        if (string.IsNullOrEmpty(Value))
            return string.Empty;

        return System.IO.Path.GetDirectoryName(Value) ?? string.Empty;
    }
}