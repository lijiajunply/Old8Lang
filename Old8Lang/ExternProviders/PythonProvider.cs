using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using Python.Runtime;

namespace Old8Lang.ExternProviders;

/// <summary>
/// Python 语言提供者
/// 支持 Python 脚本文件和 Python 模块导入
/// </summary>
public class PythonProvider : IExternProvider
{
    private readonly ExternType _pythonType;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pythonType">Python 类型（PythonScript 或 PythonModule）</param>
    public PythonProvider(ExternType pythonType)
    {
        if (pythonType != ExternType.PythonScript && pythonType != ExternType.PythonModule)
        {
            throw new ArgumentException("PythonProvider 仅支持 PythonScript 和 PythonModule 类型", nameof(pythonType));
        }
        _pythonType = pythonType;
    }

    /// <summary>
    /// 不支持编译模式（Python 需要动态运行时）
    /// </summary>
    public bool SupportsCompilation => false;

    /// <summary>
    /// 解释模式：加载 Python 函数
    /// </summary>
    public void LoadFunctions(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        VariateManager manager)
    {
        // 初始化 Python 运行时(仅初始化一次)
        if (!PythonEngine.IsInitialized)
        {
            // 检测并设置 Python DLL 路径
            var pythonDll = DetectPythonDll();
            if (!string.IsNullOrEmpty(pythonDll))
            {
                Runtime.PythonDLL = pythonDll;
            }
            else
            {
                throw new ImportError(null,
                    "无法找到 Python 运行时。请确保系统已安装 Python 3.7 或更高版本。\n" +
                    "或者手动设置环境变量 PYTHONNET_PYDLL 指向 Python 动态库路径。");
            }

            try
            {
                PythonEngine.Initialize();
            }
            catch (Exception ex)
            {
                throw new ImportError(null,
                    $"Python 运行时初始化失败：{ex.Message}\n" +
                    $"Python DLL 路径：{Runtime.PythonDLL}");
            }
        }

        using (Py.GIL())
        {
            dynamic module;

            if (_pythonType == ExternType.PythonModule)
            {
                // 导入全局 Python 模块
                var moduleName = source.StartsWith("pymodule:")
                    ? source.Substring("pymodule:".Length)
                    : source;
                module = Py.Import(moduleName);
            }
            else
            {
                // 导入 Python 脚本文件
                var scriptPath = source.StartsWith("py:")
                    ? source.Substring("py:".Length)
                    : source;

                // 解析脚本路径（支持相对路径）
                string fullPath;
                if (Path.IsPathRooted(scriptPath))
                {
                    fullPath = scriptPath;
                }
                else
                {
                    // 相对路径优先从当前工作目录解析
                    var cwdPath = Path.Combine(Directory.GetCurrentDirectory(), scriptPath);
                    if (File.Exists(cwdPath))
                    {
                        fullPath = cwdPath;
                    }
                    else
                    {
                        // 如果当前目录找不到,尝试从脚本文件所在目录解析
                        var baseDir = manager.Path != null && File.Exists(manager.Path)
                            ? Path.GetDirectoryName(manager.Path) ?? Directory.GetCurrentDirectory()
                            : manager.Path ?? Directory.GetCurrentDirectory();

                        fullPath = Path.Combine(baseDir, scriptPath);
                    }
                }

                if (!File.Exists(fullPath))
                {
                    throw new ImportError(null, $"Python 脚本文件不存在：{fullPath}");
                }

                // 执行脚本并获取模块
                var code = File.ReadAllText(fullPath);
                var scope = Py.CreateScope();
                scope.Exec(code);
                module = scope;
            }

            // 导入函数
            foreach (var funcDecl in functions)
            {
                var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

                // 获取 Python 函数对象
                if (!module.HasAttr(funcDecl.FunctionName))
                {
                    throw new InvalidOperationError(null,
                        $"Python 模块中找不到函数：{funcDecl.FunctionName}");
                }

                var pyFunc = module.GetAttr(funcDecl.FunctionName);

                // 创建包装函数
                var wrapperFunc = CreatePythonFunctionWrapper(pyFunc, funcDecl, targetName);
                manager.AddClassAndFunc(wrapperFunc);
            }
        }
    }

    /// <summary>
    /// 编译模式：不支持
    /// </summary>
    public void GenerateIL(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        ILGenerator ilGenerator,
        LocalManager localManager)
    {
        throw new NotSupportedException("Python extern 函数不支持编译模式，仅支持解释模式执行。");
    }

    /// <summary>
    /// 创建 Python 函数包装器
    /// </summary>
    private PythonFunctionLangValue CreatePythonFunctionWrapper(dynamic pyFunc, ExternFunctionDeclaration funcDecl, string targetName)
    {
        // 创建一个 Old8Lang 函数，该函数内部调用 Python 函数
        var signature = funcDecl.FunctionSignature?.FuncLangValue;
        var parameters = signature?.Ids ?? new List<LangId>();

        // 创建包装函数
        var funcValue = new PythonFunctionLangValue(targetName, pyFunc, parameters);

        return funcValue;
    }

    /// <summary>
    /// 检测系统中的 Python DLL 路径
    /// </summary>
    private string? DetectPythonDll()
    {
        // 如果已经设置,直接返回
        if (!string.IsNullOrEmpty(Runtime.PythonDLL))
        {
            return Runtime.PythonDLL;
        }

        // 根据操作系统选择不同的检测策略
        if (OperatingSystem.IsWindows())
        {
            return DetectPythonDllWindows();
        }
        else if (OperatingSystem.IsMacOS())
        {
            return DetectPythonDllMacOS();
        }
        else if (OperatingSystem.IsLinux())
        {
            return DetectPythonDllLinux();
        }

        return null;
    }

    /// <summary>
    /// Windows 平台检测 Python DLL
    /// </summary>
    private string? DetectPythonDllWindows()
    {
        // 尝试常见的 Python 版本 (从高到低)
        var versions = new[] { "313", "312", "311", "310", "39", "38", "37" };

        foreach (var ver in versions)
        {
            // 检查 PATH 环境变量
            var pythonDll = $"python{ver}.dll";
            if (File.Exists(Path.Combine(Environment.SystemDirectory, pythonDll)))
            {
                return pythonDll;
            }

            // 检查常见安装位置
            var paths = new[]
            {
                $@"C:\Python{ver}\python{ver}.dll",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    $@"Programs\Python\Python{ver}\python{ver}.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    $@"Python{ver}\python{ver}.dll")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// macOS 平台检测 Python 动态库
    /// </summary>
    private string? DetectPythonDllMacOS()
    {
        // 先尝试使用 python3 命令查询 Python 路径
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "-c \"import sys; print(sys.base_prefix)\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var basePath = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(basePath))
                {
                    // 获取版本号
                    var versionInfo = new ProcessStartInfo
                    {
                        FileName = "python3",
                        Arguments = "-c \"import sys; print(str(sys.version_info.major) + '.' + str(sys.version_info.minor))\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var versionProcess = Process.Start(versionInfo);
                    if (versionProcess != null)
                    {
                        var version = versionProcess.StandardOutput.ReadToEnd().Trim();
                        versionProcess.WaitForExit();

                        if (versionProcess.ExitCode == 0)
                        {
                            // 尝试多个可能的路径
                            var candidatePaths = new[]
                            {
                                Path.Combine(basePath, "Python"), // Framework 主文件
                                Path.Combine(basePath, $"lib/libpython{version}.dylib"),
                                Path.Combine(basePath, "lib/libpython3.dylib")
                            };

                            foreach (var path in candidatePaths)
                            {
                                // 解析符号链接
                                var resolvedPath = ResolveSymbolicLink(path);
                                if (File.Exists(resolvedPath))
                                {
                                    return resolvedPath;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // 如果命令执行失败,继续尝试其他方法
        }

        // 如果 python3 命令不可用,尝试常见安装路径
        var versions = new[] { "3.13", "3.12", "3.11", "3.10", "3.9", "3.8", "3.7" };

        foreach (var ver in versions)
        {
            var paths = new[]
            {
                $"/Library/Frameworks/Python.framework/Versions/{ver}/Python",
                $"/opt/homebrew/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/Python",
                $"/usr/local/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/Python",
                $"/opt/homebrew/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/lib/libpython{ver}.dylib",
                $"/usr/local/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/lib/libpython{ver}.dylib"
            };

            foreach (var path in paths)
            {
                var resolvedPath = ResolveSymbolicLink(path);
                if (File.Exists(resolvedPath))
                {
                    return resolvedPath;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 解析符号链接到实际文件路径
    /// </summary>
    private string ResolveSymbolicLink(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return path;
        }

        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.LinkTarget != null)
            {
                // 处理相对路径
                var targetPath = Path.IsPathRooted(fileInfo.LinkTarget)
                    ? fileInfo.LinkTarget
                    : Path.Combine(Path.GetDirectoryName(path) ?? "", fileInfo.LinkTarget);

                return Path.GetFullPath(targetPath);
            }
        }
        catch
        {
            // 如果解析失败,返回原路径
        }

        return path;
    }

    /// <summary>
    /// Linux 平台检测 Python 动态库
    /// </summary>
    private string? DetectPythonDllLinux()
    {
        // 先尝试使用 python3 命令查询
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "-c \"import sys; print(sys.base_prefix + '/lib/libpython' + str(sys.version_info.major) + '.' + str(sys.version_info.minor) + '.so')\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && File.Exists(output))
                {
                    return output;
                }
            }
        }
        catch
        {
            // 如果命令执行失败,继续尝试其他方法
        }

        var versions = new[] { "3.13", "3.12", "3.11", "3.10", "3.9", "3.8", "3.7" };

        foreach (var ver in versions)
        {
            var paths = new[]
            {
                $"/usr/lib/x86_64-linux-gnu/libpython{ver}.so",
                $"/usr/lib/libpython{ver}.so",
                $"/usr/local/lib/libpython{ver}.so",
                $"/lib/x86_64-linux-gnu/libpython{ver}.so"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        return null;
    }
}
