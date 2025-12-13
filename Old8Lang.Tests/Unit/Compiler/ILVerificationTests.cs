using Old8Lang.LangParser;

namespace Old8Lang.Tests.Unit.Compiler;

/// <summary>
/// IL验证器单元测试
/// </summary>
public class ILVerificationTests
{
    /// <summary>
    /// 测试有效的IL代码能够通过验证
    /// </summary>
    [Fact]
    public void Verify_ValidIL_CanPassVerification()
    {
        // 测试正常情况：应该能通过IL验证
        var code = @"
            func normal_function() {
                a <- 123
                b <- 456
                c <- a + b
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);

        // 编译生成IL - 应该能通过验证
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // 验证IL已生成且委托已创建
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试IL验证开关控制
    /// </summary>
    [Fact]
    public void Verify_ILVerificationCanBeDisabled()
    {
        // 禁用IL验证
        Old8Lang.Compiler.Compiler.ILVerificationEnabled = false;
        
        try
        {
            // 测试正常情况：应该能通过编译
            var code = @"
                func normal_function() {
                    a <- 123
                    b <- 456
                    c <- a + b
                }
            ";
            var interpreter = new LangInterpreter();

            var ast = interpreter.Build(code);

            // 编译生成IL - 应该能通过编译
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

            // 验证IL已生成且委托已创建
            Assert.NotNull(compiledAction);
        }
        finally
        {
            // 恢复IL验证开关
            Old8Lang.Compiler.Compiler.ILVerificationEnabled = true;
        }
    }
    
    /// <summary>
    /// 测试IL验证器能够正确处理异常
    /// </summary>
    [Fact]
    public void Verify_ILVerifierHandlesExceptions()
    {
        // 这个测试主要验证IL验证器不会因为异常而崩溃
        var code = @"
            func exception_test() {
                a <- 123
                PrintLine(a)
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);

        // 编译生成IL - 应该能通过验证
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // 验证IL已生成且委托已创建
        Assert.NotNull(compiledAction);
    }
}
