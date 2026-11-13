using System.Globalization;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 错误类
/// </summary>
/// <param name="value"></param>
public partial class ErrorLangValue(Old8Exception value) : LangValueType
{
    /// <summary>
    /// 错误异常对象
    /// </summary>
    public Old8Exception Exception => value;

    /// <summary>
    /// 获取异常的调用栈
    /// </summary>
    public List<CallStackFrame> StackTrace => value.CallStack;

    /// <summary>
    /// 获取友好的错误信息
    /// </summary>
    public string FriendlyMessage => value.FriendlyMessage;

    /// <summary>
    /// 类型名用异常类名（如 <c>ZeroDivisionError</c>），供 <c>TypeOf(e)</c> 判断错误种类。
    /// </summary>
    public override string TypeToString() => value.GetType().Name;

    public override string ToString()
    {
        const string reset = "\e[0m";
        const string red = "\e[31m";
        const string yellow = "\e[33m";
        const string blue = "\e[34m";
        const string green = "\e[32m";
        const string cyan = "\e[36m";
        StringBuilder sb = new();

        // 错误标题和消息
        sb.AppendLine($"{red}[{value.ErrorCode}]{reset} {value.Message.Split('\n')[0]}");

        // 错误位置
        sb.AppendLine($"{yellow}位置:{reset} {value.Position}");

        // 源代码上下文
        if (value.SourceContext is { Length: > 0 })
        {
            sb.AppendLine($"{blue}上下文:{reset}");
            foreach (var line in value.SourceContext)
            {
                sb.AppendLine($"  {line}");
            }
        }

        // 调用栈信息
        if (value.CallStack.Count > 0)
        {
            sb.AppendLine($"{cyan}调用栈:{reset}");
            for (int i = 0; i < value.CallStack.Count; i++)
            {
                var frame = value.CallStack[i];
                var indent = new string(' ', i * 2);
                sb.AppendLine($"{indent}{frame.FunctionName} at {frame.Position}");
            }
        }

        // 建议
        if (!string.IsNullOrEmpty(value.Suggestion))
        {
            sb.AppendLine($"{green}建议:{reset} {value.Suggestion}");
        }

        // 移除最后一个换行符
        if (sb.Length > 0)
        {
            sb.Length--;
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取异常类型名称
    /// </summary>
    public string Type => value.GetType().Name;

    /// <summary>
    /// 获取异常的位置信息
    /// </summary>
    public new string Position => value.Position.ToString();

    /// <summary>
    /// 获取异常的建议
    /// </summary>
    public string? Suggestion => value.Suggestion;

    /// <summary>
    /// 获取异常的时间戳
    /// </summary>
    public DateTime Timestamp => value.Timestamp;

    /// <summary>
    /// 获取异常的请求ID
    /// </summary>
    public string RequestId => value.RequestId.ToString();

    /// <summary>
    /// 获取简短的字符串表示，用于字符串拼接和显示
    /// </summary>
    public override string ToDisplayString()
    {
        // 返回友好的错误消息，不包含格式化信息
        return FriendlyMessage;
    }

}