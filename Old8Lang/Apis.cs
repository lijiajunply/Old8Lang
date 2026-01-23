// ReSharper disable once RedundantUsingDirective

using System.Reflection;
using System.Text;
using Old8Lang.AST.Expression;

namespace Old8Lang;

/// <summary>
/// 提供 Old8Lang 语言的公共 API 方法，包括文件操作、路径处理、配置管理等功能
/// </summary>
public static class Apis
{
    #region 辅助函数

    /// <summary>
    /// 将列表转换为逗号分隔的字符串
    /// </summary>
    /// <typeparam name="T">列表元素的类型</typeparam>
    /// <param name="a">要转换的列表，可为 null</param>
    /// <returns>逗号分隔的字符串表示，空列表返回空字符串</returns>
    public static string ListToString<T>(List<T>? a)
    {
        if (a is null)
            return "";
        var builder = new StringBuilder();
        for (var i = 0; i < a.Count; i++)
            builder.Append(a[i] + (i == a.Count - 1 ? "" : ","));

        return builder.ToString();
    }

    /// <summary>
    /// 将数组转换为逗号分隔的字符串
    /// </summary>
    /// <typeparam name="T">数组元素的类型</typeparam>
    /// <param name="a">要转换的数组</param>
    /// <returns>逗号分隔的字符串表示</returns>
    public static string ArrayToString<T>(T[] a)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < a.Length; i++)
        {
            var b = i == 0 && a.Length == 1 || i == a.Length - 1 ? "" : ",";
            builder.Append(a[i] + b);
        }

        return builder.ToString();
    }

    /// <summary>
    /// 将 Old8Lang 值类型列表转换为 .NET 对象列表
    /// </summary>
    /// <param name="a">Old8Lang 值类型列表，可为 null</param>
    /// <returns>.NET 对象列表</returns>
    public static List<object> ListToObjects(List<LangValueType>? a)
    {
        if (a is null)
            return [];
        if (a.Count == 0)
            return [];
        return a[0] == null! ? [] : [.. a.Select(x => x.GetValue())];
    }

    #endregion

    #region 文件和目录读取

    /// <summary>
    /// 从文件读取内容，支持多种路径处理策略
    /// </summary>
    /// <param name="filename">文件名或路径</param>
    /// <returns>文件内容，或在文件不存在时返回原始文件名</returns>
    /// <remarks>
    /// 路径处理逻辑：
    /// 1. 处理 macOS 上缺少开头斜杠的绝对路径
    /// 2. 检查是否为绝对路径，直接使用
    /// 3. 相对路径尝试从当前目录查找
    /// 4. 尝试从应用程序基目录查找
    /// 5. 所有尝试失败返回原始文件名
    /// </remarks>
    public static string FromFile(string filename)
    {
        // 处理macOS上缺少开头斜杠的绝对路径
        if (filename.StartsWith("Users/") || filename.StartsWith("Volumes/"))
        {
            filename = "/" + filename;
        }

        // 如果是绝对路径，直接使用
        if (Path.IsPathFullyQualified(filename))
        {
            return File.Exists(filename) ? File.ReadAllText(filename, Encoding.UTF8) : filename;
        }

        // 如果是相对路径，尝试从当前目录或应用程序目录查找
        var fullPath = Path.GetFullPath(filename);
        if (File.Exists(fullPath))
        {
            return File.ReadAllText(fullPath, Encoding.UTF8);
        }

        // 尝试从应用程序基目录查找
        var appPath = Path.Combine(AppContext.BaseDirectory, filename);
        if (File.Exists(appPath))
        {
            return File.ReadAllText(appPath, Encoding.UTF8);
        }

        // 所有尝试都失败，返回原始文件名
        return filename;
    }

    /// <summary>
    /// 从目录读取 init.old8 文件的内容
    /// </summary>
    /// <param name="directoryName">目录名称</param>
    /// <returns>init.old8 文件的内容</returns>
    public static string FromDirectory(string directoryName)
    {
        var builder = new StringBuilder();
        builder.Append(FromFile(directoryName + "/" + "init.old8"));
        return builder.ToString();
    }

    #endregion

    /// <summary>
    /// 读取语言配置信息
    /// </summary>
    /// <returns>语言信息对象</returns>
    /// <remarks>
    ///
    /// 配置文件查找顺序：
    /// 1. 当前目录下的 Old8Lang/LangInfo.json
    /// 2. 默认的 JsonPath 位置
    /// 3. 如果文件不存在，创建默认配置（空 LibInfos）
    ///
    /// 注意：标准库（OS、File、Terminal 等）现在通过 StandardLibraryRegistry 管理，
    /// 不再需要 LangInfo.json 配置
    /// </remarks>
    public static LangInfo ReadJson()
    {
        return new LangInfo();
    }

    /// <summary>
    /// 获取代码根目录路径
    /// </summary>
    /// <returns>代码根目录路径</returns>
    /// <remarks>
    /// 调试模式下返回项目根目录，发布模式下返回应用程序基目录
    /// </remarks>
    public static string CodePath
    {
        get
        {
#if DEBUG
            var directory = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar);
            var slice = new ArraySegment<string>(directory, 0, directory.Length - 4);
            return Path.Combine([.. slice]);
#else
            // 返回程序运行时目录或其他合理默认路径
            return AppContext.BaseDirectory;
#endif
        }
    }
}