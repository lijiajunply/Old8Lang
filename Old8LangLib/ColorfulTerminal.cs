using System.Drawing;
using Console = Colorful.Console;

namespace Old8LangLib;

/// <summary>
/// 彩色终端模块，提供彩色文本输出和ASCII艺术字功能
/// </summary>
public static class ColorfulTerminal
{
    /// <summary>
    /// 打印彩色文本
    /// </summary>
    /// <param name="context">要打印的文本内容</param>
    /// <param name="color">颜色名称，如 "Red", "Blue", "Green" 等</param>
    /// <exception cref="ArgumentException">当颜色名称无效时抛出</exception>
    public static void PrintColorful(string context,string color) => Console.Write(context,Color.FromName(color));
    
    /// <summary>
    /// 打印彩色文本并换行
    /// </summary>
    /// <param name="context">要打印的文本内容</param>
    /// <param name="color">颜色名称，如 "Red", "Blue", "Green" 等</param>
    /// <exception cref="ArgumentException">当颜色名称无效时抛出</exception>
    public static void PrintLineColorful(string context,string color) =>
        Console.WriteLine(context,Color.FromName(color));
    
    /// <summary>
    /// 打印ASCII艺术字
    /// </summary>
    /// <param name="context">要转换为ASCII艺术字的文本</param>
    public static void PrintAscii(string context) => Console.WriteAscii(context);
    
    /// <summary>
    /// 打印彩色ASCII艺术字
    /// </summary>
    /// <param name="context">要转换为ASCII艺术字的文本</param>
    /// <param name="color">颜色名称，如 "Red", "Blue", "Green" 等</param>
    /// <exception cref="ArgumentException">当颜色名称无效时抛出</exception>
    public static void PrintAsciiColorful(string context,string color) =>
        Console.WriteAscii(context,Color.FromName(color));
}