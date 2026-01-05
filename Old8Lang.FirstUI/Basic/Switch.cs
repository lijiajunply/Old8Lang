using Avalonia.Controls;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// Switch 开关组件
/// 提供二进制切换功能
/// </summary>
public class Switch(bool value = false) : WidgetBase
{
    /// <summary>
    /// 开关状态
    /// </summary>
    public bool Value { get; set; } = value;

    /// <summary>
    /// 状态变化回调
    /// </summary>
    public Action<bool>? OnChanged { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 激活状态颜色
    /// </summary>
    public string? ActiveColor { get; set; }

    /// <summary>
    /// 非激活状态颜色
    /// </summary>
    public string? InactiveColor { get; set; }

    /// <summary>
    /// 滑块颜色
    /// </summary>
    public string? ThumbColor { get; set; }

    /// <summary>
    /// 开关宽度
    /// </summary>
    public double SwitchWidth { get; set; } = 50;

    /// <summary>
    /// 开关高度
    /// </summary>
    public double SwitchHeight { get; set; } = 26;

    public override object Build(BuildContext context)
    {
        var toggleSwitch = new ToggleSwitch
        {
            IsChecked = Value,
            IsEnabled = !IsDisabled
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(toggleSwitch, this);

        // 设置开关尺寸
        toggleSwitch.Width = SwitchWidth;
        toggleSwitch.Height = SwitchHeight;

        // 应用主题样式
        ApplySwitchStyles(toggleSwitch);

        // 注册状态变化事件
        if (OnChanged != null)
        {
            toggleSwitch.IsCheckedChanged += (sender, e) =>
            {
                try
                {
                    if (sender is ToggleSwitch switchControl)
                    {
                        OnChanged?.Invoke(switchControl.IsChecked ?? false);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Switch] Error in change handler: {ex.Message}");
                }
            };
        }

        return toggleSwitch;
    }

    /// <summary>
    /// 应用开关样式
    /// </summary>
    private void ApplySwitchStyles(ToggleSwitch toggleSwitch)
    {
        // 这里可以通过设置模板来自定义样式
        // 由于 ToggleSwitch 的样式定制比较复杂，我们使用简单的方式

        // 可以通过 Background 和 Foreground 来间接控制颜色
        if (!string.IsNullOrEmpty(ActiveColor))
        {
            // 当开关打开时的颜色
        }

        if (!string.IsNullOrEmpty(InactiveColor))
        {
            // 当开关关闭时的颜色
        }

        if (!string.IsNullOrEmpty(ThumbColor))
        {
            // 滑块颜色
        }
    }

    /// <summary>
    /// 链式调用：设置状态
    /// </summary>
    public Switch SetValue(bool value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 链式调用：设置状态变化回调
    /// </summary>
    public Switch SetOnChanged(Action<bool> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置禁用状态
    /// </summary>
    public Switch SetDisabled(bool disabled)
    {
        IsDisabled = disabled;
        return this;
    }

    /// <summary>
    /// 链式调用：设置激活颜色
    /// </summary>
    public Switch SetActiveColor(string color)
    {
        ActiveColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置非激活颜色
    /// </summary>
    public Switch SetInactiveColor(string color)
    {
        InactiveColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置滑块颜色
    /// </summary>
    public Switch SetThumbColor(string color)
    {
        ThumbColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置开关尺寸
    /// </summary>
    public Switch SetSize(double width, double height)
    {
        SwitchWidth = width;
        SwitchHeight = height;
        return this;
    }
}