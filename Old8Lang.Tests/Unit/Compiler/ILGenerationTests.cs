using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Tests.Unit.Compiler;

/// <summary>
/// IL生成模块单元测试
/// </summary>
[Collection("Sequential")]
public class ILGenerationTests
{
    /// <summary>
    /// 测试BlockStatement的IL生成
    /// </summary>
    [Fact]
    public void BlockStatement_GenerateIl_ShouldGenerateCorrectIL()
    {
        // Arrange
        var code = @"
            a <- 123
            b <- 456
            c <- a + b
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        
        // Act
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test.old8", interpreter);
        
        // Assert
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试IfStatement的IL生成
    /// </summary>
    [Fact]
    public void IfStatement_GenerateIl_ShouldGenerateCorrectIL()
    {
        // Arrange
        var code = @"
            a <- 10
            if a > 5 {
                result <- 1
            } else {
                result <- 0
            }
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        
        // Act
        var compiledAction = Old8Lang.Compiler.Compiler.Compile