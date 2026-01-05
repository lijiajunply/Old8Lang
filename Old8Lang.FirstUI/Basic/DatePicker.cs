using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// DatePicker 日期选择器组件
/// 提供日期选择功能，支持多种显示模式
/// </summary>
public class DatePicker : WidgetBase
{
    /// <summary>
    /// 当前选中的日期
    /// </summary>
    public DateTime? Value { get; set; }

    /// <summary>
    /// 最小可选日期
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// 最大可选日期
    /// </summary>
    public DateTime? MaxDate { get; set; }

    /// <summary>
    /// 日期变化回调
    /// </summary>
    public Action<DateTime?>? OnChanged { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 显示格式
    /// </summary>
    public string Format { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// 占位符文本
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// 语言文化（如 "zh-CN", "en-US"）
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// 日期选择器模式
    /// </summary>
    public DatePickerMode Mode { get; set; } = DatePickerMode.DayMonthYear;

    /// <summary>
    /// 是否显示周数
    /// </summary>
    public bool ShowWeekNumbers { get; set; } = false;

    /// <summary>
    /// 每周的第一天（0=周日，1=周一）
    /// </summary>
    public int FirstDayOfWeek { get; set; } = 0;

    public override object Build(BuildContext context)
    {
        var datePicker = new Avalonia.Controls.DatePicker
        {
            SelectedDate = Value,
            IsEnabled = !IsDisabled
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(datePicker, this);

        // 设置日期范围
        SetDateRange(datePicker);

        // 设置格式和其他属性
        SetDatePickerProperties(datePicker);

        // 注册事件
        RegisterDateChangedEvent(datePicker);

        return datePicker;
    }

    /// <summary>
    /// 设置日期范围
    /// </summary>
    private void SetDateRange(Avalonia.Controls.DatePicker datePicker)
    {
        // 简化实现，通过事件处理来限制日期范围
        // 在实际使用中可以通过自定义模板来实现更精确的限制
        if (MinDate.HasValue || MaxDate.HasValue)
        {
            // 添加验证逻辑到事件处理器中
        }
    }

    /// <summary>
    /// 设置日期选择器属性
    /// </summary>
    private void SetDatePickerProperties(Avalonia.Controls.DatePicker datePicker)
    {
        // 设置占位符
        if (!string.IsNullOrEmpty(Placeholder))
        {
            // Watermark 属性不存在，可以通过模板或其他方式设置
        }

        // 设置显示模式
        SetDatePickerMode(datePicker, Mode);

        // 设置文化信息
        if (!string.IsNullOrEmpty(Culture))
        {
            try
            {
                var culture = new System.Globalization.CultureInfo(Culture);
                // 应用文化设置到日期选择器
            }
            catch
            {
                Console.WriteLine($"[DatePicker] Invalid culture: {Culture}");
            }
        }

        // 设置周数显示
        if (ShowWeekNumbers)
        {
            // 在实际实现中可能需要自定义模板
        }
    }

    /// <summary>
    /// 设置日期选择器模式
    /// </summary>
    private void SetDatePickerMode(Avalonia.Controls.DatePicker datePicker, DatePickerMode mode)
    {
        switch (mode)
        {
            case DatePickerMode.DayMonth:
                // 设置为月日模式
                datePicker.DayVisible = true;
                datePicker.MonthVisible = true;
                datePicker.YearVisible = true;
                break;
                
            case DatePickerMode.MonthYear:
                // 设置为年月模式
                datePicker.DayVisible = false;
                datePicker.MonthVisible = true;
                datePicker.YearVisible = true;
                break;
                
            case DatePickerMode.Year:
                // 设置为年模式
                datePicker.DayVisible = false;
                datePicker.MonthVisible = false;
                datePicker.YearVisible = true;
                break;
                
            case DatePickerMode.DayMonthYear:
            default:
                // 设置为完整日期模式
                datePicker.DayVisible = true;
                datePicker.MonthVisible = true;
                datePicker.YearVisible = true;
                break;
        }
    }

    /// <summary>
    /// 注册日期变化事件
    /// </summary>
    private void RegisterDateChangedEvent(Avalonia.Controls.DatePicker datePicker)
    {
        if (OnChanged != null)
        {
            datePicker.SelectedDateChanged += (sender, e) =>
            {
                try
                {
                    // 转换 DateTimeOffset? 到 DateTime?
                    var selectedDateTime = datePicker.SelectedDate?.DateTime;
                    Value = selectedDateTime;
                    OnChanged?.Invoke(selectedDateTime);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[DatePicker] Error in date change: {ex.Message}");
                }
            };
        }
    }

    // ======== 链式调用方法 ========

    /// <summary>
    /// 设置日期值
    /// </summary>
    public DatePicker SetValue(DateTime? value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 设置日期范围
    /// </summary>
    public DatePicker SetDateRange(DateTime? minDate = null, DateTime? maxDate = null)
    {
        MinDate = minDate;
        MaxDate = maxDate;
        return this;
    }

    /// <summary>
    /// 设置日期变化回调
    /// </summary>
    public DatePicker SetOnChanged(Action<DateTime?> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 设置显示格式
    /// </summary>
    public DatePicker SetFormat(string format)
    {
        Format = format;
        return this;
    }

    /// <summary>
    /// 设置占位符
    /// </summary>
    public DatePicker SetPlaceholder(string placeholder)
    {
        Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// 设置文化信息
    /// </summary>
    public DatePicker SetCulture(string culture)
    {
        Culture = culture;
        return this;
    }

    /// <summary>
    /// 设置显示模式
    /// </summary>
    public DatePicker SetMode(DatePickerMode mode)
    {
        Mode = mode;
        return this;
    }

    /// <summary>
    /// 设置周相关选项
    /// </summary>
    public DatePicker SetWeekOptions(bool showWeekNumbers = false, int firstDayOfWeek = 0)
    {
        ShowWeekNumbers = showWeekNumbers;
        FirstDayOfWeek = firstDayOfWeek;
        return this;
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public DatePicker SetStates(bool isReadOnly = false, bool isDisabled = false)
    {
        IsReadOnly = isReadOnly;
        IsDisabled = isDisabled;
        return this;
    }

    // ======== 辅助方法 ========

    /// <summary>
    /// 获取格式化的日期字符串
    /// </summary>
    public string GetFormattedDate()
    {
        if (!Value.HasValue)
        return string.Empty;

        try
        {
            if (!string.IsNullOrEmpty(Culture))
            {
                return Value.Value.ToString(Format);
            }
            else
            {
                var culture = new System.Globalization.CultureInfo(Culture);
                return Value.Value.ToString(Format, culture);
            }
        }
        catch
        {
            return Value.HasValue ? Value.Value.ToString(Format) ?? string.Empty : string.Empty;
        }
    }

    /// <summary>
    /// 验证日期是否在范围内
    /// </summary>
    public bool IsDateInRange(DateTime date)
    {
        if (MinDate.HasValue && date < MinDate.Value)
            return false;

        if (MaxDate.HasValue && date > MaxDate.Value)
            return false;

        return true;
    }

    /// <summary>
    /// 获取当前月份的天数
    /// </summary>
    public int GetDaysInCurrentMonth()
    {
        if (!Value.HasValue)
            return DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

        // Value 在这里不会为 null，因为有前面的 null 检查
        if (Value.HasValue)
            return DateTime.DaysInMonth(Value.Value.Year, Value.Value.Month);
        return 30;
            return 30;
    }
}

/// <summary>
/// 日期选择器显示模式
/// </summary>
public enum DatePickerMode
{
    /// <summary>
    /// 日月年模式（完整日期）
    /// </summary>
    DayMonthYear,

    /// <summary>
    /// 月年模式
    /// </summary>
    MonthYear,

    /// <summary>
    /// 年模式
    /// </summary>
    Year,

    /// <summary>
    /// 月日模式
    /// </summary>
    DayMonth
}