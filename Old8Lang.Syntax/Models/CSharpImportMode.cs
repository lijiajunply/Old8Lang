namespace Old8Lang.AST.Statement;

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
