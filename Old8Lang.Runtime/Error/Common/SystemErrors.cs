using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 配置错误
/// </summary>
public class ConfigurationError : RuntimeError
{
    /// <summary>
    /// 配置错误代码
    /// </summary>
    public new const string ErrorCode = "CONFIGURATION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="configName">配置名称</param>
    /// <param name="message">错误信息</param>
    public ConfigurationError(IOldLangTree node, string configName, string message)
        : base(
            node,
            ErrorCode,
            $"配置 '{configName}' 错误: {message}",
            "请检查配置是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ConfigurationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查配置是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="configName">配置名称</param>
    /// <param name="message">错误信息</param>
    public ConfigurationError(SourcePosition position, string configName, string message)
        : base(
            position,
            ErrorCode,
            $"配置 '{configName}' 错误: {message}",
            "请检查配置是否正确")
    {
    }
}

/// <summary>
/// 环境变量错误
/// </summary>
public class EnvironmentError : RuntimeError
{
    /// <summary>
    /// 环境变量错误代码
    /// </summary>
    public new const string ErrorCode = "ENVIRONMENT_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="variableName">环境变量名称</param>
    public EnvironmentError(IOldLangTree node, string variableName)
        : base(
            node,
            ErrorCode,
            $"环境变量 '{variableName}' 未设置",
            "请设置相应的环境变量")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    public EnvironmentError(IOldLangTree node, string message, string suggestion)
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
    /// <param name="variableName">环境变量名称</param>
    public EnvironmentError(SourcePosition position, string variableName)
        : base(
            position,
            ErrorCode,
            $"环境变量 '{variableName}' 未设置",
            "请设置相应的环境变量")
    {
    }
}

/// <summary>
/// 操作取消错误
/// </summary>
public class OperationCancelledError : RuntimeError
{
    /// <summary>
    /// 操作取消错误代码
    /// </summary>
    public new const string ErrorCode = "OPERATION_CANCELLED_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    public OperationCancelledError(IOldLangTree node)
        : base(
            node,
            ErrorCode,
            "操作已被取消",
            "操作被用户或系统取消")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="operationName">操作名称</param>
    public OperationCancelledError(IOldLangTree node, string operationName)
        : base(
            node,
            ErrorCode,
            $"操作 '{operationName}' 已被取消",
            "操作被用户或系统取消")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="operationName">操作名称</param>
    public OperationCancelledError(SourcePosition position, string operationName)
        : base(
            position,
            ErrorCode,
            $"操作 '{operationName}' 已被取消",
            "操作被用户或系统取消")
    {
    }
}

/// <summary>
/// 数据验证错误
/// </summary>
public class ValidationError : RuntimeError
{
    /// <summary>
    /// 数据验证错误代码
    /// </summary>
    public new const string ErrorCode = "VALIDATION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="fieldName">字段名称</param>
    /// <param name="message">错误信息</param>
    public ValidationError(IOldLangTree node, string fieldName, string message)
        : base(
            node,
            ErrorCode,
            $"字段 '{fieldName}' 验证失败: {message}",
            "请检查输入数据是否符合要求")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ValidationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查输入数据是否符合要求")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="fieldName">字段名称</param>
    /// <param name="message">错误信息</param>
    public ValidationError(SourcePosition position, string fieldName, string message)
        : base(
            position,
            ErrorCode,
            $"字段 '{fieldName}' 验证失败: {message}",
            "请检查输入数据是否符合要求")
    {
    }
}

/// <summary>
/// 状态错误（对象处于无效状态）
/// </summary>
public class StateError : RuntimeError
{
    /// <summary>
    /// 状态错误代码
    /// </summary>
    public new const string ErrorCode = "STATE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public StateError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查对象状态是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="objectName">对象名称</param>
    /// <param name="currentState">当前状态</param>
    /// <param name="expectedState">期望状态</param>
    public StateError(IOldLangTree node, string objectName, string currentState, string expectedState)
        : base(
            node,
            ErrorCode,
            $"对象 '{objectName}' 状态错误: 当前状态为 '{currentState}'，期望状态为 '{expectedState}'",
            "请确保对象处于正确的状态后再执行操作")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public StateError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查对象状态是否正确")
    {
    }
}

/// <summary>
/// 范围错误（值超出有效范围）
/// </summary>
public class RangeError : RuntimeError
{
    /// <summary>
    /// 范围错误代码
    /// </summary>
    public new const string ErrorCode = "RANGE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="value">值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    public RangeError(IOldLangTree node, object value, object min, object max)
        : base(
            node,
            ErrorCode,
            $"值 {value} 超出有效范围 [{min}, {max}]",
            "请确保值在有效范围内")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public RangeError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请确保值在有效范围内")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="value">值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    public RangeError(SourcePosition position, object value, object min, object max)
        : base(
            position,
            ErrorCode,
            $"值 {value} 超出有效范围 [{min}, {max}]",
            "请确保值在有效范围内")
    {
    }
}
