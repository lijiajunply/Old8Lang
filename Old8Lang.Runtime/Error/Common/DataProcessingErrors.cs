using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 迭代器停止错误
/// </summary>
public class StopIterationError : RuntimeError
{
    /// <summary>
    /// 迭代器停止错误代码
    /// </summary>
    public new const string ErrorCode = "STOP_ITERATION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    public StopIterationError(IOldLangTree node)
        : base(
            node,
            ErrorCode,
            "迭代器已到达末尾",
            "请检查迭代器是否还有更多元素")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public StopIterationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查迭代器是否还有更多元素")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    public StopIterationError(SourcePosition position)
        : base(
            position,
            ErrorCode,
            "迭代器已到达末尾",
            "请检查迭代器是否还有更多元素")
    {
    }
}

/// <summary>
/// 编码错误
/// </summary>
public class EncodingError : RuntimeError
{
    /// <summary>
    /// 编码错误代码
    /// </summary>
    public new const string ErrorCode = "ENCODING_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="encoding">编码名称</param>
    public EncodingError(IOldLangTree node, string encoding)
        : base(
            node,
            ErrorCode,
            $"不支持的编码: '{encoding}'",
            "请使用支持的编码格式，如 UTF-8, ASCII, GB2312 等")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    public EncodingError(IOldLangTree node, string message, string suggestion)
        : base(
            node,
            ErrorCode,
            message,
            suggestion)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="encoding">编码名称</param>
    public EncodingError(SourcePosition position, string encoding)
        : base(
            position,
            ErrorCode,
            $"不支持的编码: '{encoding}'",
            "请使用支持的编码格式，如 UTF-8, ASCII, GB2312 等")
    {
    }
}

/// <summary>
/// 解码错误
/// </summary>
public class DecodeError : RuntimeError
{
    /// <summary>
    /// 解码错误代码
    /// </summary>
    public new const string ErrorCode = "DECODE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="encoding">编码名称</param>
    public DecodeError(IOldLangTree node, string encoding)
        : base(
            node,
            ErrorCode,
            $"无法使用 '{encoding}' 编码解码数据",
            "请检查数据的编码格式是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="encoding">编码名称</param>
    public DecodeError(SourcePosition position, string encoding)
        : base(
            position,
            ErrorCode,
            $"无法使用 '{encoding}' 编码解码数据",
            "请检查数据的编码格式是否正确")
    {
    }
}

/// <summary>
/// JSON 解析错误
/// </summary>
public class JsonParseError : RuntimeError
{
    /// <summary>
    /// JSON 解析错误代码
    /// </summary>
    public new const string ErrorCode = "JSON_PARSE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public JsonParseError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            $"JSON 解析错误: {message}",
            "请检查 JSON 格式是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="position">JSON 中的位置</param>
    /// <param name="message">错误信息</param>
    public JsonParseError(IOldLangTree node, int position, string message)
        : base(
            node,
            ErrorCode,
            $"JSON 解析错误 (位置 {position}): {message}",
            "请检查 JSON 格式是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sourcePosition">源代码位置信息</param>
    /// <param name="message">错误信息</param>
    public JsonParseError(SourcePosition sourcePosition, string message)
        : base(
            sourcePosition,
            ErrorCode,
            $"JSON 解析错误: {message}",
            "请检查 JSON 格式是否正确")
    {
    }
}

/// <summary>
/// 正则表达式错误
/// </summary>
public class RegexError : RuntimeError
{
    /// <summary>
    /// 正则表达式错误代码
    /// </summary>
    public new const string ErrorCode = "REGEX_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="message">错误信息</param>
    public RegexError(IOldLangTree node, string pattern, string message)
        : base(
            node,
            ErrorCode,
            $"正则表达式 '{pattern}' 错误: {message}",
            "请检查正则表达式语法是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public RegexError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查正则表达式语法是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="message">错误信息</param>
    public RegexError(SourcePosition position, string pattern, string message)
        : base(
            position,
            ErrorCode,
            $"正则表达式 '{pattern}' 错误: {message}",
            "请检查正则表达式语法是否正确")
    {
    }
}

/// <summary>
/// 序列化错误
/// </summary>
public class SerializationError : RuntimeError
{
    /// <summary>
    /// 序列化错误代码
    /// </summary>
    public new const string ErrorCode = "SERIALIZATION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="typeName">类型名称</param>
    /// <param name="message">错误信息</param>
    public SerializationError(IOldLangTree node, string typeName, string message)
        : base(
            node,
            ErrorCode,
            $"无法序列化类型 '{typeName}': {message}",
            "请检查对象是否可序列化")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public SerializationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查对象是否可序列化")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="typeName">类型名称</param>
    /// <param name="message">错误信息</param>
    public SerializationError(SourcePosition position, string typeName, string message)
        : base(
            position,
            ErrorCode,
            $"无法序列化类型 '{typeName}': {message}",
            "请检查对象是否可序列化")
    {
    }
}

/// <summary>
/// 反序列化错误
/// </summary>
public class DeserializationError : RuntimeError
{
    /// <summary>
    /// 反序列化错误代码
    /// </summary>
    public new const string ErrorCode = "DESERIALIZATION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="typeName">类型名称</param>
    /// <param name="message">错误信息</param>
    public DeserializationError(IOldLangTree node, string typeName, string message)
        : base(
            node,
            ErrorCode,
            $"无法反序列化为类型 '{typeName}': {message}",
            "请检查数据格式是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public DeserializationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查数据格式是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="typeName">类型名称</param>
    /// <param name="message">错误信息</param>
    public DeserializationError(SourcePosition position, string typeName, string message)
        : base(
            position,
            ErrorCode,
            $"无法反序列化为类型 '{typeName}': {message}",
            "请检查数据格式是否正确")
    {
    }
}
