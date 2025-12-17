using Old8Lang.AST;using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Unit.Error;

[Collection("Sequential")]
public class ExceptionCreationTests
{
    [Fact]
    public void RuntimeError_WithPosition_CanBeCreated()
    {
        // Arrange
        var position = new SourcePosition(1, 10, "test.old8");
        
        // Act
        var exception = new InvalidOperationError(position, "无效操作错误信息");
        
        // Assert
        Assert.NotNull(exception);
        Assert.Contains("INVALID_OPERATION_ERROR", exception.Message);
        Assert.Equal(position, exception.Position);
        Assert.NotNull(exception.Message);
    }
    
    [Fact]
    public void RuntimeError_Message_ContainsErrorCodeAndPosition()
    {
        // Arrange
        var position = new SourcePosition(1, 10, "test.old8");
        
        // Act
        var exception = new InvalidOperationError(position, "无效操作错误信息");
        
        // Assert
        Assert.Contains("INVALID_OPERATION_ERROR", exception.Message);
        Assert.Contains("1", exception.Message);
        Assert.Contains("10", exception.Message);
        Assert.Contains("test.old8", exception.Message);
    }
    
    [Fact]
    public void InvalidOperationError_CanBeCreatedWithAllParameters()
    {
        // Arrange
        var position = new SourcePosition(5, 20);
        var suggestion = "请检查操作是否合法";
        
        // Act
        var exception = new InvalidOperationError(position, "无效操作错误信息", suggestion);
        
        // Assert
        Assert.Contains("INVALID_OPERATION_ERROR", exception.Message);
        Assert.Equal(position, exception.Position);
        Assert.Equal(suggestion, exception.Suggestion);
        Assert.NotNull(exception.Message);
    }
    
    [Fact]
    public void AttributeError_CanBeCreatedWithNode()
    {
        // Arrange
        var mockNode = new MockLangTree();
        
        // Act
        var exception = new AttributeError(mockNode, "nonExistentAttr", "TestType");
        
        // Assert
        Assert.NotNull(exception);
        Assert.Contains("ATTRIBUTE_ERROR", exception.Message);
        Assert.Equal(mockNode, exception.Node);
        Assert.Contains("nonExistentAttr", exception.Message);
        Assert.Contains("TestType", exception.Message);
        Assert.NotNull(exception.Suggestion);
    }
    
    [Fact]
    public void KeyError_CanBeCreatedWithPosition()
    {
        // Arrange
        var position = new SourcePosition(10, 5);
        
        // Act
        var exception = new KeyError(position, "nonExistentKey");
        
        // Assert
        Assert.NotNull(exception);
        Assert.Contains("KEY_ERROR", exception.Message);
        Assert.Equal(position, exception.Position);
        Assert.Contains("nonExistentKey", exception.Message);
        Assert.NotNull(exception.Suggestion);
    }
    
    [Fact]
    public void ZeroDivisionError_CanBeCreatedWithNode()
    {
        // Arrange
        var mockNode = new MockLangTree();
        
        // Act
        var exception = new ZeroDivisionError(mockNode);
        
        // Assert
        Assert.NotNull(exception);
        Assert.Contains("ZERO_DIVISION_ERROR", exception.Message);
        Assert.Equal(mockNode, exception.Node);
        Assert.Contains("除零错误", exception.Message);
        Assert.NotNull(exception.Suggestion);
    }
    
    [Fact]
    public void OutOfMemoryError_CanBeCreatedWithMessage()
    {
        // Arrange
        var mockNode = new MockLangTree();
        
        // Act
        var exception = new OutOfMemoryError(mockNode, "内存不足错误信息");
        
        // Assert
        Assert.NotNull(exception);
        Assert.Contains("OUT_OF_MEMORY_ERROR", exception.Message);
        Assert.Contains("内存溢出", exception.Message);
        Assert.Contains("内存不足错误信息", exception.Message);
        Assert.NotNull(exception.Suggestion);
    }
    
    [Fact]
    public void OverflowError_CanBeCreatedWithOperation()
    {
        // Arrange
        var mockNode = new MockLangTree();
        
        // Act
        var exception = new OverflowError(mockNode, "addition");
        
        // Assert
        Assert.NotNull(exception);
        Assert.Contains("OVERFLOW_ERROR", exception.Message);
        Assert.Contains("数值溢出", exception.Message);
        Assert.Contains("addition", exception.Message);
        Assert.NotNull(exception.Suggestion);
    }
    
    [Fact]
    public void Old8Exception_CurrentInterpreter_CanBeSetAndGet()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        
        // Act
        Old8Exception.CurrentInterpreter = interpreter;
        var result = Old8Exception.CurrentInterpreter;
        
        // Assert
        Assert.Equal(interpreter, result);
    }
    
    [Fact]
    public void Old8Exception_CurrentInterpreter_CanBeResetToNull()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        Old8Exception.CurrentInterpreter = interpreter;
        
        // Act
        Old8Exception.CurrentInterpreter = null;
        var result = Old8Exception.CurrentInterpreter;
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void SourcePosition_ToString_ReturnsFormattedString()
    {
        // Arrange
        var position = new SourcePosition(10, 25, "test_file.old8");
        
        // Act
        var result = position.ToString();
        
        // Assert
        Assert.Contains("test_file.old8", result);
        Assert.Contains("10", result);
        Assert.Contains("25", result);
    }
    
    [Fact]
    public void SourcePosition_DefaultValues_AreSetCorrectly()
    {
        // Arrange
        var position = new SourcePosition();
        
        // Act & Assert
        Assert.Equal(0, position.Line);
        Assert.Equal(0, position.Column);
        Assert.Null(position.FileName);
        Assert.Null(position.TokenValue);
    }
    
    [Fact]
    public void SourcePosition_WithLineAndColumn_ReturnsCorrectValues()
    {
        // Arrange
        var position = new SourcePosition(5, 15);
        
        // Act & Assert
        Assert.Equal(5, position.Line);
        Assert.Equal(15, position.Column);
    }
    
    [Fact]
    public void RuntimeError_Message_IncludesSuggestionWhenProvided()
    {
        // Arrange
        var position = new SourcePosition(1, 1);
        var suggestion = "这是一个自定义建议";
        
        // Act
        var exception = new InvalidOperationError(position, "无效操作错误信息", suggestion);
        
        // Assert
        Assert.Contains(suggestion, exception.Message);
    }
}