using System.IO.Compression;
using System.Text;

namespace Old8LangLib;

public static class FileLib
{
    public static string FileRead(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }
        return File.ReadAllText(path);
    }

    public static string[] FileReadLines(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: '{path}'", path);
        }
        return File.ReadLines(path).ToArray();
    }

    public static void CopyFile(string filepath, string copyPath)
    {
        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"源文件不存在: '{filepath}'", filepath);
        }
        File.Copy(filepath, copyPath, true);
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

    public static void CompressZip(string filePath, string zipPath)
    {
        if (!Directory.Exists(filePath))
        {
            throw new DirectoryNotFoundException($"目录不存在: '{filePath}'");
        }
        ZipFile.CreateFromDirectory(filePath, zipPath);
    }

    public static List<string> ZipReadAll(string zipPath)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"ZIP文件不存在: '{zipPath}'", zipPath);
        }
        return ZipFile.OpenRead(zipPath).Entries.Select(s => s.Name).ToList();
    }
}