using Old8Lang.Error;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL 模式下的运行期类型错误（与解释器/虚拟机的 TYPE_ERROR 口径一致）。
/// </summary>
/// <remarks>
/// 静态已知的类型不匹配不能再直接生成 `call int fn(int)` 这类无效 IL：
/// 调用点改为发射这里的 helper，运行期抛 <see cref="TypeError"/>。
/// </remarks>
public static class IlTypeErrors
{
    /// <summary>把 CLR 类型映射成语言层类型名（用于错误消息）。</summary>
    public static string TypeName(Type? type) => type switch
    {
        null => "object",
        _ when type == typeof(int) => "int",
        _ when type == typeof(double) => "double",
        _ when type == typeof(string) => "string",
        _ when type == typeof(bool) => "bool",
        _ when type == typeof(char) => "char",
        _ when type == typeof(object) => "object",
        _ => type.Name
    };

    /// <summary>对不支持的类型执行运算（与 VM 的 INVALID_OPERATION_ERROR 一致）。</summary>
    public static object? ThrowInvalidOperation(string message)
        => throw new InvalidOperationError(new SourcePosition(), message);

    /// <summary>函数参数类型不匹配。</summary>
    public static object? ThrowParameterMismatch(string expected, string actual, string parameterName)
        => throw new TypeError(new SourcePosition(), expected, actual,
            $"参数 '{parameterName}' 类型不匹配");

    /// <summary>函数返回值类型不匹配。</summary>
    public static object? ThrowReturnMismatch(string expected, string actual)
        => throw new TypeError(new SourcePosition(), expected, actual,
            "返回值类型不匹配");

    /// <summary>带类型注解的变量赋值类型不匹配。</summary>
    public static object? ThrowAssignmentMismatch(string expected, string actual, string variableName)
        => throw new TypeError(new SourcePosition(), expected, actual,
            $"变量 '{variableName}' 赋值类型不匹配");
}
