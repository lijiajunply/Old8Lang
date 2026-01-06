using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Extern 导入类型
/// </summary>
public enum ExternType
{
    /// <summary>
    /// C/C++ 原生 DLL（P/Invoke）
    /// </summary>
    NativeDll,

    /// <summary>
    /// Python 脚本文件
    /// </summary>
    PythonScript,

    /// <summary>
    /// Python 全局模块
    /// </summary>
    PythonModule
}

/// <summary>
/// P/Invoke 调用约定类型
/// </summary>
public enum CallingConventionType
{
    /// <summary>
    /// Cdecl 调用约定（默认，C 标准）
    /// </summary>
    Cdecl,

    /// <summary>
    /// StdCall 调用约定（Windows API 标准）
    /// </summary>
    StdCall,

    /// <summary>
    /// WinApi 调用约定（等同于 StdCall）
    /// </summary>
    WinApi
}

/// <summary>
/// 外部函数声明
/// </summary>
public class ExternFunctionDeclaration
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// 参数列表（FuncInit 包含参数类型信息）
    /// </summary>
    public FuncInit? FunctionSignature { get; }

    /// <summary>
    /// 别名（可选）
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// 调用约定
    /// </summary>
    public CallingConventionType CallingConvention { get; }

    public ExternFunctionDeclaration(
        string functionName,
        FuncInit? functionSignature = null,
        string? alias = null,
        CallingConventionType callingConvention = CallingConventionType.Cdecl)
    {
        FunctionName = functionName;
        FunctionSignature = functionSignature;
        Alias = alias;
        CallingConvention = callingConvention;
    }
}

/// <summary>
/// Extern 语句类，用于处理外部函数调用
/// 支持 C/C++ 原生库函数导入（P/Invoke）和 Python 函数导入
/// </summary>
public partial class ExternStatement : OldStatement
{
    /// <summary>
    /// DLL/模块名称（对于 C/C++ 是 DLL 名，对于 Python 是脚本路径或模块名）
    /// </summary>
    private readonly string DllName;

    /// <summary>
    /// 外部函数声明列表
    /// </summary>
    private readonly List<ExternFunctionDeclaration> Functions;

    /// <summary>
    /// 默认调用约定（仅用于 C/C++ P/Invoke）
    /// </summary>
    private readonly CallingConventionType DefaultCallingConvention;

    /// <summary>
    /// Extern 类型（C/C++ DLL、Python 脚本或 Python 模块）
    /// </summary>
    private readonly ExternType ExternType;

    /// <summary>
    /// 构造函数：创建 extern 语句
    /// </summary>
    /// <param name="dllName">DLL/模块名称</param>
    /// <param name="functions">外部函数声明列表</param>
    /// <param name="defaultCallingConvention">默认调用约定</param>
    /// <param name="externType">Extern 类型</param>
    public ExternStatement(
        string dllName,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention = CallingConventionType.Cdecl,
        ExternType externType = ExternType.NativeDll)
    {
        DllName = dllName;
        Functions = functions;
        DefaultCallingConvention = defaultCallingConvention;
        ExternType = externType;
    }

    /// <summary>
    /// 判断 DLL 名称并返回 Extern 类型
    /// </summary>
    public static ExternType DetectExternType(string dllName)
    {
        // Python 脚本文件：以 .py 结尾或 py: 前缀
        if (dllName.EndsWith(".py", StringComparison.OrdinalIgnoreCase) ||
            dllName.StartsWith("py:", StringComparison.OrdinalIgnoreCase))
        {
            return ExternType.PythonScript;
        }

        // Python 全局模块：pymodule: 前缀
        if (dllName.StartsWith("pymodule:", StringComparison.OrdinalIgnoreCase))
        {
            return ExternType.PythonModule;
        }

        // 默认为原生 DLL
        return ExternType.NativeDll;
    }

    /// <summary>
    /// 在解释模式下执行 extern 导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public override void Run(VariateManager manager)
    {
        // 根据类型选择不同的导入方式
        if (ExternType == ExternType.NativeDll)
        {
            RunNativeDll(manager);
        }
        else if (ExternType is ExternType.PythonScript or ExternType.PythonModule)
        {
            RunPython(manager);
        }
    }

    /// <summary>
    /// 执行原生 DLL 导入（P/Invoke）
    /// </summary>
    private void RunNativeDll(VariateManager manager)
    {
        foreach (var funcDecl in Functions)
        {
            var callingConv = funcDecl.CallingConvention != CallingConventionType.Cdecl
                ? funcDecl.CallingConvention
                : DefaultCallingConvention;

            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 创建 P/Invoke 委托
            var delegateType = CreateDelegateType(funcDecl, callingConv);
            var funcPtr = NativeLibrary.GetExport(
                NativeLibrary.Load(DllName),
                funcDecl.FunctionName);

            var del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);

            // 将委托包装为 Old8Lang 函数
            var funcValue = new FuncLangValue(targetName, del.Method, funcDecl.FunctionSignature?.FuncLangValue);
            manager.AddClassAndFunc(funcValue);
        }
    }

    /// <summary>
    /// 执行 Python 导入
    /// </summary>
    private void RunPython(VariateManager manager)
    {
        // 初始化 Python 运行时(仅初始化一次)
        if (!Python.Runtime.PythonEngine.IsInitialized)
        {
            // 检测并设置 Python DLL 路径
            var pythonDll = DetectPythonDll();
            if (!string.IsNullOrEmpty(pythonDll))
            {
                Python.Runtime.Runtime.PythonDLL = pythonDll;
            }
            else
            {
                throw new ImportError(Position,
                    "无法找到 Python 运行时。请确保系统已安装 Python 3.7 或更高版本。\n" +
                    "或者手动设置环境变量 PYTHONNET_PYDLL 指向 Python 动态库路径。");
            }

            try
            {
                Python.Runtime.PythonEngine.Initialize();
            }
            catch (Exception ex)
            {
                throw new ImportError(Position,
                    $"Python 运行时初始化失败：{ex.Message}\n" +
                    $"Python DLL 路径：{Python.Runtime.Runtime.PythonDLL}");
            }
        }

        using (Python.Runtime.Py.GIL())
        {
            dynamic module;

            if (ExternType == ExternType.PythonModule)
            {
                // 导入全局 Python 模块
                var moduleName = DllName.StartsWith("pymodule:")
                    ? DllName.Substring("pymodule:".Length)
                    : DllName;
                module = Python.Runtime.Py.Import(moduleName);
            }
            else
            {
                // 导入 Python 脚本文件
                var scriptPath = DllName.StartsWith("py:")
                    ? DllName.Substring("py:".Length)
                    : DllName;

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
                    throw new ImportError(Position, $"Python 脚本文件不存在：{fullPath}");
                }

                // 执行脚本并获取模块
                var code = File.ReadAllText(fullPath);
                var scope = Python.Runtime.Py.CreateScope();
                scope.Exec(code);
                module = scope;
            }

            // 导入函数
            foreach (var funcDecl in Functions)
            {
                var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

                // 获取 Python 函数对象
                if (!module.HasAttr(funcDecl.FunctionName))
                {
                    throw new InvalidOperationError(this,
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
    /// 创建 Python 函数包装器
    /// </summary>
    private PythonFunctionLangValue CreatePythonFunctionWrapper(dynamic pyFunc, ExternFunctionDeclaration funcDecl, string targetName)
    {
        // 创建一个 Old8Lang 函数，该函数内部调用 Python 函数
        var signature = funcDecl.FunctionSignature?.FuncLangValue;
        var parameters = signature?.Ids ?? new List<LangId>();

        // 创建包装函数体
        var wrapperStatements = new List<IOldLangTree>();

        // 注意：这里需要特殊处理，因为 Python 函数调用需要在运行时进行
        // 我们将 Python 函数对象存储为闭包变量
        var funcValue = new PythonFunctionLangValue(targetName, pyFunc, parameters);

        return funcValue;
    }

    /// <summary>
    /// 检测系统中的 Python DLL 路径
    /// </summary>
    private string? DetectPythonDll()
    {
        // 如果已经设置,直接返回
        if (!string.IsNullOrEmpty(Python.Runtime.Runtime.PythonDLL))
        {
            return Python.Runtime.Runtime.PythonDLL;
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
        var versions = new[] { "312", "311", "310", "39", "38", "37" };

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
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "-c \"import sys; print(sys.base_prefix)\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                var basePath = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(basePath))
                {
                    // 获取版本号
                    var versionInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "python3",
                        Arguments = "-c \"import sys; print(str(sys.version_info.major) + '.' + str(sys.version_info.minor))\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var versionProcess = System.Diagnostics.Process.Start(versionInfo);
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
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "-c \"import sys; print(sys.base_prefix + '/lib/libpython' + str(sys.version_info.major) + '.' + str(sys.version_info.minor) + '.so')\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
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

        var versions = new[] { "3.12", "3.11", "3.10", "3.9", "3.8", "3.7" };

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

    /// <summary>
    /// 在编译模式下生成 extern 导入的 IL 代码
    /// </summary>
    /// <param name="ilGenerator">IL 指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        foreach (var funcDecl in Functions)
        {
            var callingConv = funcDecl.CallingConvention != CallingConventionType.Cdecl
                ? funcDecl.CallingConvention
                : DefaultCallingConvention;

            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 创建 P/Invoke 方法
            var pinvokeMethod = CreatePInvokeMethod(funcDecl, callingConv);

            // 注册到局部变量管理器
            local.DelegateVar.Add(targetName, pinvokeMethod);
        }
    }

    /// <summary>
    /// 创建委托类型用于 P/Invoke
    /// </summary>
    private Type CreateDelegateType(ExternFunctionDeclaration funcDecl, CallingConventionType callingConv)
    {
        if (funcDecl.FunctionSignature == null)
        {
            throw new TypeError(this, $"extern 函数 {funcDecl.FunctionName} 必须指定函数签名（参数类型和返回类型）");
        }

        var signature = funcDecl.FunctionSignature.FuncLangValue;
        var paramTypes = signature.Ids?
            .Select(p => ConvertOld8TypeToCSharpType(p.AssumptionType))
            .ToArray() ?? [];

        var returnType = ConvertOld8TypeToCSharpType(signature.Id?.AssumptionType);

        // 动态创建委托类型
        var assemblyName = new AssemblyName($"ExternDelegate_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("ExternModule");
        var typeBuilder = moduleBuilder.DefineType(
            $"ExternDelegate_{funcDecl.FunctionName}",
            TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(MulticastDelegate));

        // 添加 UnmanagedFunctionPointer 特性
        var unmanagedAttr = new CustomAttributeBuilder(
            typeof(UnmanagedFunctionPointerAttribute).GetConstructor([typeof(CallingConvention)])!,
            [ConvertCallingConvention(callingConv)]);
        typeBuilder.SetCustomAttribute(unmanagedAttr);

        // 定义构造函数
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(object), typeof(IntPtr)]);
        ctorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

        // 定义 Invoke 方法
        var invokeBuilder = typeBuilder.DefineMethod(
            "Invoke",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            returnType,
            paramTypes);
        invokeBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

        return typeBuilder.CreateType()!;
    }

    /// <summary>
    /// 创建 P/Invoke 方法信息
    /// </summary>
    private MethodInfo CreatePInvokeMethod(ExternFunctionDeclaration funcDecl, CallingConventionType callingConv)
    {
        if (funcDecl.FunctionSignature == null)
        {
            throw new TypeError(this, $"extern 函数 {funcDecl.FunctionName} 必须指定函数签名（参数类型和返回类型）");
        }

        var signature = funcDecl.FunctionSignature.FuncLangValue;
        var paramTypes = signature.Ids?
            .Select(p => ConvertOld8TypeToCSharpType(p.AssumptionType))
            .ToArray() ?? [];

        var returnType = ConvertOld8TypeToCSharpType(signature.Id?.AssumptionType);

        // 动态创建类型和方法
        var assemblyName = new AssemblyName($"ExternPInvoke_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("ExternPInvokeModule");
        var typeBuilder = moduleBuilder.DefineType(
            $"ExternPInvoke_{funcDecl.FunctionName}",
            TypeAttributes.Public);

        // 定义 P/Invoke 方法
        var methodBuilder = typeBuilder.DefinePInvokeMethod(
            funcDecl.FunctionName,
            DllName,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
            CallingConventions.Standard,
            returnType,
            paramTypes,
            ConvertCallingConvention(callingConv),
            CharSet.Auto);
        methodBuilder.SetImplementationFlags(
            methodBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

        var type = typeBuilder.CreateType()!;
        return type.GetMethod(funcDecl.FunctionName)!;
    }

    /// <summary>
    /// 将 Old8Lang 类型转换为 C# 类型
    /// </summary>
    private Type ConvertOld8TypeToCSharpType(string? old8Type)
    {
        return old8Type?.ToLower() switch
        {
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "float" => typeof(float),
            "bool" => typeof(bool),
            "string" => typeof(string),
            "void" => typeof(void),
            "object" => typeof(object),
            "char" => typeof(char),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "uint" => typeof(uint),
            "ulong" => typeof(ulong),
            "ushort" => typeof(ushort),
            null => typeof(void),
            _ => throw new TypeError(this, $"不支持的 extern 类型: {old8Type}")
        };
    }

    /// <summary>
    /// 转换调用约定枚举
    /// </summary>
    private CallingConvention ConvertCallingConvention(CallingConventionType type)
    {
        return type switch
        {
            CallingConventionType.Cdecl => CallingConvention.Cdecl,
            CallingConventionType.StdCall => CallingConvention.StdCall,
            CallingConventionType.WinApi => CallingConvention.Winapi,
            _ => CallingConvention.Cdecl
        };
    }

    /// <summary>
    /// 获取指定索引处的语句
    /// </summary>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量
    /// </summary>
    public override int Count => 0;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 将 extern 语句转换为字符串表示
    /// </summary>
    public override string ToString()
    {
        var convStr = DefaultCallingConvention != CallingConventionType.Cdecl
            ? $" {DefaultCallingConvention.ToString().ToLower()}"
            : "";

        if (Functions.Count == 1)
        {
            var func = Functions[0];
            var convOverrideStr = func.CallingConvention != DefaultCallingConvention
                ? $"{func.CallingConvention.ToString().ToLower()} "
                : "";
            var aliasStr = func.Alias != null ? $" as {func.Alias}" : "";
            var signature = FormatFunctionSignature(func);
            return $"native extern \"{DllName}\"{convStr} {convOverrideStr}func {func.FunctionName}{signature}{aliasStr}";
        }

        var funcs = string.Join("\n    ", Functions.Select(f =>
        {
            var convOverrideStr = f.CallingConvention != DefaultCallingConvention
                ? $"{f.CallingConvention.ToString().ToLower()} "
                : "";
            var aliasStr = f.Alias != null ? $" as {f.Alias}" : "";
            var signature = FormatFunctionSignature(f);
            return $"{convOverrideStr}func {f.FunctionName}{signature}{aliasStr}";
        }));

        return $"native extern \"{DllName}\"{convStr} {{\n    {funcs}\n}}";
    }

    /// <summary>
    /// 格式化函数签名为字符串
    /// </summary>
    private string FormatFunctionSignature(ExternFunctionDeclaration func)
    {
        if (func.FunctionSignature == null)
            return "()";

        var funcValue = func.FunctionSignature.FuncLangValue;
        var parameters = funcValue.Ids != null
            ? string.Join(", ", funcValue.Ids.Select(p =>
            {
                var type = !string.IsNullOrEmpty(p.AssumptionType) ? $":{p.AssumptionType}" : "";
                return $"{p.IdName}{type}";
            }))
            : "";

        var returnType = funcValue.Id?.AssumptionType ?? "void";
        return $"({parameters}) -> {returnType}";
    }
}
