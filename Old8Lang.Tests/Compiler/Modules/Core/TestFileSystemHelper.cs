namespace Old8Lang.Tests.Compiler.Modules.Core;

/// <summary>
/// 文件系统测试助手类
/// </summary>
[Collection("Sequential")]
public class TestFileSystemHelper(string testFilesDirectory)
{
    /// <summary>
    /// 创建测试模块目录结构
    /// </summary>
    public void CreateModuleStructure(string moduleName, Dictionary<string, string> files)
    {
        var moduleDir = Path.Combine(testFilesDirectory, moduleName);
        Directory.CreateDirectory(moduleDir);

        foreach (var file in files)
        {
            var filePath = Path.Combine(moduleDir, file.Key);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, file.Value);
        }
    }

    /// <summary>
    /// 创建嵌套模块结构
    /// </summary>
    public void CreateNestedModuleStructure(string basePath, Dictionary<string, Dictionary<string, string>> modules)
    {
        var rootDir = Path.Combine(testFilesDirectory, basePath);
        Directory.CreateDirectory(rootDir);

        foreach (var module in modules)
        {
            var moduleDir = Path.Combine(rootDir, module.Key);
            Directory.CreateDirectory(moduleDir);

            foreach (var file in module.Value)
            {
                var filePath = Path.Combine(moduleDir, file.Key);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(filePath, file.Value);
            }
        }
    }

    /// <summary>
    /// 清理测试文件和目录
    /// </summary>
    public void Cleanup(string moduleName)
    {
        var moduleDir = Path.Combine(testFilesDirectory, moduleName);
        if (Directory.Exists(moduleDir))
        {
            try
            {
                Directory.Delete(moduleDir, true);
            }
            catch
            {
                // 忽略清理失败
            }
        }
    }

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    public bool FileExists(string relativePath)
    {
        var fullPath = Path.Combine(testFilesDirectory, relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// 读取文件内容
    /// </summary>
    public string ReadFile(string relativePath)
    {
        var fullPath = Path.Combine(testFilesDirectory, relativePath);
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// 写入文件内容
    /// </summary>
    public void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(testFilesDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
    }
}