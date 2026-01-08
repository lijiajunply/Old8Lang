using System.Collections.Generic;
using System.Text;

namespace Old8Lang.LangParser;

/// <summary>
/// 预编译指令处理器，在词法分析阶段处理 #if、#define 等预编译指令
/// </summary>
public class PreprocessorTokenizer
{
    /// <summary>
    /// 输入源代码
    /// </summary>
    private readonly string _input;

    /// <summary>
    /// 当前扫描位置
    /// </summary>
    private int _currentIndex;

    /// <summary>
    /// 预编译符号管理器
    /// </summary>
    private readonly PreprocessorSymbols _symbols;

    /// <summary>
    /// 当前行号
    /// </summary>
    private int _line;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="input">源代码</param>
    /// <param name="symbols">预编译符号管理器</param>
    public PreprocessorTokenizer(string input, PreprocessorSymbols symbols)
    {
        _input = input;
        _currentIndex = 0;
        _symbols = symbols;
        _line = 1;
    }

    /// <summary>
    /// 处理预编译指令，返回处理后的代码
    /// </summary>
    /// <returns>移除未激活代码块后的源代码</returns>
    public string Process()
    {
        var result = new StringBuilder();
        // (isActive: 当前块是否激活, anyBranchTaken: 在当前 if/elif/else 块中是否已有分支被激活)
        var conditionStack = new Stack<(bool isActive, bool anyBranchTaken)>();

        bool inString = false;
        bool inChar = false;
        bool inComment = false;
        bool inMultiLineComment = false;
        bool escapeNext = false;

        while (_currentIndex < _input.Length)
        {
            var ch = _input[_currentIndex];

            // 处理转义字符
            if (escapeNext)
            {
                bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
                if (isActive)
                {
                    result.Append(ch);
                }
                escapeNext = false;
                if (ch == '\n')
                {
                    _line++;
                }
                _currentIndex++;
                continue;
            }

            // 检查转义开始
            if ((inString || inChar) && ch == '\\')
            {
                bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
                if (isActive)
                {
                    result.Append(ch);
                }
                escapeNext = true;
                _currentIndex++;
                continue;
            }

            // 检查字符串开始/结束
            if (ch == '"' && !inChar && !inComment && !inMultiLineComment)
            {
                inString = !inString;
                bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
                if (isActive)
                {
                    result.Append(ch);
                }
                _currentIndex++;
                continue;
            }

            // 检查字符开始/结束
            if (ch == '\'' && !inString && !inComment && !inMultiLineComment)
            {
                inChar = !inChar;
                bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
                if (isActive)
                {
                    result.Append(ch);
                }
                _currentIndex++;
                continue;
            }

            // 检查多行注释结束
            if (inMultiLineComment && ch == '*' && _currentIndex + 1 < _input.Length && _input[_currentIndex + 1] == '/')
            {
                inMultiLineComment = false;
                bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
                if (isActive)
                {
                    result.Append("*/");
                }
                _currentIndex += 2;
                continue;
            }

            // 检查注释开始
            if (!inString && !inChar && !inComment && !inMultiLineComment && ch == '/')
            {
                if (_currentIndex + 1 < _input.Length)
                {
                    if (_input[_currentIndex + 1] == '/')
                    {
                        // 单行注释
                        inComment = true;
                        bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
                        if (isActive)
                        {
                            result.Append("//");
                        }
                        _currentIndex += 2;
                        continue;
                    }
                    else if (_input[_currentIndex + 1] == '*')
                    {
                        // 多行注释
                        inMultiLineComment = true;
                        bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
                        if (isActive)
                        {
                            result.Append("/*");
                        }
                        _currentIndex += 2;
                        continue;
                    }
                }
            }

            // 检查单行注释结束
            if (inComment && ch == '\n')
            {
                inComment = false;
            }

            // 检查是否是预编译指令（以 # 开头，且不在字符串/注释中）
            if (ch == '#' && !inString && !inChar && !inComment && !inMultiLineComment && IsAtLineStart())
            {
                ProcessDirective(result, conditionStack);
            }
            else
            {
                // 检查当前是否在激活的代码块中
                bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);

                if (isActive)
                {
                    result.Append(ch);
                }
                else
                {
                    // 未激活的代码块，只保留换行符以保持行号准确
                    if (ch == '\n')
                    {
                        result.Append('\n');
                    }
                }

                if (ch == '\n')
                {
                    _line++;
                }

                _currentIndex++;
            }
        }

        // 检查是否有未闭合的 #if
        if (conditionStack.Count > 0)
        {
            throw new Error.SyntaxError(
                "#if",
                _line,
                0,
                $"预编译指令错误：存在未闭合的 #if 指令，缺少 #endif");
        }

        return result.ToString();
    }

    /// <summary>
    /// 检查当前位置是否在行首（忽略空格和制表符）
    /// </summary>
    private bool IsAtLineStart()
    {
        // 向前查找，直到遇到换行符或非空白字符
        for (int i = _currentIndex - 1; i >= 0; i--)
        {
            if (_input[i] == '\n')
            {
                return true;
            }
            if (_input[i] != ' ' && _input[i] != '\t' && _input[i] != '\r')
            {
                return false;
            }
        }
        return true; // 文件开头
    }

    /// <summary>
    /// 处理预编译指令
    /// </summary>
    private void ProcessDirective(StringBuilder result, Stack<(bool isActive, bool anyBranchTaken)> conditionStack)
    {
        _currentIndex++; // 跳过 #

        // 读取指令名称
        var directiveName = ReadDirectiveName();

        switch (directiveName)
        {
            case "define":
                ProcessDefine(conditionStack);
                break;

            case "undef":
                ProcessUndef(conditionStack);
                break;

            case "if":
                ProcessIf(conditionStack);
                break;

            case "elif":
                ProcessElif(conditionStack);
                break;

            case "else":
                ProcessElse(conditionStack);
                break;

            case "endif":
                ProcessEndif(conditionStack);
                break;

            default:
                throw new Error.SyntaxError(
                    $"#{directiveName}",
                    _line,
                    0,
                    $"预编译指令错误：未知的预编译指令 '#{directiveName}'");
        }

        // 跳过指令行的剩余部分直到换行
        SkipToEndOfLine();
        result.Append('\n'); // 保留换行符以保持行号
        _line++;
    }

    /// <summary>
    /// 读取指令名称
    /// </summary>
    private string ReadDirectiveName()
    {
        var sb = new StringBuilder();

        // 跳过空格
        while (_currentIndex < _input.Length && (_input[_currentIndex] == ' ' || _input[_currentIndex] == '\t'))
        {
            _currentIndex++;
        }

        // 读取指令名称
        while (_currentIndex < _input.Length && char.IsLetter(_input[_currentIndex]))
        {
            sb.Append(_input[_currentIndex]);
            _currentIndex++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// 读取指令参数（到行尾）
    /// </summary>
    private string ReadDirectiveArgument()
    {
        var sb = new StringBuilder();

        // 跳过空格
        while (_currentIndex < _input.Length && (_input[_currentIndex] == ' ' || _input[_currentIndex] == '\t'))
        {
            _currentIndex++;
        }

        // 读取参数直到换行
        while (_currentIndex < _input.Length && _input[_currentIndex] != '\n' && _input[_currentIndex] != '\r')
        {
            sb.Append(_input[_currentIndex]);
            _currentIndex++;
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// 跳过到行尾
    /// </summary>
    private void SkipToEndOfLine()
    {
        while (_currentIndex < _input.Length && _input[_currentIndex] != '\n')
        {
            _currentIndex++;
        }
    }

    /// <summary>
    /// 处理 #define 指令
    /// </summary>
    private void ProcessDefine(Stack<(bool isActive, bool anyBranchTaken)> conditionStack)
    {
        // 只有在激活的代码块中才执行 #define
        bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);

        var symbolName = ReadDirectiveArgument();

        if (string.IsNullOrWhiteSpace(symbolName))
        {
            throw new Error.SyntaxError(
                "#define",
                _line,
                0,
                "预编译指令错误：#define 指令缺少符号名称");
        }

        if (isActive)
        {
            _symbols.DefineSymbol(symbolName);
        }
    }

    /// <summary>
    /// 处理 #undef 指令
    /// </summary>
    private void ProcessUndef(Stack<(bool isActive, bool anyBranchTaken)> conditionStack)
    {
        // 只有在激活的代码块中才执行 #undef
        bool isActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);

        var symbolName = ReadDirectiveArgument();

        if (string.IsNullOrWhiteSpace(symbolName))
        {
            throw new Error.SyntaxError(
                "#undef",
                _line,
                0,
                "预编译指令错误：#undef 指令缺少符号名称");
        }

        if (isActive)
        {
            _symbols.UndefineSymbol(symbolName);
        }
    }

    /// <summary>
    /// 处理 #if 指令
    /// </summary>
    private void ProcessIf(Stack<(bool isActive, bool anyBranchTaken)> conditionStack)
    {
        var condition = ReadDirectiveArgument();

        if (string.IsNullOrWhiteSpace(condition))
        {
            throw new Error.SyntaxError(
                "#if",
                _line,
                0,
                "预编译指令错误：#if 指令缺少条件表达式");
        }

        // 只有在父级激活时才计算条件
        bool parentActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
        bool conditionResult = parentActive && _symbols.EvaluateCondition(condition);

        // 如果条件为真，则当前分支激活，且已有分支被激活
        conditionStack.Push((conditionResult, conditionResult));
    }

    /// <summary>
    /// 处理 #elif 指令
    /// </summary>
    private void ProcessElif(Stack<(bool isActive, bool anyBranchTaken)> conditionStack)
    {
        if (conditionStack.Count == 0)
        {
            throw new Error.SyntaxError(
                "#elif",
                _line,
                0,
                "预编译指令错误：#elif 指令没有对应的 #if");
        }

        var condition = ReadDirectiveArgument();

        if (string.IsNullOrWhiteSpace(condition))
        {
            throw new Error.SyntaxError(
                "#elif",
                _line,
                0,
                "预编译指令错误：#elif 指令缺少条件表达式");
        }

        // 弹出当前 #if/#elif 的状态
        var (_, anyBranchTaken) = conditionStack.Pop();

        // 只有在父级激活且前面的分支都未激活时，才计算当前条件
        bool parentActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
        bool conditionResult = parentActive && !anyBranchTaken && _symbols.EvaluateCondition(condition);

        // 如果当前条件为真，或者之前有分支已经被激活，则 anyBranchTaken 应该为 true
        conditionStack.Push((conditionResult, anyBranchTaken || conditionResult));
    }

    /// <summary>
    /// 处理 #else 指令
    /// </summary>
    private void ProcessElse(Stack<(bool isActive, bool anyBranchTaken)> conditionStack)
    {
        if (conditionStack.Count == 0)
        {
            throw new Error.SyntaxError(
                "#else",
                _line,
                0,
                "预编译指令错误：#else 指令没有对应的 #if");
        }

        // 弹出当前 #if/#elif 的状态
        var (_, anyBranchTaken) = conditionStack.Pop();

        // #else 块只有在父级激活且前面的分支都未激活时才激活
        bool parentActive = conditionStack.Count == 0 || conditionStack.All(x => x.isActive);
        bool elseActive = parentActive && !anyBranchTaken;

        // 如果 #else 激活，或者之前有分支已经被激活，则 anyBranchTaken 应该为 true
        conditionStack.Push((elseActive, anyBranchTaken || elseActive));
    }

    /// <summary>
    /// 处理 #endif 指令
    /// </summary>
    private void ProcessEndif(Stack<(bool isActive, bool anyBranchTaken)> conditionStack)
    {
        if (conditionStack.Count == 0)
        {
            throw new Error.SyntaxError(
                "#endif",
                _line,
                0,
                "预编译指令错误：#endif 指令没有对应的 #if");
        }

        conditionStack.Pop();
    }
}
