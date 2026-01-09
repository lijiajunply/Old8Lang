using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 代码格式化服务
/// </summary>
public class FormattingService
{
    /// <summary>
    /// 格式化文档
    /// </summary>
    public List<TextEdit> FormatDocument(string content, FormattingOptions options)
    {
        var lines = content.Split('\n');
        var formattedLines = new List<string>();
        int indentLevel = 0;
        bool inMultilineComment = false;

        foreach (var t in lines)
        {
            var line = t.TrimEnd('\r');
            var trimmedLine = line.Trim();

            // 处理空行
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                formattedLines.Add("");
                continue;
            }

            // 处理多行注释
            if (trimmedLine.StartsWith("/*"))
            {
                inMultilineComment = true;
            }

            if (trimmedLine.EndsWith("*/"))
            {
                inMultilineComment = false;
            }

            // 如果在多行注释中，保持原样
            if (inMultilineComment)
            {
                formattedLines.Add(line);
                continue;
            }

            // 单行注释保持缩进
            if (trimmedLine.StartsWith("//"))
            {
                formattedLines.Add(GetIndentation(indentLevel, options) + trimmedLine);
                continue;
            }

            // 处理右大括号减少缩进
            if (trimmedLine.StartsWith("}"))
            {
                indentLevel = Math.Max(0, indentLevel - 1);
            }

            // 应用缩进
            var formattedLine = GetIndentation(indentLevel, options) + trimmedLine;
            formattedLines.Add(formattedLine);

            // 处理左大括号增加缩进
            if (trimmedLine.EndsWith("{"))
            {
                indentLevel++;
            }

            // 处理同一行的右大括号
            if (trimmedLine.Contains("}") && trimmedLine.EndsWith("}") && !trimmedLine.StartsWith("}"))
            {
                indentLevel = Math.Max(0, indentLevel - 1);
            }
        }

        // 创建单个编辑操作，替换整个文档
        var edits = new List<TextEdit>
        {
            new TextEdit
            {
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(0, 0),
                    new Position(lines.Length, 0)
                ),
                NewText = string.Join("\n", formattedLines)
            }
        };

        return edits;
    }

    /// <summary>
    /// 获取缩进字符串
    /// </summary>
    private string GetIndentation(int level, FormattingOptions options)
    {
        if (level <= 0)
        {
            return "";
        }

        if (options.InsertSpaces)
        {
            return new string(' ', (int)(level * options.TabSize));
        }
        else
        {
            return new string('\t', level);
        }
    }

    /// <summary>
    /// 格式化选定范围
    /// </summary>
    public List<TextEdit> FormatRange(string content, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range,
        FormattingOptions options)
    {
        var lines = content.Split('\n');
        var startLine = (int)range.Start.Line;
        var endLine = (int)range.End.Line;

        // 提取选定范围的内容
        var selectedLines = new List<string>();
        for (int i = startLine; i <= endLine && i < lines.Length; i++)
        {
            selectedLines.Add(lines[i]);
        }

        var selectedContent = string.Join("\n", selectedLines);

        // 格式化选定内容
        var formattedEdits = FormatDocument(selectedContent, options);

        // 调整编辑范围 - 创建新的 TextEdit 对象而不是修改现有的
        var adjustedEdits = formattedEdits.Select(edit => new TextEdit
        {
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(startLine + edit.Range.Start.Line, edit.Range.Start.Character),
                new Position(startLine + edit.Range.End.Line, edit.Range.End.Character)
            ),
            NewText = edit.NewText
        }).ToList();

        return adjustedEdits;
    }
}