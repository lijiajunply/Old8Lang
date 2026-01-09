using System.Reflection.Emit;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 文件头指令 AST 节点
/// 用于在文件开头声明配置信息，类似 Python 的 shebang 或 encoding 声明
///
/// 语法示例：
/// #!encoding utf-8
/// #!author 张三
/// #!version 1.0.0
/// #!strict true
/// #!optimize 2
/// </summary>
public class FileHeaderDirective(string directiveName, string directiveValue, int line) : OldStatement
{
    /// <summary>
    /// 指令名称（如 encoding, author, version, strict 等）
    /// </summary>
    public string DirectiveName { get; set; } = directiveName;

    /// <summary>
    /// 指令值（字符串形式）
    /// </summary>
    public string DirectiveValue { get; set; } = directiveValue;

    /// <summary>
    /// 行号
    /// </summary>
    public int Line { get; set; } = line;

    public override string ToString()
    {
        return $"#!{DirectiveName} {DirectiveValue}";
    }

    public override void Run(VariateManager manager)
    {
        // 文件头指令在解析时已经处理，运行时不需要执行任何操作
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 文件头指令在编译时已经处理，不需要生成IL代码
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // 文件头指令通常不需要遍历
        return default!;
    }
}

/// <summary>
/// 文件头指令集合
/// 包含一个文件中所有的文件头指令
/// </summary>
public class FileHeaderDirectives
{
    private readonly Dictionary<string, string> DirectivesDict = new();

    /// <summary>
    /// 所有指令的列表（保持顺序）
    /// </summary>
    public List<FileHeaderDirective> DirectiveList { get; } = [];

    /// <summary>
    /// 添加指令
    /// </summary>
    public void AddDirective(FileHeaderDirective directive)
    {
        DirectiveList.Add(directive);
        // 如果有重复的指令名，后面的会覆盖前面的
        DirectivesDict[directive.DirectiveName.ToLower()] = directive.DirectiveValue;
    }

    /// <summary>
    /// 获取指令值
    /// </summary>
    public string? GetDirective(string name)
    {
        return DirectivesDict.GetValueOrDefault(name.ToLower());
    }

    /// <summary>
    /// 检查是否存在某个指令
    /// </summary>
    public bool HasDirective(string name)
    {
        return DirectivesDict.ContainsKey(name.ToLower());
    }

    /// <summary>
    /// 获取布尔类型的指令值
    /// </summary>
    public bool GetBoolDirective(string name, bool defaultValue = false)
    {
        var value = GetDirective(name);
        if (value is null) return defaultValue;

        return value.ToLower() switch
        {
            "true" => true,
            "false" => false,
            "1" => true,
            "0" => false,
            "yes" => true,
            "no" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// 获取整数类型的指令值
    /// </summary>
    public int GetIntDirective(string name, int defaultValue = 0)
    {
        var value = GetDirective(name);
        if (value is null) return defaultValue;

        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}