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
