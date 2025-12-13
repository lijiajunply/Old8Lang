using System.Diagnostics;
using System.Globalization;

namespace Old8LangLib;

/// <summary>
/// 时间处理模块，用于各种时间操作
/// </summary>
public static class Time
{
    private static readonly Stopwatch Sw = Stopwatch.StartNew();

    // ========== 时间获取 ==========

    /// <summary>
    /// 获取当前时间（本地时间）
    /// </summary>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string GetLocalTime(string? format = null)
    {
        DateTime now = DateTime.Now;
        return string.IsNullOrEmpty(format) ? now.ToString(CultureInfo.InvariantCulture) : now.ToString(format);
    }

    /// <summary>
    /// 获取当前UTC时间
    /// </summary>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string GetUtcTime(string? format = null)
    {
        DateTime now = DateTime.UtcNow;
        return string.IsNullOrEmpty(format) ? now.ToString(CultureInfo.InvariantCulture) : now.ToString(format);
    }

    /// <summary>
    /// 获取指定时区的当前时间
    /// </summary>
    /// <param name="timeZoneId">时区ID，例如："Asia/Shanghai", "America/New_York", "Europe/London"</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string GetTimeInTimeZone(string timeZoneId, string? format = null)
    {
        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset timeInZone = TimeZoneInfo.ConvertTime(now, timeZone);
            return string.IsNullOrEmpty(format) ? timeInZone.ToString() : timeInZone.ToString(format);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ArgumentException($"无效的时区ID: {timeZoneId}", nameof(timeZoneId), ex);
        }
        catch (InvalidTimeZoneException ex)
        {
            throw new ArgumentException($"无效的时区: {timeZoneId}", nameof(timeZoneId), ex);
        }
    }

    // ========== 时间转换 ==========

    /// <summary>
    /// 将本地时间转换为UTC时间
    /// </summary>
    /// <param name="localTime">本地时间</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的UTC时间字符串</returns>
    public static string LocalToUtc(DateTime localTime, string? format = null)
    {
        DateTime utcTime = localTime.ToUniversalTime();
        return string.IsNullOrEmpty(format) ? utcTime.ToString(CultureInfo.InvariantCulture) : utcTime.ToString(format);
    }

    /// <summary>
    /// 将UTC时间转换为本地时间
    /// </summary>
    /// <param name="utcTime">UTC时间</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的本地时间字符串</returns>
    public static string UtcToLocal(DateTime utcTime, string? format = null)
    {
        DateTime localTime = utcTime.ToLocalTime();
        return string.IsNullOrEmpty(format)
            ? localTime.ToString(CultureInfo.InvariantCulture)
            : localTime.ToString(format);
    }

    /// <summary>
    /// 在不同时区之间转换时间
    /// </summary>
    /// <param name="time">输入时间</param>
    /// <param name="fromTimeZoneId">源时区ID</param>
    /// <param name="toTimeZoneId">目标时区ID</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的目标时区时间字符串</returns>
    public static string ConvertTimeBetweenTimeZones(DateTime time, string fromTimeZoneId, string toTimeZoneId,
        string? format = null)
    {
        try
        {
            var fromTimeZone = TimeZoneInfo.FindSystemTimeZoneById(fromTimeZoneId);
            var toTimeZone = TimeZoneInfo.FindSystemTimeZoneById(toTimeZoneId);

            var fromTime = new DateTimeOffset(time, fromTimeZone.GetUtcOffset(time));
            var toTime = TimeZoneInfo.ConvertTime(fromTime, toTimeZone);

            return string.IsNullOrEmpty(format) ? toTime.ToString() : toTime.ToString(format);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ArgumentException($"无效的时区ID", ex);
        }
        catch (InvalidTimeZoneException ex)
        {
            throw new ArgumentException($"无效的时区", ex);
        }
    }

    // ========== 时间戳 ==========

    /// <summary>
    /// 获取当前Unix时间戳（秒）
    /// </summary>
    /// <returns>Unix时间戳（秒）</returns>
    public static long GetUnixTimeSeconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 获取当前Unix时间戳（毫秒）
    /// </summary>
    /// <returns>Unix时间戳（毫秒）</returns>
    public static long GetUnixTimeMilliseconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 将Unix时间戳（秒）转换为DateTime
    /// </summary>
    /// <param name="seconds">Unix时间戳（秒）</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string FromUnixTimeSeconds(long seconds, string? format = null)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return string.IsNullOrEmpty(format) ? dateTimeOffset.ToString() : dateTimeOffset.ToString(format);
    }

    /// <summary>
    /// 将Unix时间戳（毫秒）转换为DateTime
    /// </summary>
    /// <param name="milliseconds">Unix时间戳（毫秒）</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string FromUnixTimeMilliseconds(long milliseconds, string? format = null)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        return string.IsNullOrEmpty(format) ? dateTimeOffset.ToString() : dateTimeOffset.ToString(format);
    }

    // ========== 时间测量 ==========

    /// <summary>
    /// 开始测量时间
    /// </summary>
    public static void StartTimer()
    {
        Sw.Restart();
    }

    /// <summary>
    /// 停止测量时间
    /// </summary>
    /// <returns>经过的毫秒数</returns>
    public static double StopTimer()
    {
        Sw.Stop();
        return Sw.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// 重置计时器
    /// </summary>
    public static void ResetTimer()
    {
        Sw.Reset();
    }

    /// <summary>
    /// 获取经过的毫秒数，不停止计时器
    /// </summary>
    /// <returns>经过的毫秒数</returns>
    public static double GetElapsedMilliseconds()
    {
        return Sw.Elapsed.TotalMilliseconds;
    }

    // ========== 时间格式化 ==========

    /// <summary>
    /// 获取常用时间格式
    /// </summary>
    /// <returns>常用时间格式数组</returns>
    public static string[] GetCommonFormats()
    {
        return
        [
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "HH:mm:ss",
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd HH:mm:ss.fff",
            "ddd, dd MMM yyyy HH:mm:ss GMT"
        ];
    }

    /// <summary>
    /// 格式化时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="format">时间格式字符串</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string Format(DateTime dateTime, string format)
    {
        if (string.IsNullOrEmpty(format))
        {
            throw new ArgumentNullException(nameof(format), "时间格式不能为空");
        }

        return dateTime.ToString(format);
    }

    // ========== 时间操作 ==========

    /// <summary>
    /// 添加天数
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="days">天数</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string AddDays(DateTime dateTime, int days, string? format = null)
    {
        var result = dateTime.AddDays(days);
        return string.IsNullOrEmpty(format) ? result.ToString(CultureInfo.InvariantCulture) : result.ToString(format);
    }

    /// <summary>
    /// 添加小时
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="hours">小时数</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string AddHours(DateTime dateTime, int hours, string? format = null)
    {
        var result = dateTime.AddHours(hours);
        return string.IsNullOrEmpty(format) ? result.ToString(CultureInfo.InvariantCulture) : result.ToString(format);
    }

    /// <summary>
    /// 添加分钟
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="minutes">分钟数</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string AddMinutes(DateTime dateTime, int minutes, string? format = null)
    {
        var result = dateTime.AddMinutes(minutes);
        return string.IsNullOrEmpty(format) ? result.ToString(CultureInfo.InvariantCulture) : result.ToString(format);
    }

    /// <summary>
    /// 添加秒
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="seconds">秒数</param>
    /// <param name="format">时间格式字符串，默认为null</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string AddSeconds(DateTime dateTime, int seconds, string? format = null)
    {
        var result = dateTime.AddSeconds(seconds);
        return string.IsNullOrEmpty(format) ? result.ToString(CultureInfo.InvariantCulture) : result.ToString(format);
    }

    // ========== 兼容性方法 ==========

    /// <summary>
    /// 获取当前时间（兼容旧版本）
    /// </summary>
    /// <param name="format">时间格式字符串</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string GetTimeNow(string format)
    {
        return GetLocalTime(format);
    }

    /// <summary>
    /// 获取常用时间格式（兼容旧版本）
    /// </summary>
    /// <returns>常用时间格式数组</returns>
    public static string[] TimeFormat()
    {
        return GetCommonFormats();
    }

    /// <summary>
    /// 开始测量时间（兼容旧版本）
    /// </summary>
    public static void TimeStart()
    {
        StartTimer();
    }

    /// <summary>
    /// 停止测量时间（兼容旧版本）
    /// </summary>
    /// <returns>经过的毫秒数</returns>
    public static double TimeStop()
    {
        return StopTimer();
    }

    /// <summary>
    /// 获取当前Unix时间戳（秒）（兼容旧版本）
    /// </summary>
    /// <returns>Unix时间戳（秒）字符串</returns>
    public static string TimeStamp()
    {
        return GetUnixTimeSeconds().ToString();
    }
}