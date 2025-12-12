using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class NativeStatement : OldStatement
{
    
    private readonly string DllName;

    private readonly string ClassName;

    private readonly string? MethodName;

    private string? NativeName { get; set; }

    private readonly string? Name;
    private readonly FuncLangValue? FuncValue;

    public NativeStatement(string dllName, string className, string methodName, string nativeName)
    {
        DllName = dllName;
        ClassName = className;
        MethodName = methodName;
        NativeName = nativeName;
    }

    public NativeStatement(string dllName, string className, string methodName, string nativeName, FuncInit a)
    {
        DllName = dllName;
        ClassName = className;
        MethodName = methodName;
        NativeName = nativeName;
        FuncValue = a.FuncLangValue;
    }

    public NativeStatement(string dllName, string className, string name = "")
    {
        DllName = dllName;
        ClassName = className;
        Name = name;
    }

    public override void Run(VariateManager manager)
    {
        // 构建DLL路径，尝试多种可能的位置
        var dllFileName = $"{DllName}.dll";
        string path;

        // 1. 尝试从Old8LangLib/OldLib/dll目录查找
        var oldLibDllPath = Path.Combine(manager.LangInfo?.ImportPath ?? "", "dll", dllFileName);
        if (File.Exists(oldLibDllPath))
        {
            path = oldLibDllPath;
        }
        // 2. 尝试从当前文件所在目录的dll子目录查找
        else if (string.IsNullOrEmpty(manager.Path))
        {
            path = Path.Combine(Path.GetDirectoryName(manager.Path) ?? "", "dll", dllFileName);
        }
        // 3. 尝试直接从应用程序基目录查找
        else
        {
            path = Path.Combine(AppContext.BaseDirectory, dllFileName);
        }

        // 确保文件存在
        if (!File.Exists(path))
        {
            // 尝试使用当前目录的绝对路径
            var absolutePath = Path.GetFullPath(dllFileName);
            if (File.Exists(absolutePath))
            {
                path = absolutePath;
            }
            else
            {
                // 尝试使用Old8LangLib.dll的绝对路径（针对Old8LangLib特殊处理）
                if (DllName == "Old8LangLib")
                {
                    var directDllPath = Path.Combine(Directory.GetCurrentDirectory(), "Old8LangLib", "OldLib", "dll",
                        dllFileName);
                    if (File.Exists(directDllPath))
                    {
                        path = directDllPath;
                    }
                    else
                    {
                        // 最后尝试从bin目录查找 - 支持 net8.0 和 net10.0
                        var binDllPath8 = Path.Combine(Directory.GetCurrentDirectory(), "Old8LangLib", "bin", "Debug",
                            "net8.0", dllFileName);
                        var binDllPath10 = Path.Combine(Directory.GetCurrentDirectory(), "Old8LangLib", "bin", "Debug",
                            "net10.0", dllFileName);
                        if (File.Exists(binDllPath10))
                        {
                            path = binDllPath10;
                        }
                        else if (File.Exists(binDllPath8))
                        {
                            path = binDllPath8;
                        }
                        else
                        {
                            throw new FileNotFoundException(
                                $"无法找到DLL文件 {dllFileName}，尝试的路径：{oldLibDllPath}, {path}, {absolutePath}, {directDllPath}, {binDllPath8}, {binDllPath10}");
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException(
                        $"无法找到DLL文件 {dllFileName}，尝试的路径：{oldLibDllPath}, {path}, {absolutePath}");
                }
            }
        }

        // 加载程序集并获取类型
        var assembly = Assembly.LoadFile(path);
        var type = assembly.GetType($"{DllName}.{ClassName}");

        if (!string.IsNullOrEmpty(Name))
        {
            type = assembly.GetType($"{Name}.{ClassName}");
            if (type is null)
            {
                type = Type.GetType($"{Name}.{ClassName}");
                if (type is null)
                    throw new TypeError(this, $"找不到类型 {Name}.{ClassName}");
            }

            manager.AddClassAndFunc(new NativeStaticAny(ClassName, type));
            return;
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var methodInfo = type?.GetMethod(MethodName);
            if (methodInfo == null) throw new InvalidOperationError(this, $"找不到方法 {MethodName} 在 {ClassName} 类中");
            if (string.IsNullOrEmpty(NativeName))
                NativeName = MethodName;
            var func = new FuncLangValue(NativeName, methodInfo, FuncValue);
            manager.AddClassAndFunc(func);
            return;
        }

        manager.AddClassAndFunc((ImportInfo)new NativeAnyLangValue(DllName, ClassName, path).Run(manager));
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var path = $"{Path.GetDirectoryName(local.FilePath)}/dll/{DllName}.dll"; // filepath/dll/dllname
        var assembly = Assembly.LoadFile(path);
        var type = assembly.GetType($"{DllName}.{ClassName}");
        if (!string.IsNullOrEmpty(Name))
        {
            type = assembly.GetType($"{Name}.{ClassName}");
            if (type is null)
            {
                type = Type.GetType($"{Name}.{ClassName}");
                if (type is null)
                    throw new TypeError(this, $"找不到类型 {Name}.{ClassName}");
            }

            local.ClassVar.Add(ClassName, type);
            return;
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var methodInfo = type?.GetMethod(MethodName);
            if (methodInfo == null) throw new InvalidOperationError(this, $"找不到方法 {MethodName} 在 {ClassName} 类中");
            if (string.IsNullOrEmpty(NativeName))
                NativeName = MethodName;
            local.DelegateVar.Add(NativeName, methodInfo);
        }
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return $"import native {DllName}.{ClassName} as {Name}";
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var funcName = string.IsNullOrEmpty(NativeName) ? MethodName : NativeName;
            return $"import native {DllName}.{ClassName}.{MethodName} as {funcName}\n{FuncValue}";
        }

        return $"import native {DllName}.{ClassName}";
    }
}