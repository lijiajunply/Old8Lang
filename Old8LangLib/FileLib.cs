using System.IO.Compression;
using System.Text;
using System.Xml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Old8LangLib;

/// <summary>
/// 文件操作库，提供丰富的文件和目录操作功能
/// </summary>
public static class FileLib
{
    /// <summary>
    /// 读取文件内容为字符串
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <returns>文件内容字符串</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static string FileRead(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return File.ReadAllText(path, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// 读取文件内容为字符串数组，每行为一个元素
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <returns>文件内容字符串数组</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static string[] FileReadLines(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return [.. File.ReadLines(path, encoding ?? Encoding.UTF8)];
    }

    /// <summary>
    /// 将字符串内容写入文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="content">要写入的内容</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void FileWrite(string path, string content, Encoding? encoding = null)
    {
        File.WriteAllText(path, content, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// 将字符串数组写入文件，每行一个元素
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="lines">要写入的字符串数组</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void FileWriteLines(string path, string[] lines, Encoding? encoding = null)
    {
        File.WriteAllLines(path, lines, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// 将字符串内容追加到文件末尾
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="content">要追加的内容</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void FileAppend(string path, string content, Encoding? encoding = null)
    {
        File.AppendAllText(path, content, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// 将字符串数组追加到文件末尾，每行一个元素
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="lines">要追加的字符串数组</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void FileAppendLines(string path, string[] lines, Encoding? encoding = null)
    {
        File.AppendAllLines(path, lines, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// 读取文件内容为字节数组
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件内容字节数组</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static byte[] ReadAllBytes(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// 将字节数组写入文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="bytes">要写入的字节数组</param>
    public static void WriteAllBytes(string path, byte[] bytes)
    {
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>
    /// 将字节数组追加到文件末尾
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="bytes">要追加的字节数组</param>
    public static void AppendAllBytes(string path, byte[] bytes)
    {
        File.AppendAllBytes(path, bytes);
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="filepath">源文件路径</param>
    /// <param name="copyPath">目标文件路径</param>
    /// <exception cref="FileNotFoundException">当源文件不存在时抛出</exception>
    public static void CopyFile(string filepath, string copyPath)
    {
        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"源文件不存在: '{filepath}'", filepath);
        }

        File.Copy(filepath, copyPath, true);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static void DeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        File.Delete(path);
    }

    /// <summary>
    /// 重命名文件
    /// </summary>
    /// <param name="oldPath">旧文件路径</param>
    /// <param name="newPath">新文件路径</param>
    /// <exception cref="FileNotFoundException">当旧文件不存在时抛出</exception>
    public static void RenameFile(string oldPath, string newPath)
    {
        if (!File.Exists(oldPath))
        {
            throw new FileNotFoundException($"文件不存在: '{oldPath}'", oldPath);
        }

        File.Move(oldPath, newPath, true);
    }

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件是否存在</returns>
    public static bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// 创建目录，如果目录已存在则不执行任何操作
    /// </summary>
    /// <param name="path">目录路径</param>
    public static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <summary>
    /// 删除目录
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="recursive">是否递归删除子目录和文件</param>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static void DeleteDirectory(string path, bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{path}'");
        }

        Directory.Delete(path, recursive);
    }

    /// <summary>
    /// 检查目录是否存在
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>目录是否存在</returns>
    public static bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <summary>
    /// 获取目录信息
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>目录信息字符串，包含名称、路径、创建时间、修改时间、访问时间和只读属性</returns>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static string GetDirectoryInfo(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{path}'");
        }

        var dirInfo = new DirectoryInfo(path);
        return $"目录名: {dirInfo.Name}\n" +
               $"完整路径: {dirInfo.FullName}\n" +
               $"创建时间: {dirInfo.CreationTime}\n" +
               $"修改时间: {dirInfo.LastWriteTime}\n" +
               $"访问时间: {dirInfo.LastAccessTime}\n" +
               $"是否只读: {dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly)}";
    }

    /// <summary>
    /// 获取指定目录下的子目录列表
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="searchPattern">搜索模式，默认为"*"</param>
    /// <param name="searchOption">搜索选项，默认为TopDirectoryOnly</param>
    /// <returns>子目录路径数组</returns>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static string[] GetDirectories(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{path}'");
        }

        return Directory.GetDirectories(path, searchPattern, searchOption);
    }

    /// <summary>
    /// 获取指定目录下的文件列表
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="searchPattern">搜索模式，默认为"*"</param>
    /// <param name="searchOption">搜索选项，默认为TopDirectoryOnly</param>
    /// <returns>文件路径数组</returns>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static string[] GetFiles(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{path}'");
        }

        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    /// <summary>
    /// 移动目录
    /// </summary>
    /// <param name="sourceDirName">源目录路径</param>
    /// <param name="destDirName">目标目录路径</param>
    /// <exception cref="DirectoryNotFoundException">当源目录不存在时抛出</exception>
    public static void MoveDirectory(string sourceDirName, string destDirName)
    {
        if (!Directory.Exists(sourceDirName))
        {
            throw new DirectoryNotFoundException($"源目录不存在: '{sourceDirName}'");
        }

        Directory.Move(sourceDirName, destDirName);
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件信息字符串，包含名称、路径、大小、创建时间、修改时间、访问时间和只读属性</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static string GetFileInfo(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        var fileInfo = new FileInfo(path);
        return $"文件名: {fileInfo.Name}\n" +
               $"完整路径: {fileInfo.FullName}\n" +
               $"大小: {fileInfo.Length} 字节\n" +
               $"创建时间: {fileInfo.CreationTime}\n" +
               $"修改时间: {fileInfo.LastWriteTime}\n" +
               $"访问时间: {fileInfo.LastAccessTime}\n" +
               $"是否只读: {fileInfo.IsReadOnly}";
    }

    /// <summary>
    /// 解压ZIP文件
    /// </summary>
    /// <param name="zipPath">ZIP文件路径</param>
    /// <param name="newPath">解压目标目录</param>
    /// <exception cref="FileNotFoundException">当ZIP文件不存在时抛出</exception>
    public static void UnpackZip(string zipPath, string newPath)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"ZIP文件不存在: '{zipPath}'", zipPath);
        }

        Directory.CreateDirectory(newPath);
        ZipFile.ExtractToDirectory(zipPath, newPath, Encoding.UTF8, true);
    }

    /// <summary>
    /// 将目录压缩为ZIP文件
    /// </summary>
    /// <param name="filePath">要压缩的目录路径</param>
    /// <param name="zipPath">生成的ZIP文件路径</param>
    /// <param name="compressionLevel">压缩级别，默认为Optimal</param>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static void CompressZip(string filePath, string zipPath,
        CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        if (!Directory.Exists(filePath))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{filePath}'");
        }

        ZipFile.CreateFromDirectory(filePath, zipPath, compressionLevel, false);
    }

    /// <summary>
    /// 读取ZIP文件中的所有条目名称
    /// </summary>
    /// <param name="zipPath">ZIP文件路径</param>
    /// <param name="includeFullPaths">是否包含完整路径，默认为false</param>
    /// <returns>ZIP文件条目名称列表</returns>
    /// <exception cref="FileNotFoundException">当ZIP文件不存在时抛出</exception>
    public static List<string> ZipReadAll(string zipPath, bool includeFullPaths = false)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"ZIP文件不存在: '{zipPath}'", zipPath);
        }

        using var archive = ZipFile.OpenRead(zipPath);
        if (includeFullPaths)
        {
            return archive.Entries.Select(e => e.FullName).ToList();
        }

        return archive.Entries.Select(e => e.Name).ToList();
    }

    /// <summary>
    /// 读取并格式化XML文件
    /// </summary>
    /// <param name="path">XML文件路径</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <returns>格式化后的XML字符串</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    /// <exception cref="XmlException">当XML文件格式错误时抛出</exception>
    public static string ReadXml(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        try
        {
            var doc = new XmlDocument();
            using var reader = new StreamReader(path, encoding ?? Encoding.UTF8);
            doc.Load(reader);
            var stringWriter = new StringWriter();
            var xmlWriter = XmlWriter.Create(stringWriter,
                new XmlWriterSettings { Indent = true, Encoding = encoding ?? Encoding.UTF8 });
            doc.WriteTo(xmlWriter);
            xmlWriter.Close();
            return stringWriter.ToString();
        }
        catch (Exception ex)
        {
            throw new XmlException($"读取XML文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将XML内容写入文件
    /// </summary>
    /// <param name="path">XML文件路径</param>
    /// <param name="xmlContent">XML内容字符串</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <exception cref="XmlException">当XML内容格式错误时抛出</exception>
    public static void WriteXml(string path, string xmlContent, Encoding? encoding = null)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            using var writer = new StreamWriter(path, false, encoding ?? Encoding.UTF8);
            doc.Save(writer);
        }
        catch (Exception ex)
        {
            throw new XmlException($"写入XML文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 读取YAML文件并反序列化为动态对象
    /// </summary>
    /// <param name="path">YAML文件路径</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <returns>反序列化后的动态对象</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    /// <exception cref="InvalidOperationException">当YAML文件格式错误时抛出</exception>
    public static dynamic ReadYaml(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        try
        {
            var yamlContent = File.ReadAllText(path, encoding ?? Encoding.UTF8);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<dynamic>(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"读取YAML文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将动态对象序列化为YAML并写入文件
    /// </summary>
    /// <param name="path">YAML文件路径</param>
    /// <param name="data">要序列化的动态对象</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <exception cref="InvalidOperationException">当序列化失败时抛出</exception>
    public static void WriteYaml(string path, dynamic data, Encoding? encoding = null)
    {
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yamlContent = serializer.Serialize(data);
            File.WriteAllText(path, yamlContent, encoding ?? Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"写入YAML文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取文件大小（字节）
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件大小，单位为字节</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static long GetFileSize(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return new FileInfo(path).Length;
    }

    /// <summary>
    /// 获取文件最后修改时间
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件最后修改时间</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static DateTime GetLastWriteTime(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return File.GetLastWriteTime(path);
    }

    /// <summary>
    /// 设置文件最后修改时间
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="lastWriteTime">要设置的最后修改时间</param>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static void SetLastWriteTime(string path, DateTime lastWriteTime)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        File.SetLastWriteTime(path, lastWriteTime);
    }
}