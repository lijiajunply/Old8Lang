using Old8Lang.AST.Statement;
using Old8Lang.Error;

namespace Old8Lang.LangParser;

public class LangInterpreter
{
    public readonly VariateManager Manager = new();

    /// <summary>
    /// 源代码
    /// </summary>
    private string? SourceCode { get; set; }

    public LangInterpreter()
    {
        Manager.Interpreter = this;
        Manager.LangInfo ??= Apis.ReadJson();
    }

    // 实现接口方法
    public BlockStatement Build(string code)
    {
        return Build(code, null);
    }

    // 重载方法，支持传递文件名
    public BlockStatement Build(string code, string? fileName)
    {
        SourceCode = code;
        Manager.Path = fileName ?? "";

        // 设置当前解释器，以便在错误处理中使用
        Old8Exception.CurrentInterpreter = this;

        var parser = LangTokenizer.Tokenize(code);
        if (parser == null) throw new SyntaxError(new SourcePosition(1, 1), "语法出错");
        //parser.ForEach(x => Console.WriteLine(x));
        var result = new LangParser(parser, code, fileName).ParseProgram();

        // 清除当前解释器
        // Old8Exception.CurrentInterpreter = null;

        return result;
    }

    public static List<LangToken> Tokenize(string code)
    {
        return LangTokenizer.Tokenize(code);
    }

    /// <summary>
    /// 获取错误位置附近的源代码上下文
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <returns>错误位置附近的源代码上下文</returns>
    public string[] GetSourceContext(SourcePosition position)
    {
        if (string.IsNullOrEmpty(SourceCode))
        {
            return [];
        }

        var lines = SourceCode.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var contextLines = new List<string>();

        // 获取错误行前后的上下文，最多显示3行上下文
        // 确保行号至少为1，然后转换为0-based索引
        var safeLine = Math.Max(1, position.Line);
        // 转换为0-based索引
        var zeroBasedLine = safeLine - 1;
        var startLine = Math.Max(0, zeroBasedLine - 2);
        var endLine = Math.Min(lines.Length - 1, zeroBasedLine + 1);

        for (var i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }

        return contextLines.ToArray();
    }

    public AbsUseClass OutputProvider { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }
}