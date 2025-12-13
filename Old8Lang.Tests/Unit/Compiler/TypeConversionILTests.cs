using System.Reflection;
using System.Reflection.Emit;
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
        operation.LoadIlValue(ilGenerator, local);
        var resultType = operation.OutputType(local);
        
        // Assert
        Assert.Equal(typeof(string), resultType);
    }
    
    [Fact]
    public void BoolToIntConversionIL()
    {
        // Arrange
        var left = new BoolLangValue(true);
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
    public void IntToBoolConversionIL()
    {
        // Arrange
        var left = new IntLangValue(42);
        var right = new LangId("bool");
        var operation = new Operation(left, LangTokenType.As, right);
        var ilGenerator = GetILGenerator(out var methodBuilder);
        var local = new LocalManager();
        
        // Act
        operation.LoadIlValue(ilGenerator, local);
        var resultType = operation.OutputType(local);
        
        // Assert
        Assert.Equal(typeof(bool), resultType);
    }
    
    [Fact]
    public void StringToIntConversionIL()
    {
        // Arrange
        var left = new StringLangValue("42");
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
    public void CharToIntConversionIL()
    {
        // Arrange
        var left = new CharLangValue('A');
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
    public void IntToCharConversionIL()
    {
        // Arrange
        var left = new IntLangValue(65);
        var right = new LangId("char");
        var operation = new Operation(left, LangTokenType.As, right);
        var ilGenerator = GetILGenerator(out var methodBuilder);
        var local = new LocalManager();
        
        // Act
        operation.LoadIlValue(ilGenerator, local);
        var resultType = operation.OutputType(local);
        
        // Assert
        Assert.Equal(typeof(char), resultType);
    }
    
    private static System.Reflection.Emit.ILGenerator GetILGenerator(out MethodBuilder methodBuilder)
    {
        // Create a dynamic assembly
        var assemblyName = new AssemblyName("TestAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        
        // Create a dynamic module
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");
        
        // Create a dynamic type
        var typeBuilder = moduleBuilder.DefineType("TestType", TypeAttributes.Public);
        
        // Create a dynamic method
        methodBuilder = typeBuilder.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        
        // Get the IL generator
        return methodBuilder.GetILGenerator();
    }
}