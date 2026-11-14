namespace Old8Lang.AST.Statement;

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
