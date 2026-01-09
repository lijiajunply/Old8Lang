using Old8Lang.AST;
using System.Text.RegularExpressions;

namespace Old8Lang.LangParser;

/// <summary>
/// 文档注释解析器
/// 支持解析多种文档注释风格：
/// - Google Style (Args:, Returns:)
/// - NumPy Style (Parameters, Returns with dashes)
/// - Sphinx Style (:param, :type, :return:, :rtype:)
/// - JavaDoc Style (@param, @return, @throws)
/// - 中文风格 (参数:, 返回:, 异常:)
/// </summary>
public static class DocCommentParser
{
    /// <summary>
    /// 解析文档注释字符串为结构化信息
    /// </summary>
    /// <param name="rawComment">原始文档注释文本（已去除 /// 前缀）</param>
    /// <returns>结构化的文档注释信息</returns>
    public static DocCommentInfo Parse(string rawComment)
    {
        if (string.IsNullOrWhiteSpace(rawComment))
        {
            return new DocCommentInfo();
        }

        var docInfo = new DocCommentInfo
        {
            RawText = rawComment
        };

        // 按行分割 - 先用trimmed版本检测风格，然后针对不同风格使用不同的行处理
        var linesTrimmed = rawComment.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToList();

        var linesPreserveIndent = rawComment.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (linesTrimmed.Count == 0)
        {
            return docInfo;
        }

        // 检测文档风格并解析
        var style = DetectStyle(linesTrimmed);

        switch (style)
        {
            case DocStyle.Google:
                ParseGoogleStyle(linesTrimmed, docInfo);
                break;
            case DocStyle.Sphinx:
                ParseSphinxStyle(linesTrimmed, docInfo);
                break;
            case DocStyle.JavaDoc:
                ParseJavaDocStyle(linesTrimmed, docInfo);
                break;
            case DocStyle.Chinese:
                ParseChineseStyle(linesTrimmed, docInfo);
                break;
            default:
                // 默认解析：将所有内容作为摘要
                ParseDefaultStyle(linesTrimmed, docInfo);
                break;
        }

        return docInfo;
    }

    /// <summary>
    /// 检测文档注释风格
    /// </summary>
    private static DocStyle DetectStyle(List<string> lines)
    {
        var allText = string.Join(" ", lines).ToLower();

        // 检测 JavaDoc 风格 (@param, @return, @throws)
        if (Regex.IsMatch(allText, @"@param|@return|@throws"))
        {
            return DocStyle.JavaDoc;
        }

        // 检测 Sphinx 风格 (:param, :type:, :return:, :rtype:)
        if (Regex.IsMatch(allText, @":param|:type:|:return:|:rtype:"))
        {
            return DocStyle.Sphinx;
        }

        // 检测 Google 风格 (Args:, Returns:)
        if (Regex.IsMatch(allText, @"\bargs:|\breturns:", RegexOptions.IgnoreCase))
        {
            return DocStyle.Google;
        }

        // 检测中文风格 (参数:, 返回:, 异常:)
        if (Regex.IsMatch(allText, @"参数[:：]|返回[:：]|异常[:：]"))
        {
            return DocStyle.Chinese;
        }

        return DocStyle.Default;
    }

    /// <summary>
    /// 解析 Google Style 文档注释
    /// 格式：
    /// Summary line
    ///
    /// Args:
    ///     param_name (type): description
    ///
    /// Returns:
    ///     type: description
    /// </summary>
    private static void ParseGoogleStyle(List<string> lines, DocCommentInfo docInfo)
    {
        var currentSection = SectionType.Summary;
        var summaryLines = new List<string>();
        var currentParamLines = new List<string>();
        var returnLines = new List<string>();
        var throwsLines = new List<string>();

        foreach (var line in lines)
        {
            var lowerLine = line.ToLower();

            // 检测章节标题
            if (lowerLine.StartsWith("args:") || lowerLine.StartsWith("arguments:") ||
                lowerLine.StartsWith("parameters:"))
            {
                currentSection = SectionType.Parameters;
                continue;
            }
            else if (lowerLine.StartsWith("returns:") || lowerLine.StartsWith("return:"))
            {
                currentSection = SectionType.Returns;
                continue;
            }
            else if (lowerLine.StartsWith("raises:") || lowerLine.StartsWith("throws:"))
            {
                currentSection = SectionType.Throws;
                continue;
            }
            else if (lowerLine.StartsWith("example:") || lowerLine.StartsWith("examples:"))
            {
                currentSection = SectionType.Examples;
                continue;
            }

            // 根据当前章节处理内容
            switch (currentSection)
            {
                case SectionType.Summary:
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        summaryLines.Add(line);
                    }

                    break;

                case SectionType.Parameters:
                    currentParamLines.Add(line);
                    break;

                case SectionType.Returns:
                    returnLines.Add(line);
                    break;

                case SectionType.Throws:
                    throwsLines.Add(line);
                    break;

                case SectionType.Examples:
                    docInfo.Examples.Add(line);
                    break;
            }
        }

        // 处理摘要
        docInfo.Summary = string.Join(" ", summaryLines).Trim();

        // 处理参数
        ParseGoogleStyleParameters(currentParamLines, docInfo);

        // 处理返回值
        ParseGoogleStyleReturns(returnLines, docInfo);

        // 处理异常
        ParseGoogleStyleThrows(throwsLines, docInfo);
    }

    /// <summary>
    /// 解析 Google Style 的参数
    /// 格式：param_name (type): description
    /// </summary>
    private static void ParseGoogleStyleParameters(List<string> lines, DocCommentInfo docInfo)
    {
        // 匹配：name (type): description 或 name: description
        var paramPattern = @"^\s*(\w+)\s*(?:\(([^)]+)\))?\s*:\s*(.+)$";

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = Regex.Match(line, paramPattern);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var type = match.Groups[2].Success ? match.Groups[2].Value : null;
                var description = match.Groups[3].Value.Trim();

                docInfo.Parameters.Add(new ParameterDoc(name, type, description));
            }
        }
    }

    /// <summary>
    /// 解析 Google Style 的返回值
    /// 格式：type: description
    /// </summary>
    private static void ParseGoogleStyleReturns(List<string> lines, DocCommentInfo docInfo)
    {
        var returnText = string.Join(" ", lines).Trim();
        if (string.IsNullOrWhiteSpace(returnText))
            return;

        // 匹配：type: description
        var match = Regex.Match(returnText, @"^\s*(\w+)\s*:\s*(.+)$");
        if (match.Success)
        {
            var type = match.Groups[1].Value;
            var description = match.Groups[2].Value.Trim();
            docInfo.Returns = new ReturnDoc(type, description);
        }
        else
        {
            docInfo.Returns = new ReturnDoc(null, returnText);
        }
    }

    /// <summary>
    /// 解析 Google Style 的异常
    /// 格式：ExceptionType: description
    /// </summary>
    private static void ParseGoogleStyleThrows(List<string> lines, DocCommentInfo docInfo)
    {
        var throwsPattern = @"^\s*(\w+)\s*:\s*(.+)$";

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = Regex.Match(line, throwsPattern);
            if (match.Success)
            {
                var type = match.Groups[1].Value;
                var description = match.Groups[2].Value.Trim();
                docInfo.Throws.Add(new ThrowsDoc(type, description));
            }
        }
    }

    /// <summary>
    /// 解析 Sphinx Style (reStructuredText) 文档注释
    /// 格式：
    /// Summary line
    ///
    /// :param name: description
    /// :type name: type
    /// :return: description
    /// :rtype: type
    /// </summary>
    private static void ParseSphinxStyle(List<string> lines, DocCommentInfo docInfo)
    {
        var summaryLines = new List<string>();
        var paramDescriptions = new Dictionary<string, string>();
        var paramTypes = new Dictionary<string, string>();
        string? returnDescription = null;
        string? returnType = null;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // :param name: description
            var paramMatch = Regex.Match(line, @"^:param\s+(\w+)\s*:\s*(.+)$");
            if (paramMatch.Success)
            {
                var name = paramMatch.Groups[1].Value;
                var description = paramMatch.Groups[2].Value.Trim();
                paramDescriptions[name] = description;
                continue;
            }

            // :type name: type
            var typeMatch = Regex.Match(line, @"^:type\s+(\w+)\s*:\s*(.+)$");
            if (typeMatch.Success)
            {
                var name = typeMatch.Groups[1].Value;
                var type = typeMatch.Groups[2].Value.Trim();
                paramTypes[name] = type;
                continue;
            }

            // :return: description
            var returnMatch = Regex.Match(line, @"^:return\s*:\s*(.+)$");
            if (returnMatch.Success)
            {
                returnDescription = returnMatch.Groups[1].Value.Trim();
                continue;
            }

            // :rtype: type
            var rtypeMatch = Regex.Match(line, @"^:rtype\s*:\s*(.+)$");
            if (rtypeMatch.Success)
            {
                returnType = rtypeMatch.Groups[1].Value.Trim();
                continue;
            }

            // 非标记行作为摘要
            if (!line.StartsWith(":"))
            {
                summaryLines.Add(line);
            }
        }

        // 处理摘要
        docInfo.Summary = string.Join(" ", summaryLines).Trim();

        // 处理参数
        foreach (var paramName in paramDescriptions.Keys)
        {
            var description = paramDescriptions[paramName];
            var type = paramTypes.GetValueOrDefault(paramName);
            docInfo.Parameters.Add(new ParameterDoc(paramName, type, description));
        }

        // 处理返回值
        if (returnDescription is not null || returnType is not null)
        {
            docInfo.Returns = new ReturnDoc(returnType, returnDescription ?? string.Empty);
        }
    }

    /// <summary>
    /// 解析 JavaDoc Style 文档注释
    /// 格式：
    /// Summary line
    ///
    /// @param name description
    /// @return description
    /// @throws ExceptionType description
    /// </summary>
    private static void ParseJavaDocStyle(List<string> lines, DocCommentInfo docInfo)
    {
        var summaryLines = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // @param name description
            var paramMatch = Regex.Match(line, @"^@param\s+(\w+)\s+(.+)$");
            if (paramMatch.Success)
            {
                var name = paramMatch.Groups[1].Value;
                var description = paramMatch.Groups[2].Value.Trim();
                docInfo.Parameters.Add(new ParameterDoc(name, null, description));
                continue;
            }

            // @return description
            var returnMatch = Regex.Match(line, @"^@return\s+(.+)$");
            if (returnMatch.Success)
            {
                var description = returnMatch.Groups[1].Value.Trim();
                docInfo.Returns = new ReturnDoc(null, description);
                continue;
            }

            // @throws ExceptionType description
            var throwsMatch = Regex.Match(line, @"^@throws\s+(\w+)\s+(.+)$");
            if (throwsMatch.Success)
            {
                var type = throwsMatch.Groups[1].Value;
                var description = throwsMatch.Groups[2].Value.Trim();
                docInfo.Throws.Add(new ThrowsDoc(type, description));
                continue;
            }

            // 非标记行作为摘要
            if (!line.StartsWith("@"))
            {
                summaryLines.Add(line);
            }
        }

        // 处理摘要
        docInfo.Summary = string.Join(" ", summaryLines).Trim();
    }

    /// <summary>
    /// 解析中文风格文档注释
    /// 格式：
    /// 摘要
    ///
    /// 参数:
    ///   - name: description
    ///
    /// 返回:
    ///   description
    ///
    /// 异常:
    ///   - ExceptionType: description
    /// </summary>
    private static void ParseChineseStyle(List<string> lines, DocCommentInfo docInfo)
    {
        var currentSection = SectionType.Summary;
        var summaryLines = new List<string>();
        var paramLines = new List<string>();
        var returnLines = new List<string>();
        var throwsLines = new List<string>();

        foreach (var line in lines)
        {
            // 检测章节标题
            if (Regex.IsMatch(line, @"^参数[:：]"))
            {
                currentSection = SectionType.Parameters;
                continue;
            }
            else if (Regex.IsMatch(line, @"^返回[:：]"))
            {
                currentSection = SectionType.Returns;
                continue;
            }
            else if (Regex.IsMatch(line, @"^异常[:：]|^错误[:：]|^抛出[:：]"))
            {
                currentSection = SectionType.Throws;
                continue;
            }
            else if (Regex.IsMatch(line, @"^示例[:：]"))
            {
                currentSection = SectionType.Examples;
                continue;
            }

            // 根据当前章节处理内容
            switch (currentSection)
            {
                case SectionType.Summary:
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        summaryLines.Add(line);
                    }

                    break;

                case SectionType.Parameters:
                    paramLines.Add(line);
                    break;

                case SectionType.Returns:
                    returnLines.Add(line);
                    break;

                case SectionType.Throws:
                    throwsLines.Add(line);
                    break;

                case SectionType.Examples:
                    docInfo.Examples.Add(line);
                    break;
            }
        }

        // 处理摘要
        docInfo.Summary = string.Join(" ", summaryLines).Trim();

        // 处理参数
        ParseChineseStyleParameters(paramLines, docInfo);

        // 处理返回值
        var returnText = string.Join(" ", returnLines).Trim();
        if (!string.IsNullOrWhiteSpace(returnText))
        {
            docInfo.Returns = new ReturnDoc(null, returnText);
        }

        // 处理异常
        ParseChineseStyleThrows(throwsLines, docInfo);
    }

    /// <summary>
    /// 解析中文风格的参数
    /// 格式：- name: description 或 - name（type）：description
    /// </summary>
    private static void ParseChineseStyleParameters(List<string> lines, DocCommentInfo docInfo)
    {
        // 匹配：- name: description 或 - name（type）：description
        var paramPattern = @"^\s*-\s*(\w+)\s*(?:[\(（]([^\)）]+)[\)）])?\s*[:：]\s*(.+)$";

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = Regex.Match(line, paramPattern);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var type = match.Groups[2].Success ? match.Groups[2].Value : null;
                var description = match.Groups[3].Value.Trim();

                docInfo.Parameters.Add(new ParameterDoc(name, type, description));
            }
        }
    }

    /// <summary>
    /// 解析中文风格的异常
    /// 格式：- ExceptionType: description
    /// </summary>
    private static void ParseChineseStyleThrows(List<string> lines, DocCommentInfo docInfo)
    {
        var throwsPattern = @"^\s*-\s*(\w+)\s*[:：]\s*(.+)$";

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = Regex.Match(line, throwsPattern);
            if (match.Success)
            {
                var type = match.Groups[1].Value;
                var description = match.Groups[2].Value.Trim();
                docInfo.Throws.Add(new ThrowsDoc(type, description));
            }
        }
    }

    /// <summary>
    /// 默认解析风格：将所有内容作为摘要
    /// </summary>
    private static void ParseDefaultStyle(List<string> lines, DocCommentInfo docInfo)
    {
        docInfo.Summary = string.Join(" ", lines).Trim();
    }

    /// <summary>
    /// 文档风格枚举
    /// </summary>
    private enum DocStyle
    {
        Default,
        Google,
        Sphinx,
        JavaDoc,
        Chinese
    }

    /// <summary>
    /// 章节类型枚举
    /// </summary>
    private enum SectionType
    {
        Summary,
        Parameters,
        Returns,
        Throws,
        Examples
    }
}