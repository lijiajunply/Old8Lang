using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.IO;

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
            // 获取动态方法的IL字节
            var ilBytes = dynamicMethod.GetMethodImplementationFlags() != MethodImplAttributes.IL
                ? Array.Empty<byte>()
                : GetILBytes(dynamicMethod);
            
            if (ilBytes.Length == 0)
            {
                result.Errors.Add(new VerificationError
                {
                    Severity = Severity.Error,
                    Code = "IL001",
                    Message = "方法没有IL实现",
                    MethodName = methodName
                });
                return result;
            }
            
            // 解析IL指令
            var instructions = ParseILInstructions(ilBytes);
            
            // 执行基本验证
            ValidateILInstructions(instructions, result, methodName);
            
            // 执行堆栈验证
            ValidateStackOperations(instructions, result, methodName);
            
            // 执行类型验证
            ValidateTypeSafety(instructions, result, methodName);
            
            // 如果没有错误，则验证通过
            if (result.Errors.Count == 0)
            {
                result.IsValid = true;
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new VerificationError
            {
                Severity = Severity.Error,
                Code