// ReSharper disable once RedundantUsingDirective

using System.Reflection;
using System.Text;
using System.Text.Json;
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
        if (a == null)
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
        if (a == null)
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
    /// 修改语言基本信息并保存到配置文件
    /// </summary>
    /// <param name="import">导入路径</param>
    /// <param name="ver">语言版本</param>
    /// <param name="uri">语言官方网站URL，默认为 https://downland.old8lang.com</param>
    /// <returns>更新后的语言信息对象</returns>
    public static LangInfo ChangeBasicInfo(string import, string ver, string uri = "https://downland.old8lang.com")
    {
        var langInfo = new LangInfo { ImportPath = import, Var = ver, Url = uri };
        var jsonString = JsonSerializer.Serialize(langInfo);
        File.WriteAllText(JsonPath, jsonString);
        return langInfo;
    }

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
        LangInfo langInfo;

        // 直接测试Old8Lang目录下的LangInfo.json文件
        var directJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Old8Lang", "LangInfo.json");
        if (File.Exists(directJsonPath))
        {
            var jsonString = File.ReadAllText(directJsonPath, Encoding.UTF8);
            langInfo = JsonSerializer.Deserialize<LangInfo>(jsonString)!;
        }
        else if (File.Exists(JsonPath))
        {
            var jsonString = File.ReadAllText(JsonPath, Encoding.UTF8);
            langInfo = JsonSerializer.Deserialize<LangInfo>(jsonString)!;
        }
        else
        {
            // 如果文件不存在，创建一个空的 LangInfo 对象
            // 不再自动填充默认库，因为标准库现在由 StandardLibraryRegistry 管理
            langInfo = new LangInfo { Var = "1.0.0", Url = "https://downland.old8lang.com" };
        }

        // 不再自动添加默认库信息，标准库由 StandardLibraryRegistry 管理
        // 保留空的 LibInfos 用于用户自定义库（向后兼容）

        if (Directory.Exists(langInfo.ImportPath)) return langInfo;
        var s = Path.GetDirectoryName(CodePath);
#if RELEASE
        s = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif

        // 尝试使用绝对路径
        var absoluteImportPath = Path.Combine(Directory.GetCurrentDirectory(), "Old8LangLib", "OldLib");
        langInfo.ImportPath = Directory.Exists(absoluteImportPath)
            ? absoluteImportPath
            : Path.Combine(s ?? "", "Old8LangLib", "OldLib");

        return langInfo;
    }

    /// <summary>
    /// 安装导入的模块
    /// </summary>
    /// <param name="context">安装上下文信息</param>
    /// <returns>安装是否成功，当前版本始终返回 false</returns>
    /// <remarks>
    /// 此方法目前未实现完整功能，仅返回 false
    /// </remarks>
    public static bool ImportInstall(string context)
    {
        if (string.IsNullOrEmpty(context)) return false;
        // 此方法功能尚未实现
        // var _ = new HttpClient();

        return false;
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

    /// <summary>
    /// 获取语言配置文件的路径
    /// </summary>
    /// <returns>配置文件的完整路径</returns>
    private static string JsonPath
    {
        get
        {
#if DEBUG
            var filename = Path.Combine(Path.GetDirectoryName(CodePath)!, "Old8Lang", "LangInfo.json");
            if (filename.StartsWith("Users/") || filename.StartsWith("Volumes/"))
            {
                filename = "/" + filename;
            }

            return filename;
#else
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "LangInfo.json");
#endif
        }
    }
}