using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;

namespace Old8Lang.Tests.Unit.Compiler;

[Collection("Sequential")]
public class LocalManagerTests
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
    
    [Fact]
    public void LocalManager_LocalVar_Management()
    {
        // Arrange
        var localManager = new LocalManager();
        var dynamicMethod = new DynamicMethod("TestMethod", typeof(void), null);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var localBuilder = ilGenerator.DeclareLocal(typeof(int));
        
        // Act
        localManager.AddLocalVar("testVar", localBuilder);
        
        // Assert
        Assert.True(localManager.IsHasVar("testVar"));
        Assert.Equal(1, localManager.GetCount());
        Assert.Equal(localBuilder, localManager.GetLocalVar("testVar"));
        
        // Act - Remove local var
        localManager.RemoveLocalVar("testVar");
        
        // Assert
        Assert.False(localManager.IsHasVar("testVar"));
        Assert.Equal(0, localManager.GetCount());
        Assert.Null(localManager.GetLocalVar("testVar"));
    }
    
    [Fact]
    public void LocalManager_Restore_FromClonedInstance()
    {
        // Arrange
        var localManager = new LocalManager { FilePath = "original.old8" };
        var dynamicMethod = new DynamicMethod("TestMethod", typeof(void), null);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var localBuilder = ilGenerator.DeclareLocal(typeof(int));
        localManager.AddLocalVar("testVar", localBuilder);
        
        // Act
        var cloned = localManager.Clone();
        cloned.FilePath = "cloned.old8";
        var newLocalBuilder = ilGenerator.DeclareLocal(typeof(string));
        cloned.AddLocalVar("newVar", newLocalBuilder);
        
        localManager.Restore(cloned);
        
        // Assert
        Assert.Equal("cloned.old8", localManager.FilePath);
        Assert.True(localManager.IsHasVar("testVar"));
        Assert.True(localManager.IsHasVar("newVar"));
        Assert.Equal(2, localManager.GetCount());
    }
    
    [Fact]
    public void LocalManager_ValidateType_CompatibleTypes()
    {
        // Arrange
        var localManager = new LocalManager();
        var position = new SourcePosition(1, 1);
        
        // Act & Assert
        Assert.True(localManager.ValidateType(typeof(object), typeof(string), position));
        Assert.True(localManager.ValidateType(typeof(int), typeof(int), position));
    }
    
    [Fact]
    public void LocalManager_FuncParameters_Management()
    {
        // Arrange
        var localManager = new LocalManager();
        var parameters = new List<LangId> { new("param1"), new("param2") };
        
        // Act
        localManager.FuncParameters["testFunc"] = parameters;
        
        // Assert
        Assert.True(localManager.FuncParameters.ContainsKey("testFunc"));
        Assert.Equal(2, localManager.FuncParameters["testFunc"].Count);
        Assert.Equal("param1", localManager.FuncParameters["testFunc"][0].IdName);
        Assert.Equal("param2", localManager.FuncParameters["testFunc"][1].IdName);
    }
    
    [Fact]
    public void LocalManager_ClassVar_Management()
    {
        // Arrange
        var localManager = new LocalManager();
        
        // Act
        localManager.ClassVar["TestClass"] = typeof(int);
        
        // Assert
        Assert.True(localManager.ClassVar.ContainsKey("TestClass"));
        Assert.Equal(typeof(int), localManager.ClassVar["TestClass"]);
    }
    
    [Fact]
    public void LocalManager_FieldVar_Management()
    {
        // Arrange
        var localManager = new LocalManager();
        var fieldInfo = typeof(string).GetField("Empty")!;
        
        // Act
        localManager.FieldVar["testField"] = fieldInfo;
        
        // Assert
        Assert.True(localManager.FieldVar.ContainsKey("testField"));
        Assert.Equal(fieldInfo, localManager.FieldVar["testField"]);
    }
    
    [Fact]
    public void LocalManager_DelegateVar_Management()
    {
        // Arrange
        var localManager = new LocalManager();
        var methodInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!;
        
        // Act
        localManager.DelegateVar["testDelegate"] = methodInfo;
        
        // Assert
        Assert.True(localManager.DelegateVar.ContainsKey("testDelegate"));
        Assert.Equal(methodInfo, localManager.DelegateVar["testDelegate"]);
    }
    
    [Fact]
    public void LocalManager_LocalVarTypes_Management()
    {
        // Arrange
        var localManager = new LocalManager();
        
        // Act
        localManager.LocalVarTypes["testVar"] = typeof(int);
        
        // Assert
        Assert.True(localManager.LocalVarTypes.ContainsKey("testVar"));
        Assert.Equal(typeof(int), localManager.LocalVarTypes["testVar"]);
    }
    
    [Fact]
    public void LocalManager_BreakAndContinueLabels()
    {
        // Arrange
        var localManager = new LocalManager();
        var dynamicMethod = new DynamicMethod("TestMethod", typeof(void), null);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var breakLabel = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();
        
        // Act
        localManager.BreakLabel = breakLabel;
        localManager.ContinueLabel = continueLabel;
        
        // Assert
        Assert.Equal(breakLabel, localManager.BreakLabel);
        Assert.Equal(continueLabel, localManager.ContinueLabel);
    }
}