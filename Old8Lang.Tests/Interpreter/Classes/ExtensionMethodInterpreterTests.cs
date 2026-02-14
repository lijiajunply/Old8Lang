using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 扩展方法解释器模式测试
/// </summary>
[Collection("Sequential")]
public class ExtensionMethodInterpreterTests
{
    /// <summary>
    /// 测试基本扩展方法执行
    /// </summary>
    [Fact]
    public void Execute_BasicExtensionMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func double() -> int {
                    return this * 2
                }
            }

            x <- 5
            result <- x.double()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试扩展方法带参数
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func add(n:int) -> int {
                    return this + n
                }
            }

            x <- 5
            result <- x.add(10)
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试字符串扩展方法
    /// </summary>
    [Fact]
    public void Execute_StringExtensionMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension string {
                func repeat(n:int) -> string {
                    result <- """"
                    i <- 0
                    while i < n {
                        result <- result + this
                        i <- i + 1
                    }
                    return result
                }
            }

            text <- ""Hello""
            result <- text.repeat(3)
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("HelloHelloHello", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试列表扩展方法
    /// </summary>
    [Fact]
    public void Execute_ListExtensionMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension list {
                func sum() -> int {
                    total <- 0
                    for item in this {
                        total <- total + item
                    }
                    return total
                }
            }

            numbers <- [1, 2, 3, 4, 5]
            result <- numbers.sum()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个扩展方法
    /// </summary>
    [Fact]
    public void Execute_MultipleExtensionMethods_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func double() -> int {
                    return this * 2
                }

                func triple() -> int {
                    return this * 3
                }

                func isEven() -> bool {
                    return this % 2 == 0
                }
            }

            x <- 5
            doubled <- x.double()
            tripled <- x.triple()
            isEven <- x.isEven()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var doubled = interpreter.Manager.GetValue(new LangId("doubled"));
        var tripled = interpreter.Manager.GetValue(new LangId("tripled"));
        var isEven = interpreter.Manager.GetValue(new LangId("isEven"));

        Assert.Equal(10, ((IntLangValue)doubled).Value);
        Assert.Equal(15, ((IntLangValue)tripled).Value);
        Assert.False(((BoolLangValue)isEven).Value);
    }

    /// <summary>
    /// 测试扩展方法链式调用
    /// </summary>
    [Fact]
    public void Execute_ChainedExtensionMethods_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func add(n:int) -> int {
                    return this + n
                }

                func multiply(n:int) -> int {
                    return this * n
                }
            }

            x <- 5
            result <- x.add(3).multiply(2)
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(16, ((IntLangValue)result).Value); // (5 + 3) * 2 = 16
    }

    /// <summary>
    /// 测试扩展方法访问this
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodAccessingThis_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func square() -> int {
                    return this * this
                }

                func cube() -> int {
                    return this * this * this
                }
            }

            x <- 3
            squared <- x.square()
            cubed <- x.cube()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var squared = interpreter.Manager.GetValue(new LangId("squared"));
        var cubed = interpreter.Manager.GetValue(new LangId("cubed"));

        Assert.Equal(9, ((IntLangValue)squared).Value);
        Assert.Equal(27, ((IntLangValue)cubed).Value);
    }

    /// <summary>
    /// 测试多个参数的扩展方法
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithMultipleParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func between(min:int, max:int) -> bool {
                    return this >= min and this <= max
                }
            }

            x <- 5
            result1 <- x.between(1, 10)
            result2 <- x.between(6, 10)
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.True(((BoolLangValue)result1).Value);
        Assert.False(((BoolLangValue)result2).Value);
    }

    /// <summary>
    /// 测试扩展方法返回不同类型
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodReturningDifferentType_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func toStr() -> string {
                    return this.ToStr()
                }

                func isPositive() -> bool {
                    return this > 0
                }
            }

            x <- 42
            str <- x.toStr()
            positive <- x.isPositive()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var str = interpreter.Manager.GetValue(new LangId("str"));
        var positive = interpreter.Manager.GetValue(new LangId("positive"));

        Assert.IsType<StringLangValue>(str);
        Assert.Equal("42", ((StringLangValue)str).Value);
        Assert.True(((BoolLangValue)positive).Value);
    }

    /// <summary>
    /// 测试为不同类型添加同名扩展方法
    /// </summary>
    [Fact]
    public void Execute_SameMethodNameForDifferentTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func getValue() -> int {
                    return this
                }
            }

            extension string {
                func getValue() -> string {
                    return this
                }
            }

            x <- 42
            s <- ""hello""
            intValue <- x.getValue()
            strValue <- s.getValue()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intValue = interpreter.Manager.GetValue(new LangId("intValue"));
        var strValue = interpreter.Manager.GetValue(new LangId("strValue"));

        Assert.IsType<IntLangValue>(intValue);
        Assert.Equal(42, ((IntLangValue)intValue).Value);
        Assert.IsType<StringLangValue>(strValue);
        Assert.Equal("hello", ((StringLangValue)strValue).Value);
    }

    /// <summary>
    /// 测试扩展方法中的局部变量
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithLocalVariables_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func factorial() -> int {
                    result <- 1
                    i <- 1
                    while i <= this {
                        result <- result * i
                        i <- i + 1
                    }
                    return result
                }
            }

            x <- 5
            result <- x.factorial()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(120, ((IntLangValue)result).Value); // 5! = 120
    }

    /// <summary>
    /// 测试扩展方法使用内置函数
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodUsingBuiltInFunctions_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func doubleAndPrint() -> int {
                    result <- this * 2
                    Print(result.ToStr())
                    return result
                }
            }

            x <- 5
            result <- x.doubleAndPrint()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试扩展方法中的条件语句
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithConditionals_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func abs() -> int {
                    if this < 0 {
                        return -this
                    }
                    return this
                }
            }

            x <- -5
            y <- 10
            absX <- x.abs()
            absY <- y.abs()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var absX = interpreter.Manager.GetValue(new LangId("absX"));
        var absY = interpreter.Manager.GetValue(new LangId("absY"));

        Assert.Equal(5, ((IntLangValue)absX).Value);
        Assert.Equal(10, ((IntLangValue)absY).Value);
    }

    /// <summary>
    /// 测试扩展方法中的循环
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithLoops_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func sumUpTo() -> int {
                    sum <- 0
                    for i in [1 ~ this] {
                        sum <- sum + i
                    }
                    return sum
                }
            }

            x <- 10
            result <- x.sumUpTo()
        ";

        // Act
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // 1+2+3+...+10 = 55
    }
}
