// Utils/PartialClassConverter.cs
using System.Text;
using System.Text.RegularExpressions;
using Old8Lang.CodeGen.Scanner;

namespace Old8Lang.CodeGen.Utils;

/// <summary>
/// Partial Class 转换器 - 自动将类标记为 partial
/// </summary>
public class PartialClassConverter
{
    private readonly List<AstNodeInfo> _nodes;
    private readonly bool _dryRun;

    public PartialClassConverter(List<AstNodeInfo> nodes, bool dryRun = false)
    {
        _nodes = nodes;
        _dryRun = dryRun;
    }

    /// <summary>
    /// 转换所有节点为 partial class
    /// </summary>
    public async Task<int> ConvertAll()
    {
        int convertedCount = 0;
        var fileGroups = _nodes.GroupBy(n => n.FilePath).ToList();

        Console.WriteLine($"[INFO] 需要处理 {fileGroups.Count} 个文件");

        foreach (var group in fileGroups)
        {
            var filePath = group.Key;
            var classesInFile = group.ToList();

            try
            {
                if (await ConvertFile(filePath, classesInFile))
                {
                    convertedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 处理文件失败 {filePath}: {ex.Message}");
            }
        }

        return convertedCount;
    }

    /// <summary>
    /// 转换单个文件
    /// </summary>
    private async Task<bool> ConvertFile(string filePath, List<AstNodeInfo> classesInFile)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[WARN] 文件不存在: {filePath}");
            return false;
        }

        // 读取文件内容
        var content = await File.ReadAllTextAsync(filePath);
        var originalContent = content;
        bool modified = false;

        // 为每个类添加 partial 关键字
        foreach (var nodeInfo in classesInFile)
        {
            var pattern = CreateClassPattern(nodeInfo.ClassName);
            var replacement = CreatePartialReplacement(nodeInfo.ClassName);

            var newContent = Regex.Replace(content, pattern, replacement, RegexOptions.Multiline);

            if (newContent != content)
            {
                content = newContent;
                modified = true;
                Console.WriteLine($"[INFO] 转换类: {nodeInfo.ClassName} (文件: {Path.GetFileName(filePath)})");
            }
        }

        // 如果有修改，写回文件
        if (modified)
        {
            if (_dryRun)
            {
                Console.WriteLine($"[DRY-RUN] 将修改文件: {filePath}");
                Console.WriteLine($"变更预览:");
                ShowDiff(originalContent, content, filePath);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, content);
                Console.WriteLine($"[SUCCESS] 已更新文件: {filePath}");
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// 创建类匹配模式
    /// </summary>
    private string CreateClassPattern(string className)
    {
        // 匹配 public class ClassName 或 internal class ClassName
        // 确保不匹配已经是 partial 的类
        return $@"(public|internal|private|protected)\s+(sealed\s+)?class\s+{Regex.Escape(className)}(?!\s+partial)(\s|<|\(|:)";
    }

    /// <summary>
    /// 创建 partial 替换
    /// </summary>
    private string CreatePartialReplacement(string className)
    {
        return $"$1 ${{2}}partial class {className}$3";
    }

    /// <summary>
    /// 显示差异（简化版）
    /// </summary>
    private void ShowDiff(string original, string modified, string filePath)
    {
        var originalLines = original.Split('\n');
        var modifiedLines = modified.Split('\n');

        for (int i = 0; i < Math.Min(originalLines.Length, modifiedLines.Length); i++)
        {
            if (originalLines[i] != modifiedLines[i])
            {
                Console.WriteLine($"  行 {i + 1}:");
                Console.WriteLine($"  - {originalLines[i].TrimStart()}");
                Console.WriteLine($"  + {modifiedLines[i].TrimStart()}");
            }
        }
    }
}
