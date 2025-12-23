using System.Runtime.Versioning;

namespace Old8LangLib;

/// <summary>
/// 终端操作模块，提供控制台交互功能
/// </summary>
public static class Terminal
{
    /// <summary>
    /// 设置控制台标题
    /// </summary>
    /// <param name="title">新的控制台标题</param>
    public static void Title(string title) => Console.Title = title;
    
    /// <summary>
    /// 读取单个ASCII字符
    /// </summary>
    /// <returns>读取到的ASCII字符的整数表示</returns>
    public static int ReadAscii() => Console.Read();
    
    /// <summary>
    /// 读取单个按键
    /// </summary>
    /// <returns>读取到的按键的字符串表示</returns>
    public static string ReadKey() => Console.ReadKey().Key.ToString();
    
    /// <summary>
    /// 发出默认蜂鸣声
    /// </summary>
    public static void Beep() => Console.Beep();
    
    /// <summary>
    /// 清屏
    /// </summary>
    public static void Clear() => Console.Clear();

    /// <summary>
    /// 在Windows平台上发出特定频率和时长的蜂鸣声
    /// </summary>
    /// <param name="tone">音调名称</param>
    /// <param name="duration">时长名称</param>
    /// <exception cref="ArgumentException">当音调或时长名称无效时抛出</exception>
    [SupportedOSPlatform("windows")]
    public static void BeepWindow(string tone, string duration) =>
        Console.Beep((int)Enum.Parse<Tone>(tone), (int)Enum.Parse<Duration>(duration));

    // ReSharper disable UnusedMember.Local
    /// <summary>
    /// 表示不同音调的枚举
    /// </summary>
    private enum Tone
    {
        /// <summary>静音</summary>
        REST = 0,
        /// <summary>G音（C下方），频率196Hz</summary>
        GBelowC = 196,
        /// <summary>A音，频率220Hz</summary>
        A = 220,
        /// <summary>A#音，频率233Hz</summary>
        ASharp = 233,
        /// <summary>B音，频率247Hz</summary>
        B = 247,
        /// <summary>C音，频率262Hz</summary>
        C = 262,
        /// <summary>C#音，频率277Hz</summary>
        Csharp = 277,
        /// <summary>D音，频率294Hz</summary>
        D = 294,
        /// <summary>D#音，频率311Hz</summary>
        DSharp = 311,
        /// <summary>E音，频率330Hz</summary>
        E = 330,
        /// <summary>F音，频率349Hz</summary>
        F = 349,
        /// <summary>F#音，频率370Hz</summary>
        Fsharp = 370,
        /// <summary>G音，频率392Hz</summary>
        G = 392,
        /// <summary>G#音，频率415Hz</summary>
        GSharp = 415,
    }

    /// <summary>
    /// 表示音符时长的枚举，单位为毫秒
    /// </summary>
    private enum Duration
    {
        /// <summary>全音符，1600毫秒</summary>
        WHOLE = 1600,
        /// <summary>二分音符，800毫秒</summary>
        HALF = WHOLE / 2,
        /// <summary>四分音符，400毫秒</summary>
        QUARTER = HALF / 2,
        /// <summary>八分音符，200毫秒</summary>
        EIGHTH = QUARTER / 2,
        /// <summary>十六分音符，100毫秒</summary>
        SIXTEENTH = EIGHTH / 2,
    }
}