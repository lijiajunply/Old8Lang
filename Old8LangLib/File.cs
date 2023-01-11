using System.IO.Compression;
using System.Text;

namespace Old8LangLib;

public class File
{
    public static string FileRead(string path) => System.IO.File.ReadAllText(path);
    public static List<string> FileReadLines(string path) => System.IO.File.ReadLines(path).ToList();
    public static void UnpackZip(string zipPath, string newPath) => 
        ZipFile.ExtractToDirectory(newPath, zipPath, Encoding.UTF8, true);
    public static void CompressZip(string filePath,string zipPath) => ZipFile.CreateFromDirectory(filePath, zipPath);
    public static List<string> ZipReadAll(string zipPath) => ZipFile.OpenRead(zipPath).Entries.Select(s => s.Name).ToList();
}