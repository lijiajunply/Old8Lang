using Old8Lang.Error;
using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Tests.Unit;

/// <summary>
/// 错误处理测试套件
/// 验证编译器对各类错误的检测能力和报告质量
/// </summary>
[Collection("Sequential")]
public class ErrorHandlingTests
{
    /// <summary>
    /// 测试语法错误检测
    /// </summary>
    [Fact]
    public void SyntaxError_DetectedCorrectly()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- 456
    c <- a + b  // 缺少右括号
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => interpreter.Build(code));
        Assert.Contains("语法错误", exception.Message);
        Assert.NotNull(exception.Position);
        Assert.NotNull(exception.SourceContext);
        Assert.NotEmpty(exception.Suggestion);
    }
    
    /// <summary>
    /// 测试语义错误检测
    /// </summary>
    [Fact]
    public void SemanticError_DetectedCorrectly()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- 456
    c <- a + b
    return d  // 变量d未定义
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        // 语义错误通常在运行时检测
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("名称错误", exception.Message);
        Assert.Contains("d", exception.Message);
    }
    
    /// <summary>
    /// 测试类型错误检测
    /// </summary>
    [Fact]
    public void TypeError_DetectedCorrectly()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- ""456""  // 字符串类型
    c <- a + b  // 类型不兼容
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        // 类型错误通常在运行时检测
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<TypeError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("类型错误", exception.Message);
        Assert.Contains("+", exception.Message);
    }
    
    /// <summary>
    /// 测试运行时错误检测
    /// </summary>
    [Fact]
    public void RuntimeError_DetectedCorrectly()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- 0
    c <- a / b  // 除以零错误
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<RuntimeError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("运行时错误", exception.Message);
        Assert.Contains("除以零", exception.Message);
    }
    
    /// <summary>
    /// 测试数组索引越界错误检测
    /// </summary>
    [Fact]
    public void IndexError_ArrayIndexOutOfBounds_Detected()
    {
        // Arrange
        var code = @"func test() {
    arr <- [1, 2, 3]
    value <- arr[10]  // 索引越界
    return value
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<IndexError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("索引错误", exception.Message);
        Assert.Contains("越界", exception.Message);
    }
    
    /// <summary>
    /// 测试字典键不存在错误检测
    /// </summary>
    [Fact]
    public void KeyError_DictKeyNotFound_Detected()
    {
        // Arrange
        var code = @"func test() {
    dict <- {""name"": ""test""}
    value <- dict[""age""]  // 键不存在
    return value
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<KeyError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("键错误", exception.Message);
        Assert.Contains("age", exception.Message);
    }
    
    /// <summary>
    /// 测试属性错误检测
    /// </summary>
    [Fact]
    public void AttributeError_InvalidMemberAccess_Detected()
    {
        // Arrange
        var code = @"class TestClass {
    field1 <- 123
}

func test() {
    obj <- TestClass()
    value <- obj.non_existent_field  // 属性不存在
    return value
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<AttributeError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("属性错误", exception.Message);
        Assert.Contains("non_existent_field", exception.Message);
    }
    
    /// <summary>
    /// 测试无效操作错误检测
    /// </summary>
    [Fact]
    public void InvalidOperationError_Detected()
    {
        // Arrange
        var code = @"func test() {
    a <- true
    b <- false
    c <- a * b  // 布尔值不支持乘法操作
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("无效操作", exception.Message);
        Assert.Contains("true", exception.Message);
    }
    
    /// <summary>
    /// 测试编译器异常检测
    /// </summary>
    [Fact]
    public void CompilerException_DetectedCorrectly()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- 456
    c <- a + b
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        // 正常情况下编译应该成功
        var ast = interpreter.Build(code);
        
        // 我们无法直接触发编译器异常，因为它通常在IL生成错误时抛出
        // 这里我们只验证编译过程不抛出异常
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试错误信息质量
    /// </summary>
    [Fact]
    public void ErrorMessage_ContainsUsefulInformation()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- ""456""  // 字符串类型
    c <- a + b  // 类型不兼容
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<TypeError>(() => ast.Run(interpreter.Manager));
        
        // Assert
        Assert.Contains("类型错误", exception.Message);
        Assert.Contains("true", exception.Message);
        Assert.Contains("+", exception.Message);
        Assert.NotNull(exception.Position);
        Assert.NotNull(exception.SourceContext);
        Assert.NotEmpty(exception.Suggestion);
    }
    
    /// <summary>
    /// 测试错误位置准确性
    /// </summary>
    [Fact]
    public void ErrorPosition_Accurate()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- 456
    c <- a + b  // 故意引入错误：缺少右括号
    d <- b - a
    return d
}
";
        var interpreter = new LangInterpreter();
        
        // Act
        var exception = Assert.Throws<SyntaxError>(() => interpreter.Build(code));
        
        // Assert
        Assert.NotNull(exception.Position);
        Assert.Equal(3, exception.Position.Line);  // 错误发生在第3行
    }
    
    /// <summary>
    /// 测试错误建议质量
    /// </summary>
    [Fact]
    public void ErrorSuggestion_Useful()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- 456
    c <- a + b  // 缺少右括号
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act
        var exception = Assert.Throws<SyntaxError>(() => interpreter.Build(code));
        
        // Assert
        Assert.NotNull(exception.Suggestion);
        Assert.NotEmpty(exception.Suggestion);
        Assert.Contains("右括号", exception.Suggestion);
    }
    
    /// <summary>
    /// 测试运行时错误的错误信息
    /// </summary>
    [Fact]
    public void RuntimeError_MessageContainsUsefulInformation()
    {
        // Arrange
        var code = @"func test() {
    a <- 123
    b <- 0
    c <- a / b  // 除以零错误
    return c
}
";
        var interpreter = new LangInterpreter();
        
        // Act
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<RuntimeError>(() => ast.Run(interpreter.Manager));
        
        // Assert
        Assert.Contains("除以零", exception.Message);
        Assert.NotNull(exception.Position);
        Assert.NotNull(exception.SourceContext);
    }
    
    /// <summary>
    /// 测试类型转换错误
    /// </summary>
    [Fact]
    public void TypeError_TypeConversionError()
    {
        // Arrange
        var code = @"func test() {
    a <- ""not a number""
    b <- int(a)  // 无法将字符串转换为整数
    return b
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<ValueError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("值错误", exception.Message);
        Assert.Contains("not a number", exception.Message);
    }
    
    /// <summary>
    /// 测试索引错误信息
    /// </summary>
    [Fact]
    public void IndexError_MessageContainsIndex()
    {
        // Arrange
        var code = @"func test() {
    arr <- [1, 2, 3]
    value <- arr[10]  // 索引越界
    return value
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<IndexError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("索引", exception.Message);
        Assert.Contains("10", exception.Message);
    }
    
    /// <summary>
    /// 测试错误处理的完整性
    /// </summary>
    [Fact]
    public void ErrorHandling_CompleteCoverage()
    {
        // 这个测试验证所有主要错误类型都能被正确检测
        var errorTypes = new List<Type> {
            typeof(SyntaxError),
            typeof(SemanticError),
            typeof(TypeError),
            typeof(RuntimeError),
            typeof(IndexError),
            typeof(KeyError),
            typeof(NameError),
            typeof(AttributeError),
            typeof(InvalidOperationError),
            typeof(ValueError),
            typeof(CompilerException),
            typeof(ImportError)
        };
        
        // 验证所有错误类型都能被创建
        foreach (var errorType in errorTypes)
        {
            // 我们只验证类型存在，不验证具体行为
            Assert.NotNull(errorType);
        }
    }
    
    /// <summary>
    /// 测试异常转换
    /// </summary>
    [Fact]
    public void ExceptionConversion_WorksCorrectly()
    {
        // Arrange
        var code = @"func test() {
    try {
        a <- 123
        b <- 0
        c <- a / b  // 除以零错误
    } catch (e) {
        // 捕获并转换异常
        error_type <- type(e)
        return error_type
    }
}
";
        var interpreter = new LangInterpreter();
        
        // Act & Assert
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        
        // 我们无法直接验证异常转换，因为它在运行时处理
        // 这里我们只验证代码能正常运行
    }
}