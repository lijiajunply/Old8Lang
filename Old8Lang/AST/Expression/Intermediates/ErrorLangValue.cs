using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 错误类
/// </summary>
/// <param name="value"></param>
public class ErrorLangValue(Old8Exception value) : LangValueType
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
    public string FriendlyMessage => value.Message.Split('\n')[0];

    public override string ToString()
    {
        const string reset = "\u001b[0m";
        const string red = "\u001b[31m";
        const string yellow = "\u001b[33m";
        const string blue = "\u001b[34m";
        const string green = "\u001b[32m";
        const string cyan = "\u001b[36m";
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
    public string Position => value.Position.ToString();
    
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
    /// 支持属性访问，如 e.FriendlyMessage
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression)
    {
        if (dotExpression is LangId langId)
        {
            var propertyName = langId.IdName;
            
            switch (propertyName)
            {
                case "FriendlyMessage":
                    return new StringLangValue(FriendlyMessage);
                case "Exception":
                    return this;
                case "StackTrace":
                    return new StringLangValue(string.Join("\n", StackTrace));
                case "Message":
                    return new StringLangValue(value.Message);
                case "ErrorCode":
                    return new StringLangValue(value.ErrorCode);
                case "Type":
                    return new StringLangValue(Type);
                case "Position":
                    return new StringLangValue(Position);
                case "Suggestion":
                    return Suggestion != null ? new StringLangValue(Suggestion) : new NullLangValue();
                case "Timestamp":
                    return new StringLangValue(Timestamp.ToString());
                case "RequestId":
                    return new StringLangValue(RequestId);
                default:
                    throw new Old8Exception(
                        "ATTRIBUTE_ERROR",
                        $"类型 'ErrorLangValue' 没有属性 '{propertyName}'",
                        langId.Position,
                        langId,
                        "请检查属性名称是否正确");
            }
        }
        
        return base.Dot(dotExpression);
    }
}