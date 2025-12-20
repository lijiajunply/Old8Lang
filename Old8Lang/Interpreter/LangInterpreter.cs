using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser;
using Old8Lang.TypeSystem;

namespace Old8Lang.Interpreter;

/// <summary>
/// Old8Lang 解释器核心类，负责将代码转换为抽象语法树并执行
/// </summary>
/// <remarks>
/// 解释器是Old8Lang的核心组件之一，提供了代码的词法分析、语法分析和执行功能。
/// 它支持两种运行模式：解释模式和编译模式（通过与编译器协同工作）。
/// </remarks>
public class LangInterpreter
{
    /// <summary>
    /// 变量管理器，负责管理解释器运行时的变量和作用域
    /// </summary>
    public readonly VariateManager Manager = new();

    /// <summary>
    /// 源代码
    /// </summary>
    private string? SourceCode { get; set; }

    /// <summary>
    /// 输出提供者，用于控制解释器的输出方式
    /// </summary>
    public AbsOutputProvider OutputProvider { get; set; } = new ConsoleOutputProvider();

    /// <summary>
    /// 是否启用编译优化
    /// </summary>
    public bool IsCompileOptimization { get; set; }

    /// <summary>
    /// 模块缓存，用于存储已解析的模块，提高导入性能
    /// 键为模块的绝对路径，值为解析后的BlockStatement
    /// </summary>
    public Dictionary<string, BlockStatement> ModuleCache { get; set; } = new();

    /// <summary>
    /// 初始化LangInterpreter实例
    /// </summary>
    public LangInterpreter()
    {
        Manager.Interpreter = this;
        Manager.LangInfo ??= Apis.ReadJson();

        // 初始化类型检查器
        TypeChecker.Initialize(Manager);

        // 注册全局 Task 对象
        Manager.Set(new LangId("Task"), TaskClassLangValue.GetInstance());
        Manager.Set(new LangId("Thread"), ThreadClassLangValue.GetInstance());
    }

    /// <summary>
    /// 支持传递文件名以获取更准确的错误信息
    /// </summary>
    /// <param name="code">要编译的Old8Lang代码</param>
    /// <param name="fileName">源代码文件名（可选）</param>
    /// <returns>表示整个程序的块语句</returns>
    /// <exception cref="SyntaxError">当代码语法错误时抛出</exception>
    public BlockStatement Build(string code, string? fileName = null)
    {
        SourceCode = code;
        Manager.Path = fileName ?? "";

        // 设置当前解释器，以便在错误处理中使用
        Old8Exception.CurrentInterpreter = this;

        // 词法分析：将代码转换为标记流
        var parser = LangTokenizer.Tokenize(code);
        if (parser == null) throw new SyntaxError(new SourcePosition(1, 1), "语法出错");

        // 语法分析：将标记流转换为抽象语法树
        var result = new LangParser.LangParser(parser, code, fileName).ParseProgram();

        return result;
    }

    /// <summary>
    /// 静态方法，将代码转换为标记流
    /// </summary>
    /// <param name="code">要标记化的Old8Lang代码</param>
    /// <returns>标记列表</returns>
    public static List<LangToken> Tokenize(string code)
    {
        return LangTokenizer.Tokenize(code);
    }

    /// <summary>
    /// 获取错误位置附近的源代码上下文，用于生成更友好的错误信息
    /// </summary>
    /// <param name="position">错误发生的位置信息</param>
    /// <returns>错误位置前后的源代码行数组</returns>
    public string[] GetSourceContext(SourcePosition position)
    {
        if (string.IsNullOrEmpty(SourceCode))
        {
            return [];
        }

        var lines = SourceCode.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var contextLines = new List<string>();

        // 确保行号至少为1，然后转换为0-based索引
        var safeLine = Math.Max(1, position.Line);
        var zeroBasedLine = safeLine - 1;

        // 获取错误行前后各2行，最多显示5行上下文
        var startLine = Math.Max(0, zeroBasedLine - 2);
        var endLine = Math.Min(lines.Length - 1, zeroBasedLine + 2);

        // 收集上下文行
        for (var i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }

        return contextLines.ToArray();
    }
}