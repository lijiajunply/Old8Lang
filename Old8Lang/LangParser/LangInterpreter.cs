using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.LangParser;

public class LangInterpreter : IMiniInterpreter
{
    public readonly VariateManager Manager = new();

    /// <summary>
    /// 源代码
    /// </summary>
    public string? SourceCode { get; private set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string? FileName { get; private set; }

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
        FileName = fileName;
        Manager.FileName = fileName;

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
            return Array.Empty<string>();
        }

        var lines = SourceCode.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var contextLines = new List<string>();

        // 获取错误行前后的上下文，最多显示3行上下文
        // 确保行号至少为0，避免负数行号导致的问题
        var safeLine = Math.Max(0, position.Line);
        int startLine = Math.Max(0, safeLine - 2);
        int endLine = Math.Min(lines.Length - 1, safeLine + 1);

        for (int i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }

        return contextLines.ToArray();
    }

    public AbsUseClass UseClass { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }
}