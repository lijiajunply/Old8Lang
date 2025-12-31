using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Error.Compilation;

/// <summary>
/// 编译器测试辅助类，用于处理编译模式下的常见问题
/// </summary>
public static class FixCompilationIssue
{
    /// <summary>
    /// 验证代码是否能在编译模式下正确编译和执行
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyCompilationAndExecution(string name, string code)
    {
        var interpreter = new LangInterpreter();
        
        try
        {
            // 构建 AST
            var ast = interpreter.Build(code);
            
            // 编译代码
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            
            // 执行代码来验证结果
            var exception = Record.Exception(() => compiledAction());
            Assert.Null(exception);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Unexpected error during {name}: {ex.Message}\nCode:\n{code}");
        }
    }
    
    /// <summary>
    /// 测试编译器模式下的类型注解严格性
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyTypeAnnotations(string name, string code)
    {
        try
        {
            // 在编译模式下，类型注解应该是严格的
            VerifyCompilationAndExecution(name, code);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Type annotation verification failed for {name}: {ex.Message}\nCode:\n{code}");
        }
    }
    
    /// <summary>
    /// 验证错误处理代码在编译模式下的行为
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyErrorHandling(string name, string code)
    {
        try
        {
            VerifyCompilationAndExecution(name, code);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Error handling verification failed for {name}: {ex.Message}\nCode:\n{code}");
        }
    }
    
    /// <summary>
    /// 验证函数在编译模式下的行为
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyFunctionCompilation(string name, string code)
    {
        try
        {
            VerifyCompilationAndExecution(name, code);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Function compilation verification failed for {name}: {ex.Message}\nCode:\n{code}");
        }
    }
    
    /// <summary>
    /// 验证集合操作在编译模式下的行为
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyCollectionCompilation(string name, string code)
    {
        try
        {
            VerifyCompilationAndExecution(name, code);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Collection compilation verification failed for {name}: {ex.Message}\nCode:\n{code}");
        }
    }
    
    /// <summary>
    /// 验证异步操作在编译模式下的行为
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyAsyncCompilation(string name, string code)
    {
        try
        {
            VerifyCompilationAndExecution(name, code);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Async compilation verification failed for {name}: {ex.Message}\nCode:\n{code}");
        }
    }
    
    /// <summary>
    /// 验证边界情况在编译模式下的行为
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyEdgeCaseCompilation(string name, string code)
    {
        try
        {
            VerifyCompilationAndExecution(name, code);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Edge case compilation verification failed for {name}: {ex.Message}\nCode:\n{code}");
        }
    }
    
    /// <summary>
    /// 验证泛型操作在编译模式下的行为
    /// </summary>
    /// <param name="name">测试名称</param>
    /// <param name="code">要测试的代码</param>
    public static void VerifyGenericCompilation(string name, string code)
    {
        try
        {
            VerifyCompilationAndExecution(name, code);
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Generic compilation verification failed for {name}: {ex.Message}\nCode:\n{code}");
        }
    }
}