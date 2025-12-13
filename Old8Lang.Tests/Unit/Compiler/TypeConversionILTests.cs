using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Unit.Compiler;

[Collection("Sequential")]
public class TypeConversionILTests
{
    [Fact]
    public void IntToDoubleConversionIL()
    {
        // Arrange
        var left = new IntLangValue(42);
        var right = new LangId("double");
        var operation = new Operation(left, LangTokenType.As, right);
        var ilGenerator = GetILGenerator(out var methodBuilder);
        var local = new LocalManager();
        
        // Act
        operation.LoadIlValue(ilGenerator, local);
        var resultType = operation.OutputType(local);
        
        // Assert
        Assert.Equal(typeof(double), resultType);
    }
    
    [Fact]
    public void DoubleToIntConversionIL()
    {
        // Arrange
        var left = new DoubleLangValue(3.14);
        var right = new LangId("int");
        var operation = new Operation(left, LangTokenType.As, right);
        var ilGenerator = GetILGenerator(out var methodBuilder);
        var local = new LocalManager();
        
        // Act
        operation.LoadIlValue(ilGenerator, local);
        var resultType = operation.OutputType(local);
        
        // Assert
        Assert.Equal(typeof(int), resultType);
    }
    
    [Fact]
    public void IntToStringConversionIL()
    {
        // Arrange
        var left = new IntLangValue(42);
        var right = new LangId("string");
        var operation = new Operation(left, LangTokenType.As, right);
        var ilGenerator = GetILGenerator(out var methodBuilder);
        var local = new LocalManager();
        
        // Act
        operation.LoadIlValue