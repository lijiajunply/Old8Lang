using System.IO.Compression;
using System.Text;
using System.Xml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Old8LangLib;

public static class FileLib
{
    public static string FileRead(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return File.ReadAllText(path, encoding ?? Encoding.UTF8);
    }

    public static string[] FileReadLines(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return [.. File.ReadLines(path, encoding ?? Encoding.UTF8)];
    }

    public static void FileWrite(string path, string content, Encoding? encoding = null)
    {
        File.WriteAllText(path, content, encoding ?? Encoding.UTF8);
    }

    public static void FileWriteLines(string path, string[] lines, Encoding? encoding = null)
    {
        File.WriteAllLines(path, lines, encoding ?? Encoding.UTF8);
    }

    public static void FileAppend(string path, string content, Encoding? encoding = null)
    {
        File.AppendAllText(path, content, encoding ?? Encoding.UTF8);
    }

    public static void FileAppendLines(string path, string[] lines, Encoding? encoding = null)
    {
        File.AppendAllLines(path, lines, encoding ?? Encoding.UTF8);
    }

    public static byte[] ReadAllBytes(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return File.ReadAllBytes(path);
    }

    public static void WriteAllBytes(string path, byte[] bytes)
    {
        File.WriteAllBytes(path, bytes);
    }

    public static void AppendAllBytes(string path, byte[] bytes)
    {
        File.AppendAllBytes(path, bytes);
    }

    public static void CopyFile(string filepath, string copyPath)
    {
        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"源文件不存在: '{filepath}'", filepath);
        }

        File.Copy(filepath, copyPath, true);
    }

    public static void DeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        File.Delete(path);
    }

    public static void RenameFile(string oldPath, string newPath)
    {
        if (!File.Exists(oldPath))
        {
            throw new FileNotFoundException($"文件不存在: '{oldPath}'", oldPath);
        }

        File.Move(oldPath, newPath, true);
    }

    public static bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public static void DeleteDirectory(string path, bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{path}'");
        }

        Directory.Delete(path, recursive);
    }

    public static bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

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

    public static string[] GetDirectories(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{path}'");
        }

        return Directory.GetDirectories(path, searchPattern, searchOption);
    }

    public static string[] GetFiles(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{path}'");
        }

        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    public static void MoveDirectory(string sourceDirName, string destDirName)
    {
        if (!Directory.Exists(sourceDirName))
        {
            throw new DirectoryNotFoundException($"源目录不存在: '{sourceDirName}'");
        }

        Directory.Move(sourceDirName, destDirName);
    }

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

    public static void UnpackZip(string zipPath, string newPath)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"ZIP文件不存在: '{zipPath}'", zipPath);
        }

        Directory.CreateDirectory(newPath);
        ZipFile.ExtractToDirectory(zipPath, newPath, Encoding.UTF8, true);
    }

    public static void CompressZip(string filePath, string zipPath,
        CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        if (!Directory.Exists(filePath))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{filePath}'");
        }

        ZipFile.CreateFromDirectory(filePath, zipPath, compressionLevel, false);
    }

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

    public static long GetFileSize(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return new FileInfo(path).Length;
    }

    public static DateTime GetLastWriteTime(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        return File.GetLastWriteTime(path);
    }

    public static void SetLastWriteTime(string path, DateTime lastWriteTime)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }

        File.SetLastWriteTime(path, lastWriteTime);
    }
}