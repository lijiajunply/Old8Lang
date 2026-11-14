using System.Text;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ProjectManagement;

namespace Old8Lang.AST.Statement;

/// <summary>
/// DLL 路径解析器，负责查找和解析 Native DLL 文件的路径
/// 优先级：标准库（运行时目录） > 包管理器包 > 传统路径
/// </summary>
public static class DllPathResolver
{
    /// <summary>
    /// 搜索路径的优先级顺序
    /// </summary>
    internal static readonly Func<string, string, StandardLibraryCatalog, string?>[] SearchStrategies =
    [
        // 1. 运行时标准库路径（最高优先级）
        (dllName, _, standardLibraries) =>
        {
            // 检查是否为当前宿主目录中注册的标准库程序集
            if (IsStandardLibraryAssembly(dllName, standardLibraries))
            {
                return Path.Combine(AppContext.BaseDirectory, $"{dllName}.dll");
            }
            return null;
        },

        // 2. 包管理器包路径
        (dllName, _, _) =>
        {
            var packagesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".old8lang", "packages", dllName, $"{dllName}.dll"
            );
            return File.Exists(packagesDir) ? packagesDir : null;
        },

        // 3. 传统标准库路径：Old8LangLib/OldLib/dll（向后兼容）
        (dllName, importPath, _) =>
        {
            if (string.IsNullOrEmpty(importPath)) return null;
            return Path.Combine(importPath, "dll", $"{dllName}.dll");
        },

        // 4. 应用程序基目录（通用）
        (dllName, _, _) => Path.Combine(AppContext.BaseDirectory, $"{dllName}.dll"),

        // 5. 当前工作目录
        (dllName, _, _) => Path.Combine(Directory.GetCurrentDirectory(), $"{dllName}.dll"),

        // 6. Old8LangLib 开发路径：bin/Debug/net10.0
        (dllName, _, _) =>
        {
            if (dllName != "Old8LangLib" && dllName != "Old8Lang.NetLib") return null;
            return Path.Combine(Directory.GetCurrentDirectory(), dllName, "bin", "Debug", "net10.0", $"{dllName}.dll");
        },

        // 7. Old8LangLib 开发路径：bin/Debug/net8.0
        (dllName, _, _) =>
        {
            if (dllName != "Old8LangLib" && dllName != "Old8Lang.NetLib") return null;
            return Path.Combine(Directory.GetCurrentDirectory(), dllName, "bin", "Debug", "net8.0", $"{dllName}.dll");
        }
    ];

    /// <summary>
    /// 检查是否为标准库程序集
    /// </summary>
    internal static bool IsStandardLibraryAssembly(string assemblyName, StandardLibraryCatalog standardLibraries)
    {
        // 只检查当前宿主的模块目录，避免跨宿主读取进程默认目录
        return standardLibraries.Libraries.Values
            .Any(lib => lib.AssemblyName == assemblyName);
    }

    /// <summary>
    /// 解析 DLL 文件路径
    /// </summary>
    /// <param name="dllName">DLL 名称（不包含 .dll 扩展名）</param>
    /// <param name="importPath">导入路径（来自 LangInfo，可选，已弃用）</param>
    /// <param name="currentFilePath">当前文件路径（可选）</param>
    /// <returns>找到的 DLL 完整路径</returns>
    /// <exception cref="FileNotFoundException">找不到 DLL 文件时抛出</exception>
    /// <remarks>未提供模块目录时使用进程默认目录，这是既有调用方的兼容路径。</remarks>
    public static string ResolveDllPath(string dllName, string? importPath = null, string? currentFilePath = null)
        => ResolveDllPath(dllName, importPath, currentFilePath, StandardLibraryRegistry.DefaultCatalog);

    /// <summary>
    /// 使用指定标准库目录解析 DLL 文件路径。
    /// </summary>
    /// <param name="dllName">DLL 名称（不包含 .dll 扩展名）</param>
    /// <param name="importPath">导入路径（来自 LangInfo，可选，已弃用）</param>
    /// <param name="currentFilePath">当前文件路径（可选）</param>
    /// <param name="standardLibraries">当前宿主可见的标准库目录。</param>
    /// <returns>找到的 DLL 完整路径</returns>
    /// <exception cref="FileNotFoundException">找不到 DLL 文件时抛出</exception>
    public static string ResolveDllPath(
        string dllName,
        string? importPath,
        string? currentFilePath,
        StandardLibraryCatalog standardLibraries)
    {
        ArgumentNullException.ThrowIfNull(standardLibraries);

        var attemptedPaths = new List<string>();

        // 优先级 1: 如果提供了当前文件路径，首先查找项目根目录的 dll 文件夹
        if (!string.IsNullOrEmpty(currentFilePath))
        {
            var currentDir = Path.GetDirectoryName(currentFilePath);
            if (!string.IsNullOrEmpty(currentDir))
            {
                // 查找项目根目录
                var projectRoot = ProjectConfig.FindProjectRoot(currentDir);
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    var projectDllPath = Path.Combine(projectRoot, "dll", $"{dllName}.dll");
                    attemptedPaths.Add(projectDllPath);

                    if (File.Exists(projectDllPath))
                    {
                        return Path.GetFullPath(projectDllPath);
                    }
                }

                // 查找当前文件所在目录的 dll 文件夹
                var localDllPath = Path.Combine(currentDir, "dll", $"{dllName}.dll");
                attemptedPaths.Add(localDllPath);

                if (File.Exists(localDllPath))
                {
                    return Path.GetFullPath(localDllPath);
                }
            }
        }

        // 优先级 2-N: 按原有策略尝试所有搜索策略
        foreach (var strategy in SearchStrategies)
        {
            var path = strategy(dllName, importPath ?? "", standardLibraries);
            if (path is null) continue;

            attemptedPaths.Add(path);

            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        // 所有路径都找不到，抛出详细的错误信息
        throw new FileNotFoundException(BuildErrorMessage(dllName, attemptedPaths, standardLibraries));
    }

    /// <summary>
    /// 构建详细的错误消息
    /// </summary>
    internal static string BuildErrorMessage(
        string dllName,
        List<string> attemptedPaths,
        StandardLibraryCatalog standardLibraries)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"❌ 无法找到 DLL 文件: {dllName}.dll");
        sb.AppendLine();
        sb.AppendLine("🔍 已尝试以下路径：");

        for (int i = 0; i < attemptedPaths.Count; i++)
        {
            sb.AppendLine($"  {i + 1}. {attemptedPaths[i]}");
        }

        sb.AppendLine();
        sb.AppendLine("💡 建议：");
        sb.AppendLine("  1. 检查 DLL 文件是否存在于上述任一路径");
        sb.AppendLine("  2. 确保 DLL 文件名正确（区分大小写）");

        if (IsStandardLibraryAssembly(dllName, standardLibraries))
        {
            sb.AppendLine($"  3. '{dllName}' 是标准库程序集，应该随 Old8Lang 一起分发");
            sb.AppendLine($"     请确保 Old8Lang 正确构建并包含所有标准库依赖");
        }
        else
        {
            sb.AppendLine($"  3. 将 {dllName}.dll 放置在以下推荐位置：");
            sb.AppendLine($"     - {Path.Combine(AppContext.BaseDirectory, $"{dllName}.dll")} （应用程序目录）");
            sb.AppendLine($"     - ~/.old8lang/packages/{dllName}/{dllName}.dll （包管理器目录）");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取搜索路径列表（用于调试和文档）
    /// </summary>
    /// <remarks>未提供模块目录时使用进程默认目录，这是既有调用方的兼容路径。</remarks>
    public static List<string> GetSearchPaths(string dllName, string? importPath)
        => GetSearchPaths(dllName, importPath, StandardLibraryRegistry.DefaultCatalog);

    /// <summary>
    /// 使用指定标准库目录获取搜索路径列表。
    /// </summary>
    public static List<string> GetSearchPaths(
        string dllName,
        string? importPath,
        StandardLibraryCatalog standardLibraries)
    {
        ArgumentNullException.ThrowIfNull(standardLibraries);
        var paths = new List<string>();

        foreach (var strategy in SearchStrategies)
        {
            var path = strategy(dllName, importPath ?? "", standardLibraries);
            if (path is not null)
            {
                paths.Add(path);
            }
        }

        return paths;
    }
}
