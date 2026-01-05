using Avalonia.Controls;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// TimePicker 时间选择器组件
/// 提供时间选择功能，支持12/24小时制式
/// </summary>
public class TimePicker : WidgetBase
{
    /// <summary>
    /// 当前选中的时间
    /// </summary>
    public TimeSpan? Value { get; set; }

    /// <summary>
    /// 时间变化回调
    /// </summary>
    public Action<TimeSpan?>? OnChanged { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 时间格式（12小时制或24小时制）
    /// </summary>
    public TimeFormat Format { get; set; } = TimeFormat.Hour24;

    /// <summary>
    /// 时间选择器宽度
    /// </summary>
    public double PickerWidth { get; set; } = 280;

    /// <summary>
    /// 时间选择器高度
    /// </summary>
    public double PickerHeight { get; set; } = 320;

    public override object Build(BuildContext context)
    {
        // 简化实现：使用文本框输入时间
        var timeTextBox = new TextBox
        {
            Text = GetFormattedTime(),
            Width = PickerWidth,
            Height = PickerHeight,
            IsEnabled = !IsDisabled,
            Watermark = Format == TimeFormat.Hour24 ? "HH:mm:ss" : "h:mm:ss"
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(timeTextBox, this);

        // 注册事件
        RegisterTimeChangedEvent(timeTextBox);

        return timeTextBox;
    }

    /// <summary>
    /// 注册时间变化事件
    /// </summary>
    private void RegisterTimeChangedEvent(TextBox timeTextBox)
    {
        if (OnChanged != null)
        {
            timeTextBox.TextChanged += (sender, e) =>
            {
                try
                {
                    // 尝试解析时间文本
                    if (TimeSpan.TryParse(timeTextBox.Text, out var timeSpan))
                    {
                        Value = timeSpan;
                        OnChanged?.Invoke(timeSpan);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[TimePicker] Error in time parse: {ex.Message}");
                }
            };
        }
    }

    // ======== 链式调用方法 ========

    /// <summary>
    /// 设置时间值
    /// </summary>
    public TimePicker SetValue(TimeSpan? value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 设置时间变化回调
    /// </summary>
    public TimePicker SetOnChanged(Action<TimeSpan?> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 设置格式
    /// </summary>
    public TimePicker SetFormat(TimeFormat format)
    {
        Format = format;
        return this;
    }

    /// <summary>
    /// 设置尺寸
    /// </summary>
    public TimePicker SetSize(double width = 280, double height = 320)
    {
        PickerWidth = width;
        PickerHeight = height;
        return this;
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public TimePicker SetStates(bool isDisabled = false)
    {
        IsDisabled = isDisabled;
        return this;
    }

    // ======== 辅助方法 ========

    /// <summary>
    /// 获取格式化的时间字符串
    /// </summary>
    public string GetFormattedTime()
    {
        if (!Value.HasValue)
            return string.Empty;

        return Format switch
        {
            TimeFormat.Hour12 => Value.Value.ToString(@"h\:mm\:ss"),
            TimeFormat.Hour24 => Value.Value.ToString(@"HH\:mm\:ss"),
            _ => Value.Value.ToString(@"HH\:mm\:ss")
        };
    }

    /// <summary>
    /// 从小时和分钟创建 TimeSpan
    /// </summary>
    public static TimeSpan FromTime(int hour, int minute, int second = 0)
    {
        return new TimeSpan(hour, minute, second);
    }

    /// <summary>
    /// 获取当前时间
    /// </summary>
    public static TimeSpan GetCurrentTime()
    {
        var now = DateTime.Now;
        return new TimeSpan(now.Hour, now.Minute, now.Second);
    }
}

/// <summary>
/// 时间格式
/// </summary>
public enum TimeFormat
{
    /// <summary>
    /// 12小时制
    /// </summary>
    Hour12,

    /// <summary>
    /// 24小时制
    /// </summary>
    Hour24
}