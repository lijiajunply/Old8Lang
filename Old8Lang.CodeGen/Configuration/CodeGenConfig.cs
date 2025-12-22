// Configuration/CodeGenConfig.cs
using System.Text.Json;

namespace Old8Lang.CodeGen.Configuration;

/// <summary>
/// 代码生成器配置
/// </summary>
public class CodeGenConfig
{
    /// <summary>
    /// 扫描目录
    /// </summary>
    public string ScanDirectory { get; set; } = "../Old8Lang/AST";

    /// <summary>
    /// 输出目录
    /// </summary>
    public string OutputDirectory { get; set; } = "../Old8Lang/AST/Visitor/Generated";

    /// <summary>
    /// 排除的文件模式
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new()
    {
        "**/ValueFunctions/**",
        "**/ModuleObjects/**"
    };

    /// <summary>
    /// 排除的类名
    /// </summary>
    public List<string> ExcludeClasses { get; set; } = new()
    {
        "IOldLangTree",
        "OldStatement",
        "LangExpression",
        "LangValueType",
        "IfChild",
        "DllPathResolver"
    };

    /// <summary>
    /// 生成 partial 类
    /// </summary>
    public bool GeneratePartialClasses { get; set; } = true;

    /// <summary>
    /// 添加 GeneratedCode 属性
    /// </summary>
    public bool AddGeneratedCodeAttribute { get; set; } = true;

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    public static CodeGenConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"[WARN] 配置文件不存在: {configPath}，使用默认配置");
            return new CodeGenConfig();
        }

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<CodeGenConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        return config ?? new CodeGenConfig();
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    public void Save(string configPath)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(configPath, json);
    }
}
