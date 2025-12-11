using Old8Lang.AST.Statement;using Old8Lang.LangParser;

namespace Old8Lang.Tests;

public class InterpreterTests
{
    [Fact]
    public void LangInterpreter_Build_ValidCode_ReturnsBlockStatement()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "a <- 10";
        
        // Act
        var result = interpreter.Build(code);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<BlockStatement>(result);
    }
    
    [Fact]
    public void LangInterpreter_Build_EmptyCode_ReturnsBlockStatement()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = string.Empty;
        
        // Act
        var result = interpreter.Build(code);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<BlockStatement>(result);
        Assert.Equal(0, result.Count);
    }
    
    [Fact]
    public void LangInterpreter_Tokenize_ValidCode_ReturnsTokens()
    {
        // Arrange
        var code = "a <- 10";
        
        // Act
        var result = LangInterpreter.Tokenize(code);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
    
    [Fact]
    public void LangInterpreter_Tokenize_EmptyCode_ReturnsEmptyList()
    {
        // Arrange
        var code = string.Empty;
        
        // Act
        var result = LangInterpreter.Tokenize(code);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    [Fact]
    public void LangInterpreter_GetSourceContext_ValidPosition_ReturnsContext()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "line1\nline2\nline3\nline4\nline5";
        var position = new SourcePosition(3, 0);
        interpreter.Build(code);
        
        // Act
        var result = interpreter.GetSourceContext(position);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("line2", result);
        Assert.Contains("line3", result);
        Assert.Contains("line4", result);
    }
    
    [Fact]
    public void LangInterpreter_GetSourceContext_OutOfRangePosition_ReturnsEmptyArray()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "line1\nline2\nline3";
        var position = new SourcePosition(10, 0);
        interpreter.Build(code);
        
        // Act
        var result = interpreter.GetSourceContext(position);
        
        // Assert
        Assert.NotNull(result);
        // 当位置超出范围时，GetSourceContext返回空数组是符合预期的
        Assert.Empty(result);
    }
    
    [Fact]
    public void LangInterpreter_GetSourceContext_NoSourceCode_ReturnsEmptyArray()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var position = new SourcePosition(1, 0);
        
        // Act
        var result = interpreter.GetSourceContext(position);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    [Fact]
    public void LangInterpreter_UseClass_DefaultIsConsoleUse()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        
        // Act
        var result = interpreter.UseClass;
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<ConsoleUse>(result);
    }
    
    [Fact]
    public void LangInterpreter_IsCompileOptimization_DefaultIsFalse()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        
        // Act
        var result = interpreter.IsCompileOptimization;
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void LangInterpreter_IsCompileOptimization_CanBeSet()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        
        // Act
        interpreter.IsCompileOptimization = true;
        
        // Assert
        Assert.True(interpreter.IsCompileOptimization);
    }
}