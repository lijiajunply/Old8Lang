using System.Text.RegularExpressions;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Unit.Error;

/// <summary>
/// 错误信息中源代码上下文的行号标注与脱字符位置测试
/// </summary>
/// <remarks>
/// 此前渲染器用 "错误行号 - 上下文长度/2" 反推窗口首行行号，
/// 该式只在窗口恒为 5 行且以错误行为中心时成立；窗口在文件首尾被裁剪后
/// 行号标注与脱字符会整体错位。另外上下文切分使用了 RemoveEmptyEntries，
/// 源码里的空行会被丢弃，使上下文下标与真实行号彻底错位。
/// </remarks>
[Collection("Sequential")]
public class SourceContextRenderingTests
{
    private static readonly Regex AnsiPattern = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);

    private static Old8Exception ParseFailure(string code)
    {
        var interpreter = new LangInterpreter();
        return Assert.Throws<SyntaxError>(() => interpreter.Build(code));
    }

    private static string StripAnsi(string text) => AnsiPattern.Replace(text, string.Empty);

    /// <summary>
    /// 取出渲染结果中 "行号 | 源码" 形式的行号，按出现顺序返回
    /// </summary>
    private static List<int> RenderedLineNumbers(string message)
    {
        var numbers = new List<int>();
        foreach (var line in StripAnsi(message).Split('\n'))
        {
            var match = Regex.Match(line, @"^\s*(-?\d+)\s*\|");
            if (match.Success)
            {
                numbers.Add(int.Parse(match.Groups[1].Value));
            }
        }

        return numbers;
    }

    [Fact]
    public void SyntaxError_AtStartOfFile_LabelsLinesFromOne()
    {
        // 错误在第 1 行：窗口向上被裁剪，旧公式会把首行标成 0
        var error = ParseFailure("<- 1\n");

        var numbers = RenderedLineNumbers(error.Message);

        Assert.NotEmpty(numbers);
        Assert.Equal(1, numbers[0]);
    }

    [Fact]
    public void SyntaxError_BlankLinesInSource_DoNotShiftLabels()
    {
        // 源码中的空行必须计入行号，否则标注会整体前移
        const string code = "x <- 1\n\ny <- 2\n\nz <- ) + 3\n\nw <- 4\n";

        var error = ParseFailure(code);
        var numbers = RenderedLineNumbers(error.Message);

        Assert.Equal(5, error.Position.Line);
        Assert.Contains(5, numbers);
        // 第 5 行必须是标注里紧邻脱字符的那一行
        var stripped = StripAnsi(error.Message);
        var errorLineIndex = stripped.IndexOf("z <- ) + 3", StringComparison.Ordinal);
        var caretIndex = stripped.IndexOf('^', StringComparison.Ordinal);
        Assert.True(errorLineIndex >= 0, "上下文应包含出错的那一行");
        Assert.True(caretIndex > errorLineIndex, "脱字符应出现在出错行之后");
    }

    [Fact]
    public void SyntaxError_ContextWindowStartLine_MatchesFirstRenderedLine()
    {
        const string code = "a <- 1\nb <- 2\nc <- 3\nd <- ) + 4\ne <- 5\n";

        var error = ParseFailure(code);
        var numbers = RenderedLineNumbers(error.Message);

        Assert.True(error.SourceContextStartLine > 0, "解析器应回传上下文窗口的起始行号");
        Assert.Equal(error.SourceContextStartLine, numbers[0]);
    }

    [Fact]
    public void SyntaxError_CaretIsIndentedToTokenColumn()
    {
        // 第三行第 6 个字符（1 起始）是 ')'，脱字符必须正对它
        const string code = "a <- 1\nb <- 2\nz <- ) + 3\n";

        var error = ParseFailure(code);
        var stripped = StripAnsi(error.Message);

        Assert.Equal(3, error.Position.Line);

        var lines = stripped.Split('\n');
        var caretLineIndex = Array.FindIndex(lines, l => l.Contains('^'));
        Assert.True(caretLineIndex > 0, "应渲染出脱字符");

        // 脱字符所在行形如 "      |      ^ 错误发生在这里"：
        // '|' 之后还有一个空格才是源码列的起点，故再减去 2
        var caretLine = lines[caretLineIndex];
        var barIndex = caretLine.IndexOf('|');
        var indent = caretLine.IndexOf('^') - barIndex - 2;

        Assert.Equal(error.Position.Column, indent);
    }

    [Fact]
    public void SyntaxError_DisplayedPosition_UsesOneBasedColumn()
    {
        // 第 3 行的 ')' 位于 0 起始下标 5，展示时应为第 6 列
        const string code = "a <- 1\nb <- 2\nz <- ) + 3\n";

        var error = ParseFailure(code);

        Assert.Equal(6, error.Position.Column + 1);
        Assert.Contains("3:6", StripAnsi(error.Message));
    }
}
