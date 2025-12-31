using Avalonia.Controls;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// Checkbox 复选框组件
/// </summary>
public class Checkbox : WidgetBase
{
    /// <summary>
    /// 标签文本
    /// </summary>
    public string Label { get; set; } = "";

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
    /// 是否三态（null, true, false）
    /// </summary>
    public bool IsThreeState { get; set; } = false;

    /// <summary>
    /// 三态值
    /// </summary>
    public bool? ThreeStateValue { get; set; }

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    public override object Build(BuildContext context)
    {
        var checkbox = new CheckBox
        {
            Content = Label,
            IsChecked = IsThreeState ? ThreeStateValue : IsChecked,
            IsEnabled = IsEnabled,
            FontSize = FontSize,
            IsThreeState = IsThreeState
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(checkbox, this);

        // 注册值变化事件
        checkbox.Checked += (sender, e) =>
        {
            if (IsThreeState)
            {
                ThreeStateValue = true;
            }
            else
            {
                IsChecked = true;
            }
            OnChanged?.Invoke(true);
        };

        checkbox.Unchecked += (sender, e) =>
        {
            if (IsThreeState)
            {
                ThreeStateValue = false;
            }
            else
            {
                IsChecked = false;
            }
            OnChanged?.Invoke(false);
        };

        checkbox.Indeterminate += (sender, e) =>
        {
            if (IsThreeState)
            {
                ThreeStateValue = null;
            }
        };

        return checkbox;
    }

    /// <summary>
    /// 链式调用：设置标签
    /// </summary>
    public Checkbox SetLabel(string label)
    {
        Label = label;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中状态
    /// </summary>
    public Checkbox SetChecked(bool isChecked)
    {
        IsChecked = isChecked;
        return this;
    }

    /// <summary>
    /// 链式调用：设置变化回调
    /// </summary>
    public Checkbox SetOnChanged(Action<bool> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置禁用状态
    /// </summary>
    public Checkbox SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        return this;
    }

    /// <summary>
    /// 链式调用：设置三态模式
    /// </summary>
    public Checkbox SetThreeState(bool isThreeState)
    {
        IsThreeState = isThreeState;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public Checkbox SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }
}
