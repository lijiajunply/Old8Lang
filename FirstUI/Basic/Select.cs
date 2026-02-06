using Avalonia.Controls;
using FirstUI.Core;
using FirstUI.Utils;

namespace FirstUI.Basic;

/// <summary>
/// Select 下拉选择框组件
/// 提供单选下拉列表功能
/// </summary>
public class Select : WidgetBase
{
    /// <summary>
    /// 选项列表
    /// </summary>
    public List<SelectOption> Options { get; set; } = [];

    /// <summary>
    /// 当前选中值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 占位符文本
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// 选择变化回调
    /// </summary>
    public Action<string?>? OnChanged { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 是否可编辑（可搜索）
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// 文本颜色
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColorOverride { get; set; }

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; } = 1;

    /// <summary>
    /// 圆角半径
    /// </summary>
    public double CornerRadius { get; set; } = 4;

    /// <summary>
    /// 下拉框高度
    /// </summary>
    public double MaxDropDownHeight { get; set; } = 300;

    /// <summary>
    /// 最大显示项数
    /// </summary>
    public int MaxVisibleItems { get; set; } = 10;

    public override object Build(BuildContext context)
    {
        var comboBox = new ComboBox
        {
            IsEnabled = !IsDisabled,
            IsEditable = IsEditable,
            FontSize = FontSize,
            MaxDropDownHeight = MaxDropDownHeight,
            PlaceholderText = Placeholder
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(comboBox, this);

        // 填充选项
        FillComboBoxItems(comboBox);

        // 设置当前选中值
        SetSelectedValue(comboBox);

        // 应用自定义样式
        ApplyComboBoxStyles(comboBox);

        // 注册选择事件
        RegisterSelectionEvent(comboBox);

        return comboBox;
    }

    /// <summary>
    /// 填充下拉框选项
    /// </summary>
    private void FillComboBoxItems(ComboBox comboBox)
    {
        comboBox.Items.Clear();

        foreach (var option in Options)
        {
            comboBox.Items.Add(new ComboBoxItem
            {
                Content = option.Label,
                Tag = option.Value
            });
        }
    }

    /// <summary>
    /// 设置当前选中值
    /// </summary>
    private void SetSelectedValue(ComboBox comboBox)
    {
        if (!string.IsNullOrEmpty(Value))
        {
            var targetItem = comboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag as string == Value);

            if (targetItem != null)
            {
                comboBox.SelectedItem = targetItem;
            }
        }
    }

    /// <summary>
    /// 应用下拉框样式
    /// </summary>
    private void ApplyComboBoxStyles(ComboBox comboBox)
    {
        // 设置文本颜色
        if (!string.IsNullOrEmpty(TextColor))
        {
            comboBox.Foreground = LayoutHelper.ParseColorBrush(TextColor);
        }

        // 设置背景颜色
        if (!string.IsNullOrEmpty(BackgroundColorOverride))
        {
            comboBox.Background = LayoutHelper.ParseColorBrush(BackgroundColorOverride);
        }

        // 设置边框
        if (!string.IsNullOrEmpty(BorderColor) && BorderWidth > 0)
        {
            comboBox.BorderBrush = LayoutHelper.ParseColorBrush(BorderColor);
            comboBox.BorderThickness = new Avalonia.Thickness(BorderWidth);
        }

        // 设置圆角
        if (CornerRadius > 0)
        {
            comboBox.CornerRadius = new Avalonia.CornerRadius(CornerRadius);
        }
    }

    /// <summary>
    /// 注册选择事件
    /// </summary>
    private void RegisterSelectionEvent(ComboBox comboBox)
    {
        if (OnChanged != null)
        {
            comboBox.SelectionChanged += (sender, e) =>
            {
                try
                {
                    if (comboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        var optionValue = selectedItem.Tag as string;
                        Value = optionValue;
                        OnChanged?.Invoke(optionValue);
                    }
                    else
                    {
                        Value = null;
                        OnChanged?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Select] Error in selection handler: {ex.Message}");
                }
            };
        }
    }

    /// <summary>
    /// 链式调用：设置选项列表
    /// </summary>
    public Select SetOptions(List<SelectOption> options)
    {
        Options = options ?? [];
        return this;
    }

    /// <summary>
    /// 链式调用：添加选项
    /// </summary>
    public Select AddOption(string label, string value)
    {
        Options.Add(new SelectOption { Label = label, Value = value });
        return this;
    }

    /// <summary>
    /// 链式调用：添加多个选项
    /// </summary>
    public Select AddOptions(IEnumerable<(string label, string value)> items)
    {
        foreach (var (label, value) in items)
        {
            Options.Add(new SelectOption { Label = label, Value = value });
        }
        return this;
    }

    /// <summary>
    /// 链式调用：设置当前值
    /// </summary>
    public Select SetValue(string? value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 链式调用：设置占位符
    /// </summary>
    public Select SetPlaceholder(string placeholder)
    {
        Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选择变化回调
    /// </summary>
    public Select SetOnChanged(Action<string?> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置禁用状态
    /// </summary>
    public Select SetDisabled(bool disabled)
    {
        IsDisabled = disabled;
        return this;
    }

    /// <summary>
    /// 链式调用：设置可编辑状态
    /// </summary>
    public Select SetEditable(bool editable)
    {
        IsEditable = editable;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public Select SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本颜色
    /// </summary>
    public Select SetTextColor(string color)
    {
        TextColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置背景颜色
    /// </summary>
    public Select SetBackgroundColorOverride(string color)
    {
        BackgroundColorOverride = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置边框
    /// </summary>
    public Select SetBorder(string color, double width = 1)
    {
        BorderColor = color;
        BorderWidth = width;
        return this;
    }

    /// <summary>
    /// 链式调用：设置圆角
    /// </summary>
    public Select SetCornerRadius(double radius)
    {
        CornerRadius = radius;
        return this;
    }

    /// <summary>
    /// 链式调用：设置下拉框高度
    /// </summary>
    public Select SetMaxDropDownHeight(double height)
    {
        MaxDropDownHeight = height;
        return this;
    }

    /// <summary>
    /// 链式调用：设置最大显示项数
    /// </summary>
    public Select SetMaxVisibleItems(int count)
    {
        MaxVisibleItems = count;
        return this;
    }
}

/// <summary>
/// 选择框选项
/// </summary>
public class SelectOption
{
    /// <summary>
    /// 显示标签
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 选项值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 选项图标（可选）
    /// </summary>
    public string? Icon { get; set; }

    public SelectOption()
    {
    }

    public SelectOption(string label, string value, bool isDisabled = false, string? icon = null)
    {
        Label = label;
        Value = value;
        IsDisabled = isDisabled;
        Icon = icon;
    }
}