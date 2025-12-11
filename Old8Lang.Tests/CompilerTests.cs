using Old8Lang.AST.Statement;using Old8Lang.Compiler;using Old8Lang.LangParser;using Xunit;

namespace Old8Lang.Tests;

public class CompilerTests
{
    [Fact]
    public void LocalManager_New_CreatesNewInstance()
    {
        // Arrange
        var localManager = new LocalManager { FilePath = "test.old8" };
        
        // Act
        var result = localManager.New();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotSame(localManager, result);
        Assert.Equal("test.old8", result.FilePath);
    }
    
    [Fact]
    public void LocalManager_Clone_CreatesDeepCopy()
    {
        // Arrange
        var localManager = new LocalManager { FilePath = "test.old8" };
        
        // Act
        var result = localManager.Clone();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotSame(localManager, result);
        Assert.Equal(localManager.FilePath, result.FilePath);
        Assert.Equal(localManager.InClassEnv, result.InClassEnv);
        Assert.Equal(localManager.BreakLabel, result.BreakLabel);
        Assert.Equal(localManager.ContinueLabel, result.ContinueLabel);
    }
    
    [Fact]
    public void LocalManager_IsHasVar_ReturnsFalseForNonExistentVar()
    {
        // Arrange
        var localManager = new LocalManager();
        
        // Act
        var result = localManager.IsHasVar("nonExistentVar");
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void LocalManager_GetCount_ReturnsZeroForEmptyManager()
    {
        // Arrange
        var localManager = new LocalManager();
        
        // Act
        var result = localManager.GetCount();
        
        // Assert
        Assert.Equal(0, result);
    }
    
    [Fact]
    public void LocalManager_FilePath_CanBeSet()
    {
        // Arrange
        var localManager = new LocalManager();
        
        // Act
        localManager.FilePath = "new_path.old8";
        
        // Assert
        Assert.Equal("new_path.old8", localManager.FilePath);
    }
    
    [Fact]
    public void LocalManager_IsInFinallyBlock_DefaultIsFalse()
    {
        // Arrange
        var localManager = new LocalManager();
        
        // Act
        var result = localManager.IsInFinallyBlock;
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void LocalManager_IsInFinallyBlock_CanBeSet()
    {
        // Arrange
        var localManager = new LocalManager();
        
        // Act
        localManager.IsInFinallyBlock = true;
        
        // Assert
        Assert.True(localManager.IsInFinallyBlock);
    }
    
    // 模拟IMiniInterpreter实现
    private class MockInterpreter : IMiniInterpreter
    {
        public BlockStatement Build(string code)
        {
            return new BlockStatement([]);
        }
        
        public AbsUseClass UseClass { get; set; } = new ConsoleUse();
        
        public bool IsCompileOptimization { get; set; } = false;
    }
}