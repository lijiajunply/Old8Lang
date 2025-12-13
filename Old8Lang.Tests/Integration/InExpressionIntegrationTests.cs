using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Integration;

/// <summary>
/// In表达式集成测试
/// 测试in表达式和for-in循环的实际运行效果
/// </summary>
[Collection("Sequential")]
public class InExpressionIntegrationTests
{
    #region in表达式运行测试

    /// <summary>
    /// 测试基本in表达式在数组上的运行效果
    /// </summary>
    [Fact]
    public void InExpression_Array_ShouldReturnTrueWhenExists()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "a <- 1 in [1, 2, 3]; b <- 4 in [1, 2, 3]";
        
        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        
        // Assert
        Assert.True((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("a")))!.Value);
        Assert.False((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("b")))!.Value);
    }

    /// <summary>
    /// 测试in表达式在字符串上的运行效果
    /// </summary>
    [Fact]
    public void InExpression_String_ShouldReturnTrueWhenExists()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "a <- 'a' in 'abc'; b <- 'd' in 'abc'";
        
        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        
        // Assert
        Assert.True((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("a")))!.Value);
        Assert.False((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("b")))!.Value);
    }

    /// <summary>
    /// 测试in表达式在字典上的运行效果
    /// </summary>
    [Fact]
    public void InExpression_Dictionary_ShouldReturnTrueWhenKeyExists()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "a <- 'name' in {'name': 'test', 'age': 10}; b <- 'gender' in {'name': 'test', 'age': 10}";
        
        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        
        // Assert
        Assert.True((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("a")))!.Value);
        Assert.False((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("b")))!.Value);
    }

    /// <summary>
    /// 测试in表达式在范围上的运行效果
    /// </summary>
    [Fact]
    public void InExpression_Range_ShouldReturnTrueWhenInRange()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "a <- 3 in [1~5]; b <- 6 in [1~5]";
        
        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        
        // Assert
        Assert.True((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("a")))!.Value);
        Assert.False((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("b")))!.Value);
    }

    #endregion

    #region for-in循环运行测试

    /// <summary>
    /// 测试for-in循环在数组上的运行效果
    /// </summary>
    [Fact]
    public void ForInLoop_Array_ShouldIterateAllElements()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "sum <- 0; for item in [1, 2, 3, 4, 5] { sum <- sum + item }";
        
        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        
        // Assert
        Assert.Equal(15, ((IntLangValue)interpreter.Manager.GetValue(new LangId("sum")))!.Value);
    }

    /// <summary>
    /// 测试for-in循环与in表达式在同一代码中的运行效果
    /// </summary>
    [Fact]
    public void ForInLoopAndInExpression_ShouldWorkTogether()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"numbers <- [1, 2, 3, 4, 5]
                     evenCount <- 0
                     for num in numbers {
                         if num in [2, 4, 6, 8, 10] {
                             evenCount <- evenCount + 1
                         }
                     }";
        
        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        
        // Assert
        Assert.Equal(2, ((IntLangValue)interpreter.Manager.GetValue(new LangId("evenCount")))!.Value);
    }

    #endregion

    #region 编译模式测试

    /// <summary>
    /// 测试in表达式在编译模式下的运行效果
    /// </summary>
    [Fact]
    public void CompileMode_InExpression_ShouldWork()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "a <- 1 in [1, 2, 3]";
        
        // Act
        var ast = interpreter.Build(code);
        var compiled = Old8Lang.Compiler.Compiler.Compile(ast, "test.old8", interpreter);
        compiled();
        
        // Assert
        Assert.True((bool)((BoolLangValue)interpreter.Manager.GetValue(new LangId("a")))!.Value);
    }

    /// <summary>
    /// 测试for-in循环在编译模式下的运行效果
    /// </summary>
    [Fact]
    public void CompileMode_ForInLoop_ShouldWork()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "sum <- 0; for item in [1, 2, 3] { sum <- sum + item }";
        
        // Act
        var ast = interpreter.Build(code);
        var compiled = Old8Lang.Compiler.Compiler.Compile(ast, "test.old8", interpreter);
        compiled();
        
        // Assert
        Assert.Equal(6, ((IntLangValue)interpreter.Manager.GetValue(new LangId("sum")))!.Value);
    }

    #endregion
}
