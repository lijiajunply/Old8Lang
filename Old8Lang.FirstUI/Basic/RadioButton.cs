using Avalonia.Controls;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// RadioButton 单选按钮组件
/// </summary>
public class RadioButton : WidgetBase
{
    /// <summary>
    /// 标签文本
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// 组名（同组的单选按钮互斥）
    /// </summary>
    public string GroupName { get; set; } = "default";

    /// <summary>
    /// 值
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// 是否选中
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// 值变化回调
    /// </summary>
    public Action<bool>? OnChanged { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    public override object Build(BuildContext context)
    {
        var radioButton = new Avalonia.Controls.RadioButton
        {
            Content = Label,
            IsChecked = IsChecked,
            IsEnabled = IsEnabled,
            FontSize = FontSize,
            GroupName = GroupName
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(radioButton, this);

        // 注册值变化事件
        radioButton.Checked += (sender, e) =>
        {
            IsChecked = true;
            OnChanged?.Invoke(true);
        };

        radioButton.Unchecked += (sender, e) =>
        {
            IsChecked = false;
            OnChanged?.Invoke(false);
        };

        return radioButton;
    }

    /// <summary>
    /// 链式调用：设置标签
    /// </summary>
    public RadioButton SetLabel(string label)
    {
        Label = label;
        return this;
    }

    /// <summary>
    /// 链式调用：设置组名
    /// </summary>
    public RadioButton SetGroupName(string groupName)
    {
        GroupName = groupName;
        return this;
    }

    /// <summary>
    /// 链式调用：设置值
    /// </summary>
    public RadioButton SetValue(string value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中状态
    /// </summary>
    public RadioButton SetChecked(bool isChecked)
    {
        IsChecked = isChecked;
        return this;
    }

    /// <summary>
    /// 链式调用：设置变化回调
    /// </summary>
    public RadioButton SetOnChanged(Action<bool> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置禁用状态
    /// </summary>
    public RadioButton SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public RadioButton SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }
}

/// <summary>
/// RadioGroup 单选按钮组组件
/// </summary>
public class RadioGroup : WidgetBase
{
    /// <summary>
    /// 单选按钮列表
    /// </summary>
    public List<RadioButtonOption> Options { get; set; } = [];

    /// <summary>
    /// 组名
    /// </summary>
    public string GroupName { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 选中的值
    /// </summary>
    public string? SelectedValue { get; set; }

    /// <summary>
    /// 值变化回调
    /// </summary>
    public Action<string>? OnChanged { get; set; }

    /// <summary>
    /// 布局方向
    /// </summary>
    public RadioGroupOrientation Orientation { get; set; } = RadioGroupOrientation.Vertical;

    /// <summary>
    /// 间距
    /// </summary>
    public double Spacing { get; set; } = 8;

    public override object Build(BuildContext context)
    {
        var container = new StackPanel
        {
            Orientation = Orientation == RadioGroupOrientation.Vertical
                ? Avalonia.Layout.Orientation.Vertical
                : Avalonia.Layout.Orientation.Horizontal,
            Spacing = Spacing
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(container, this);

        foreach (var option in Options)
        {
            var radioButton = new Avalonia.Controls.RadioButton
            {
                Content = option.Label,
                GroupName = GroupName,
                IsChecked = option.Value == SelectedValue,
                IsEnabled = option.IsEnabled
            };

            // 注册值变化事件
            radioButton.Checked += (sender, e) =>
            {
                SelectedValue = option.Value;
                OnChanged?.Invoke(option.Value);
                option.OnClick?.Invoke();
            };

            container.Children.Add(radioButton);
        }

        return container;
    }

    /// <summary>
    /// 链式调用：添加选项
    /// </summary>
    public RadioGroup AddOption(RadioButtonOption option)
    {
        Options.Add(option);
        return this;
    }

    /// <summary>
    /// 链式调用：添加选项（简化版）
    /// </summary>
    public RadioGroup AddOption(string label, string value, bool isEnabled = true)
    {
        Options.Add(new RadioButtonOption
        {
            Label = label,
            Value = value,
            IsEnabled = isEnabled
        });
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中的值
    /// </summary>
    public RadioGroup SetSelectedValue(string value)
    {
        SelectedValue = value;
        return this;
    }

    /// <summary>
    /// 链式调用：设置值变化回调
    /// </summary>
    public RadioGroup SetOnChanged(Action<string> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置布局方向
    /// </summary>
    public RadioGroup SetOrientation(RadioGroupOrientation orientation)
    {
        Orientation = orientation;
        return this;
    }

    /// <summary>
    /// 链式调用：设置间距
    /// </summary>
    public RadioGroup SetSpacing(double spacing)
    {
        Spacing = spacing;
        return this;
    }
}

/// <summary>
/// 单选按钮选项
/// </summary>
public class RadioButtonOption
{
    /// <summary>
    /// 标签
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// 值
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 点击回调
    /// </summary>
    public Action? OnClick { get; set; }
}

/// <summary>
/// 单选按钮组布局方向枚举
/// </summary>
public enum RadioGroupOrientation
{
    Vertical,
    Horizontal
}
