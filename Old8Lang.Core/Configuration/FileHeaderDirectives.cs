namespace Old8Lang;

/// <summary>
/// 文件头指令的解析结果。
/// </summary>
public sealed record FileHeaderDirectiveInfo(string Name, string Value, int Line);

/// <summary>
/// 文件头指令集合。
/// </summary>
/// <remarks>
/// 此模型仅描述源文件配置，不是可执行 AST 节点。
/// </remarks>
public sealed class FileHeaderDirectives
{
    private readonly Dictionary<string, string> _directives = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 所有指令，按在源文件中的出现顺序保留。
    /// </summary>
    public List<FileHeaderDirectiveInfo> DirectiveList { get; } = [];

    /// <summary>
    /// 添加一条指令；同名指令以最后一条为准。
    /// </summary>
    public void AddDirective(FileHeaderDirectiveInfo directive)
    {
        DirectiveList.Add(directive);
        _directives[directive.Name] = directive.Value;
    }

    public string? GetDirective(string name) => _directives.GetValueOrDefault(name);

    public bool HasDirective(string name) => _directives.ContainsKey(name);

    public bool GetBoolDirective(string name, bool defaultValue = false)
    {
        var value = GetDirective(name);
        return value?.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" => true,
            "false" or "0" or "no" => false,
            _ => defaultValue
        };
    }

    public int GetIntDirective(string name, int defaultValue = 0)
    {
        var value = GetDirective(name);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}
