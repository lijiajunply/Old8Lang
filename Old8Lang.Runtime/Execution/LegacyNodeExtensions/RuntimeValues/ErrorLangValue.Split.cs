using System.Globalization;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// ErrorLangValue 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class ErrorLangValue
{
    /// <summary>
    /// 支持属性访问，带VariManager参数
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
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
                    return Suggestion is not null ? new StringLangValue(Suggestion) : new NullLangValue();
                case "Timestamp":
                    return new StringLangValue(Timestamp.ToString(CultureInfo.InvariantCulture));
                case "RequestId":
                    return new StringLangValue(RequestId);
                case "ToStr":
                    // 处理 ToStr() 方法调用，返回字符串表示
                    return new StringLangValue(ToDisplayString());
                case "GetType":
                    // 处理 GetType() 方法调用，返回类型名称
                    return new StringLangValue(nameof(ErrorLangValue));
                default:
                    throw new Old8Exception(
                        "ATTRIBUTE_ERROR",
                        $"类型 'ErrorLangValue' 没有属性 '{propertyName}'",
                        langId.Position,
                        langId,
                        "请检查属性名称是否正确");
            }
        }

        if (dotExpression is Instance instance)
        {
            // 处理方法调用
            switch (instance.Id.IdName)
            {
                case "ToStr":
                    return new StringLangValue(ToDisplayString());
                case "GetType":
                    return new StringLangValue(nameof(ErrorLangValue));
                default:
                    throw new Old8Exception(
                        "ATTRIBUTE_ERROR",
                        $"类型 'ErrorLangValue' 没有方法 '{instance.Id.IdName}()'",
                        instance.Id.Position,
                        instance.Id,
                        "请检查方法名称是否正确");
            }
        }

        // 不调用base.Dot，因为我们已经处理了所有支持的属性
        throw new Old8Exception(
            "ATTRIBUTE_ERROR",
            $"类型 'ErrorLangValue' 没有属性 '{(dotExpression is LangId id ? id.IdName : dotExpression.ToString())}'",
            dotExpression.Position,
            dotExpression,
            "请检查属性名称是否正确");
    }
}
