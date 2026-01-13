namespace Old8Lang;

/// <summary>
/// 表示 Old8Lang 语言的元信息，用于存储语言配置和库信息
/// </summary>
[Serializable]
public class LangInfo
{
    /// <summary>
    /// 获取或设置包管理路径
    /// </summary>
    public string ImportPath { get; set; } = "";

    /// <summary>
    /// 获取或设置语言版本
    /// </summary>
    public string Var { get; set; } = "1.0.0 rc6";
}

/// <summary>
/// 表示 Old8Lang 库的元信息，用于描述单个库的属性
/// </summary>
[Serializable]
public class LibInfo
{
    /// <summary>
    /// 获取或设置库名称
    /// </summary>
    public string LibName { get; set; } = "";

    /// <summary>
    /// 获取或设置库版本号
    /// </summary>
    public double Var { get; set; }

    /// <summary>
    /// 获取或设置库是否为目录类型
    /// </summary>
    public bool IsDir { get; set; }
}