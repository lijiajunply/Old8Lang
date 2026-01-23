using System.Reflection.Emit;

namespace Old8Lang.Compiler.Verification;

/// <summary>
/// IL代码验证器，用于验证生成的IL代码是否符合ECMA-335标准
/// </summary>
/// <remarks>
/// 该类采用CLR内置的IL验证机制，通过尝试创建委托来验证动态方法的IL代码。
/// 这是一种可靠的验证方式，因为CLR在创建委托时会严格检查IL代码的正确性。
/// 验证结果包含是否通过以及详细的错误信息，便于调试和修复IL生成问题。
/// </remarks>
public static class IlVerifier
{
    /// <summary>
    /// 验证动态方法生成的IL代码
    /// </summary>
    /// <param name="dynamicMethod">要验证的动态方法</param>
    /// <param name="methodName">方法名称，用于错误报告</param>
    /// <returns>验证结果，包含是否通过以及详细的错误信息</returns>
    /// <remarks>
    /// 验证过程：
    /// 1. 创建一个VerificationResult实例用于存储验证结果
    /// 2. 尝试创建委托，这会触发CLR的IL验证
    /// 3. 如果创建委托成功，说明IL代码基本有效
    /// 4. 如果创建委托失败，捕获异常并记录详细的错误信息
    /// 5. 返回验证结果，包含是否通过以及所有错误信息
    /// </remarks>
    public static VerificationResult Verify(DynamicMethod dynamicMethod, string methodName = "DynamicMethod")
    {
        var result = new VerificationResult();

        try
        {
            // 对于DynamicMethod，我们通过尝试创建委托来验证IL
            // 这是最直接的方式，因为CLR会在创建委托时验证IL
            try
            {
                // 尝试创建委托，这会触发CLR的IL验证
                dynamicMethod.CreateDelegate(typeof(Action));

                // 如果没有抛出异常，则IL代码基本有效
                result.IsValid = true;
            }
            catch (InvalidProgramException ex)
            {
                // IL代码无效，记录详细错误
                result.Errors.Add(new VerificationError
                {
                    Severity = Severity.Error,
                    Code = "IL001",
                    Message = $"无效的IL代码: {ex.Message}",
                    MethodName = methodName,
                    Context = "CLR在创建委托时检测到无效IL代码"
                });
            }
            catch (Exception ex)
            {
                // 其他类型的异常
                result.Errors.Add(new VerificationError
                {
                    Severity = Severity.Error,
                    Code = "IL002",
                    Message = $"验证过程中发生异常: {ex.Message}",
                    MethodName = methodName,
                    StackTrace = ex.StackTrace
                });
            }
        }
        catch (Exception ex)
        {
            // 验证器内部发生异常
            result.Errors.Add(new VerificationError
            {
                Severity = Severity.Error,
                Code = "IL000",
                Message = $"验证器内部错误: {ex.Message}",
                MethodName = methodName,
                StackTrace = ex.StackTrace
            });
        }

        return result;
    }
}

/// <summary>
/// IL验证结果，包含验证是否通过以及详细的错误信息
/// </summary>
/// <remarks>
/// 该类用于存储IL验证的结果，包括：
/// - 验证是否通过（IsValid）
/// - 验证过程中发现的所有错误（Errors）
/// 可以通过检查IsValid属性快速判断IL代码是否有效，
/// 通过Errors属性获取详细的错误信息用于调试。
/// </remarks>
public class VerificationResult
{
    /// <summary>
    /// IL代码是否有效
    /// </summary>
    /// <value>如果IL代码有效则为true，否则为false</value>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证过程中发现的错误列表
    /// </summary>
    /// <value>包含所有验证错误的列表，验证通过时为空列表</value>
    public List<VerificationError> Errors { get; set; }

    /// <summary>
    /// 初始化VerificationResult实例
    /// </summary>
    public VerificationResult()
    {
        Errors = [];
    }
}

/// <summary>
/// IL验证错误，包含详细的错误信息
/// </summary>
/// <remarks>
/// 该类用于存储单个IL验证错误的详细信息，包括：
/// - 错误严重性（Severity）
/// - 错误代码（Code）
/// - 错误消息（Message）
/// - 相关方法名称（MethodName）
/// - 错误位置偏移量（Offset）
/// - 堆栈跟踪信息（StackTrace）
/// - 错误上下文信息（Context）
/// 这些信息对于调试和修复IL生成问题非常有用。
/// </remarks>
public class VerificationError
{
    /// <summary>
    /// 错误严重性
    /// </summary>
    public Severity Severity { get; set; }

    /// <summary>
    /// 错误代码，用于标识不同类型的验证错误
    /// </summary>
    /// <value>错误代码，如"IL001"表示无效的IL代码</value>
    public string? Code { get; set; }

    /// <summary>
    /// 错误消息，包含详细的错误描述
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 方法名称，用于标识出错的方法
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// 错误位置偏移量，指示错误在IL代码中的位置
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// 堆栈跟踪信息，用于调试复杂的验证错误
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// 错误上下文信息，提供错误发生时的相关上下文
    /// </summary>
    public string? Context { get; set; }
}

/// <summary>
/// 错误严重性枚举，用于表示IL验证错误的严重程度
/// </summary>
public enum Severity
{
    /// <summary>
    /// 信息，仅用于提供验证过程中的信息
    /// </summary>
    Info,

    /// <summary>
    /// 警告，IL代码可能存在问题，但不会导致程序崩溃
    /// </summary>
    Warning,

    /// <summary>
    /// 错误，IL代码无效，会导致程序崩溃
    /// </summary>
    Error
}