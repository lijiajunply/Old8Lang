using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.ExternProviders;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Extern 导入类型
/// </summary>
public enum ExternType
{
    /// <summary>
    /// C# DLL（托管程序集）
    /// </summary>
    CSharpDll,

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
    PythonModule,

    /// <summary>
    /// JavaScript 脚本文件
    /// </summary>
    JavaScript
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
public class ExternFunctionDeclaration(
    string functionName,
    FuncInit? functionSignature = null,
    string? alias = null,
    CallingConventionType callingConvention = CallingConventionType.Cdecl)
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string FunctionName { get; } = functionName;

    /// <summary>
    /// 参数列表（FuncInit 包含参数类型信息）
    /// </summary>
    public FuncInit? FunctionSignature { get; } = functionSignature;

    /// <summary>
    /// 别名（可选）
    /// </summary>
    public string? Alias { get; } = alias;

    /// <summary>
    /// 调用约定
    /// </summary>
    public CallingConventionType CallingConvention { get; } = callingConvention;
}

/// <summary>
/// C# DLL 导入模式
/// </summary>
public enum CSharpImportMode
{
    /// <summary>
    /// 单个方法导入
    /// </summary>
    SingleMethod,

    /// <summary>
    /// 批量导入所有方法 (*)
    /// </summary>
    AllMethods,

    /// <summary>
    /// 选择性导入多个方法 { Method1, Method2 }
    /// </summary>
    SelectiveMethods,

    /// <summary>
    /// 类导入
    /// </summary>
    ClassImport
}

/// <summary>
/// C# DLL 导入声明
/// </summary>
public class CSharpDllImportDeclaration
{
    /// <summary>
    /// 类名称
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// 导入模式
    /// </summary>
    public CSharpImportMode ImportMode { get; }

    /// <summary>
    /// 方法名称（单个方法导入时使用）
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// 方法列表（选择性导入时使用）
    /// </summary>
    public List<string>? MethodList { get; }

    /// <summary>
    /// 别名（可选）
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// 函数签名（单个方法导入时使用）
    /// </summary>
    public FuncInit? FunctionSignature { get; }

    /// <summary>
    /// 构造函数：单个方法导入
    /// </summary>
    public CSharpDllImportDeclaration(string className, string methodName, string? alias = null, FuncInit? functionSignature = null)
    {
        ClassName = className;
        ImportMode = CSharpImportMode.SingleMethod;
        MethodName = methodName;
        Alias = alias;
        FunctionSignature = functionSignature;
    }

    /// <summary>
    /// 构造函数：批量导入所有方法
    /// </summary>
    public CSharpDllImportDeclaration(string className, bool importAll)
    {
        ClassName = className;
        ImportMode = CSharpImportMode.AllMethods;
    }

    /// <summary>
    /// 构造函数：选择性导入多个方法
    /// </summary>
    public CSharpDllImportDeclaration(string className, List<string> methodList)
    {
        ClassName = className;
        ImportMode = CSharpImportMode.SelectiveMethods;
        MethodList = methodList;
    }

    /// <summary>
    /// 构造函数：类导入
    /// </summary>
    public CSharpDllImportDeclaration(string className, string? alias, bool isClassImport)
    {
        ClassName = className;
        ImportMode = CSharpImportMode.ClassImport;
        Alias = alias;
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
    private readonly string _dllName;

    /// <summary>
    /// 外部函数声明列表
    /// </summary>
    private readonly List<ExternFunctionDeclaration> _functions;

    /// <summary>
    /// 默认调用约定（仅用于 C/C++ P/Invoke）
    /// </summary>
    private readonly CallingConventionType _defaultCallingConvention;

    /// <summary>
    /// Extern 类型（C/C++ DLL、Python 脚本或 Python 模块）
    /// </summary>
    private readonly ExternType _externType;

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
        _dllName = dllName;
        _functions = functions;
        _defaultCallingConvention = defaultCallingConvention;
        _externType = externType;
    }

    /// <summary>
    /// 判断 DLL 名称并返回 Extern 类型
    /// </summary>
    public static ExternType DetectExternType(string dllName)
    {
        // JavaScript 脚本文件：以 .js 结尾或 js: 前缀
        if (dllName.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            dllName.StartsWith("js:", StringComparison.OrdinalIgnoreCase))
        {
            return ExternType.JavaScript;
        }

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
        // 使用工厂创建对应的提供者
        var provider = ExternProviderFactory.CreateProvider(_externType);

        // 委托给提供者执行
        provider.LoadFunctions(_dllName, _functions, _defaultCallingConvention, manager);
    }

    /// <summary>
    /// 在编译模式下生成 extern 导入的 IL 代码
    /// </summary>
    /// <param name="ilGenerator">IL 指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 使用工厂创建对应的提供者
        var provider = ExternProviderFactory.CreateProvider(_externType);

        // 检查是否支持编译模式
        if (!provider.SupportsCompilation)
        {
            throw new NotSupportedException($"{_externType} 类型的 extern 函数不支持编译模式，仅支持解释模式执行。");
        }

        // 委托给提供者生成 IL 代码
        provider.GenerateIL(_dllName, _functions, _defaultCallingConvention, ilGenerator, local);
    }

    /// <summary>
    /// 获取指定索引处的语句
    /// </summary>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量
    /// </summary>
    public override int Count => _functions.Count;

    /// <summary>
    /// 将 extern 语句转换为字符串表示
    /// </summary>
    public override string ToString()
    {
        var convStr = _defaultCallingConvention != CallingConventionType.Cdecl
            ? $" {_defaultCallingConvention.ToString().ToLower()}"
            : "";

        if (_functions.Count == 1)
        {
            var func = _functions[0];
            var convOverrideStr = func.CallingConvention != _defaultCallingConvention
                ? $"{func.CallingConvention.ToString().ToLower()} "
                : "";
            var aliasStr = func.Alias is not null ? $" as {func.Alias}" : "";
            var signature = FormatFunctionSignature(func);
            return
                $"extern \"{_dllName}\"{convStr} {convOverrideStr}func {func.FunctionName}{signature}{aliasStr}";
        }

        var funcs = string.Join("\n    ", _functions.Select(f =>
        {
            var convOverrideStr = f.CallingConvention != _defaultCallingConvention
                ? $"{f.CallingConvention.ToString().ToLower()} "
                : "";
            var aliasStr = f.Alias is not null ? $" as {f.Alias}" : "";
            var signature = FormatFunctionSignature(f);
            return $"{convOverrideStr}func {f.FunctionName}{signature}{aliasStr}";
        }));

        return $"extern \"{_dllName}\"{convStr} {{\n    {funcs}\n}}";
    }

    /// <summary>
    /// 格式化函数签名为字符串
    /// </summary>
    private string FormatFunctionSignature(ExternFunctionDeclaration func)
    {
        if (func.FunctionSignature is null)
            return "()";

        var funcValue = func.FunctionSignature.FuncValue;
        var parameters = funcValue.Ids is not null
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