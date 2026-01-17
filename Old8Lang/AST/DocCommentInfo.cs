namespace Old8Lang.AST;

/// <summary>
/// 结构化的文档注释信息
/// 支持解析多种文档注释风格（Google, NumPy, Sphinx, JavaDoc, 中文等）
/// </summary>
public class DocCommentInfo
{
    /// <summary>
    /// 原始文档注释文本（未解析）
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// 摘要/简介（函数或类的主要描述）
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 详细描述（可选）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数列表
    /// </summary>
    public List<ParameterDoc> Parameters { get; set; } = [];

    /// <summary>
    /// 返回值描述
    /// </summary>
    public ReturnDoc? Returns { get; set; }

    /// <summary>
    /// 异常/错误描述列表
    /// </summary>
    public List<ThrowsDoc> Throws { get; set; } = [];

    /// <summary>
    /// 示例代码
    /// </summary>
    public List<string> Examples { get; set; } = [];

    /// <summary>
    /// 备注
    /// </summary>
    public List<string> Remarks { get; set; } = [];

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 自定义标签（用于存储未识别的标签）
    /// </summary>
    public Dictionary<string, List<string>> CustomTags { get; set; } = new();

    /// <summary>
    /// 检查文档注释是否为空
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(RawText) &&
               string.IsNullOrWhiteSpace(Summary) &&
               string.IsNullOrWhiteSpace(Description) &&
               Parameters.Count == 0 &&
               Returns is null &&
               Throws.Count == 0;
    }
}

/// <summary>
/// 参数文档
/// </summary>
public class ParameterDoc(string name, string? type, string description)
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// 参数类型（可选，从注释中提取）
    /// </summary>
    public string? Type { get; set; } = type;

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = description;
}

/// <summary>
/// 返回值文档
/// </summary>
public class ReturnDoc
{
    /// <summary>
    /// 返回值类型（可选，从注释中提取）
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 返回值描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public ReturnDoc()
    {
    }

    public ReturnDoc(string? type, string description)
    {
        Type = type;
        Description = description;
    }
}

/// <summary>
/// 异常/错误文档
/// </summary>
public class ThrowsDoc
{
    /// <summary>
    /// 异常类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 异常描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public ThrowsDoc()
    {
    }

    public ThrowsDoc(string type, string description)
    {
        Type = type;
        Description = description;
    }
}