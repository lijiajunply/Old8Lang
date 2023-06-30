using System.IO.Compression;
using System.Text;

namespace Old8LangLib;

public static class FileLib
{
    public static string FileRead(string path) => File.ReadAllText(path);
    public static string[] FileReadLines(string path) => File.ReadLines(path).ToArray();
    public static void CopyFile(string filepath, string copyPath) => File.Copy(filepath, copyPath, true);

    public static void UnpackZip(string zipPath, string newPath) =>
        ZipFile.ExtractToDirectory(newPath, zipPath, Encoding.UTF8, true);

    public static void CompressZip(string filePath, string zipPath) => ZipFile.CreateFromDirectory(filePath, zipPath);

    public static List<string> ZipReadAll(string zipPath) =>
        ZipFile.OpenRead(zipPath).Entries.Select(s => s.Name).ToList();
}