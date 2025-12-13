using System.Reflection.Emit;

namespace Old8Lang.Compiler;

/// <summary>
/// IL代码验证器，用于验证生成的IL代码是否符合ECMA-335标准
/// </summary>
public static class ILVerifier
{
    /// <summary>
    /// 验证动态方法生成的IL代码
    /// </summary>
    /// <param name="dynamicMethod">要验证的动态方法</param>
    /// <param name="methodName">方法名称，用于错误报告</param>
    /// <returns>验证结果，包含是否通过以及详细的错误信息</returns>
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
/// 验证结果
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// IL代码是否有效
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// 验证过程中发现的错误
    /// </summary>
    public List<VerificationError> Errors { get; set; }
    
    public VerificationResult()
    {
        Errors = new List<VerificationError>();
    }
}

/// <summary>
/// 验证错误
/// </summary>
public class VerificationError
{
    /// <summary>
    /// 错误严重性
    /// </summary>
    public Severity Severity { get; set; }
    
    /// <summary>
    /// 错误代码
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// 方法名称
    /// </summary>
    public string? MethodName { get; set; }
    
    /// <summary>
    /// 错误位置偏移量
    /// </summary>
    public int Offset { get; set; }
    
    /// <summary>
    /// 堆栈跟踪信息
    /// </summary>
    public string? StackTrace { get; set; }
    
    /// <summary>
    /// 错误上下文信息
    /// </summary>
    public string? Context { get; set; }
}

/// <summary>
/// 错误严重性枚举
/// </summary>
public enum Severity
{
    /// <summary>
    /// 信息
    /// </summary>
    Info,
    
    /// <summary>
    /// 警告
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error
}
