using Old8Lang.AST.Expression;using Old8Lang.AST.Expression.Value;using Xunit;

namespace Old8Lang.Tests;

public class TypeSystemTests
{
    [Fact]
    public void LangValueType_ObjToValue_Null_ReturnsNullLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue(null);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<NullLangValue>(result);
    }
    
    [Fact]
    public void LangValueType_ObjToValue_Int_ReturnsIntLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue(42);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }
    
    [Fact]
    public void LangValueType_ObjToValue_String_ReturnsStringLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue("test");
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("test", ((StringLangValue)result).Value);
    }
    
    [Fact]
    public void LangValueType_ObjToValue_Double_ReturnsDoubleLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue(3.14);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14, ((DoubleLangValue)result).Value);
    }
    
    [Fact]
    public void LangValueType_ObjToValue_Bool_ReturnsBoolLangValue()
    {
        // Act
        var result1 = LangValueType.ObjToValue(true);
        var result2 = LangValueType.ObjToValue(false);
        
        // Assert
        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);
        
        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.False(((BoolLangValue)result2).Value);
    }
    
    [Fact]
    public void IntLangValue_Plus_Int_ReturnsIntLangValue()
    {
        // Arrange
        var a = new IntLangValue(10);
        var b = new IntLangValue(20);
        
        // Act
        var result = a.Plus(b);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }
    
    [Fact]
    public void IntLangValue_Minus_Int_ReturnsIntLangValue()
    {
        // Arrange
        var a = new IntLangValue(30);
        var b = new IntLangValue(10);
        
        // Act
        var result = a.Minus(b);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value);
    }
    
    [Fact]
    public void IntLangValue_Times_Int_ReturnsIntLangValue()
    {
        // Arrange
        var a = new IntLangValue(5);
        var b = new IntLangValue(6);
        
        // Act
        var result = a.Times(b);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }
    
    [Fact]
    public void StringLangValue_Plus_String_ReturnsStringLangValue()
    {
        // Arrange
        var a = new StringLangValue("hello");
        var b = new StringLangValue(" world");
        
        // Act
        var result = a.Plus(b);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("hello world", ((StringLangValue)result).Value);
    }
    
    [Fact]
    public void BoolLangValue_Equal_Bool_ReturnsTrueForSameValues()
    {
        // Arrange
        var a = new BoolLangValue(true);
        var b = new BoolLangValue(true);
        
        // Act
        var result = a.Equal(b);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void BoolLangValue_Equal_Bool_ReturnsFalseForDifferentValues()
    {
        // Arrange
        var a = new BoolLangValue(true);
        var b = new BoolLangValue(false);
        
        // Act
        var result = a.Equal(b);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void IntLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new IntLangValue(42);
        
        // Act
        var result = value.TypeToString();
        
        // Assert
        Assert.Equal("Int", result);
    }
    
    [Fact]
    public void StringLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new StringLangValue("test");
        
        // Act
        var result = value.TypeToString();
        
        // Assert
        Assert.Equal("String", result);
    }
    
    [Fact]
    public void DoubleLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new DoubleLangValue(3.14);
        
        // Act
        var result = value.TypeToString();
        
        // Assert
        Assert.Equal("Double", result);
    }
    
    [Fact]
    public void BoolLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new BoolLangValue(true);
        
        // Act
        var result = value.TypeToString();
        
        // Assert
        Assert.Equal("Bool", result);
    }
    
    [Fact]
    public void NullLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new NullLangValue();
        
        // Act
        var result = value.TypeToString();
        
        // Assert
        Assert.Equal("Null", result);
    }
    
    [Fact]
    public void TypeLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new TypeLangValue("testType");
        
        // Act
        var result = value.TypeToString();
        
        // Assert
        Assert.Equal("Type", result);
    }
}