using Old8Lang.Error;

namespace Old8Lang.LangParser.ParserHelpers;

/// <summary>
/// Expect 方法的错误消息生成辅助类
/// 将 Expect 方法中的详细错误消息和建议提取到独立的辅助类中
/// </summary>
public static class ExpectHelper
{
    /// <summary>
    /// 获取详细的错误消息
    /// </summary>
    public static string GetDetailedMessage(LangTokenType expectedType, LangTokenType actualType, string actualValue)
    {
        return expectedType switch
        {
            LangTokenType.RightParen => $"语法错误：缺少右括号 ')。在 '{actualValue}' 处期望右括号。",
            LangTokenType.RightBracket => $"语法错误：缺少右方括号 ']。在 '{actualValue}' 处期望右方括号。",
            LangTokenType.RightBrace => $"语法错误：缺少右大括号 '}}。在 '{actualValue}' 处期望右大括号。",
            LangTokenType.LeftParen => $"语法错误：缺少左括号 '。在 '{actualValue}' 处期望左括号。",
            LangTokenType.LeftBracket => $"语法错误：缺少左方括号 '[。在 '{actualValue}' 处期望左方括号。",
            LangTokenType.LeftBrace => $"语法错误：缺少左大括号 '{{。在 '{actualValue}' 处期望左大括号。",
            LangTokenType.Comma => $"语法错误：缺少逗号 ','。在 '{actualValue}' 处期望逗号。",
            LangTokenType.Arrow => $"语法错误：缺少箭头 '->。在 '{actualValue}' 处期望箭头。",
            LangTokenType.Colon => $"语法错误：缺少冒号 ':'。在 '{actualValue}' 处期望冒号。",
            LangTokenType.Assignment => $"语法错误：缺少赋值符号 '<-。在 '{actualValue}' 处期望赋值符号。",
            LangTokenType.Identifier => $"语法错误：缺少标识符。在 '{actualValue}' 处期望标识符。",
            LangTokenType.String => $"语法错误：缺少字符串字面量。在 '{actualValue}' 处期望字符串。",
            LangTokenType.Number => $"语法错误：缺少数字字面量。在 '{actualValue}' 处期望数字。",
            LangTokenType.If => $"语法错误：缺少 'if' 关键字。在 '{actualValue}' 处期望 'if'。",
            LangTokenType.Else => $"语法错误：缺少 'else' 关键字。在 '{actualValue}' 处期望 'else'。",
            LangTokenType.While => $"语法错误：缺少 'while' 关键字。在 '{actualValue}' 处期望 'while'。",
            LangTokenType.For => $"语法错误：缺少 'for' 关键字。在 '{actualValue}' 处期望 'for'。",
            LangTokenType.Func => $"语法错误：缺少 'func' 关键字。在 '{actualValue}' 处期望 'func'。",
            LangTokenType.Class => $"语法错误：缺少 'class' 关键字。在 '{actualValue}' 处期望 'class'。",
            LangTokenType.Import => $"语法错误：缺少 'import' 关键字。在 '{actualValue}' 处期望 'import'。",
            LangTokenType.Return => $"语法错误：缺少 'return' 关键字。在 '{actualValue}' 处期望 'return'。",
            _ => $"语法错误：期望 {expectedType}，但得到了 {actualType} '{actualValue}'。",
        };
    }

    /// <summary>
    /// 获取建议消息
    /// </summary>
    public static string GetSuggestion(LangTokenType expectedType)
    {
        return expectedType switch
        {
            LangTokenType.RightParen => "建议：检查是否缺少右括号或括号不匹配。",
            LangTokenType.RightBracket => "建议：检查是否缺少右方括号或方括号不匹配。",
            LangTokenType.RightBrace => "建议：检查是否缺少右大括号或大括号不匹配。",
            LangTokenType.LeftParen => "建议：检查是否缺少左括号或括号不匹配。",
            LangTokenType.LeftBracket => "建议：检查是否缺少左方括号或方括号不匹配。",
            LangTokenType.LeftBrace => "建议：检查是否缺少左大括号或大括号不匹配。",
            LangTokenType.Comma => "建议：检查是否缺少逗号分隔符。",
            LangTokenType.Arrow => "建议：检查 lambda 表达式是否缺少箭头符号。",
            LangTokenType.Colon => "建议：检查字典定义或类型注解是否缺少冒号。",
            LangTokenType.Assignment => "建议：检查变量赋值是否使用了正确的赋值符号 '<-。",
            LangTokenType.Identifier => "建议：检查是否需要添加标识符名称。",
            LangTokenType.String => "建议：检查字符串是否正确闭合。",
            LangTokenType.Number => "建议：检查是否需要添加数字值。",
            _ => "建议：检查语法结构是否正确。",
        };
    }
}
