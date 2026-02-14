using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser;

/// <summary>
/// 扩展方法语法解析测试
/// </summary>
[Collection("Sequential")]
public class ExtensionMethodTests
{
    /// <summary>
    /// 测试基本扩展方法声明解析
    /// </summary>
    [Fact]
    public void ParseProgram_BasicExtensionMethod_ParsesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func double() -> int {
                    return this * 2
                }
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        Assert.IsType<ExtensionDeclaration>(program[0]);

        var extension = (ExtensionDeclaration)program[0];
        Assert.Equal("int", extension.TargetTypeName);
        Assert.Equal(1, extension.ExtensionMethods.Count);
        Assert.Equal("double", extension.ExtensionMethods[0].Id?.IdName);
    }

    /// <summary>
    /// 测试多个扩展方法声明
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleExtensionMethods_ParsesCorrectly()
    {
        // Arrange
        var code = @"
            extension string {
                func repeat(n:int) -> string {
                    return this
                }

                func reverse() -> string {
                    return this
                }

                func toUpper() -> string {
                    return this
                }
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        Assert.IsType<ExtensionDeclaration>(program[0]);

        var extension = (ExtensionDeclaration)program[0];
        Assert.Equal("string", extension.TargetTypeName);
        Assert.Equal(3, extension.ExtensionMethods.Count);
        Assert.Equal("repeat", extension.ExtensionMethods[0].Id?.IdName);
        Assert.Equal("reverse", extension.ExtensionMethods[1].Id?.IdName);
        Assert.Equal("toUpper", extension.ExtensionMethods[2].Id?.IdName);
    }

    /// <summary>
    /// 测试为列表类型添加扩展方法
    /// </summary>
    [Fact]
    public void ParseProgram_ListExtensionMethod_ParsesCorrectly()
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
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        Assert.IsType<ExtensionDeclaration>(program[0]);

        var extension = (ExtensionDeclaration)program[0];
        Assert.Equal("list", extension.TargetTypeName);
        Assert.Equal(1, extension.ExtensionMethods.Count);
        Assert.Equal("sum", extension.ExtensionMethods[0].Id?.IdName);
    }

    /// <summary>
    /// 测试扩展方法带参数
    /// </summary>
    [Fact]
    public void ParseProgram_ExtensionMethodWithParameters_ParsesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func add(n:int) -> int {
                    return this + n
                }

                func multiply(a:int, b:int) -> int {
                    return this * a * b
                }
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        var extension = (ExtensionDeclaration)program[0];
        Assert.Equal(2, extension.ExtensionMethods.Count);

        // 检查第一个方法的参数
        Assert.Equal(1, extension.ExtensionMethods[0].Ids.Count);
        Assert.Equal("n", extension.ExtensionMethods[0].Ids[0].IdName);

        // 检查第二个方法的参数
        Assert.Equal(2, extension.ExtensionMethods[1].Ids.Count);
        Assert.Equal("a", extension.ExtensionMethods[1].Ids[0].IdName);
        Assert.Equal("b", extension.ExtensionMethods[1].Ids[1].IdName);
    }

    /// <summary>
    /// 测试扩展方法至少需要一个方法
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyExtensionBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
            extension int {
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        // 空扩展块应该抛出语法错误
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试多个扩展声明
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleExtensionDeclarations_ParsesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func double() -> int {
                    return this * 2
                }
            }

            extension string {
                func reverse() -> string {
                    return this
                }
            }

            extension list {
                func sum() -> int {
                    return 0
                }
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(3, program.Count);

        // 验证每个语句都是ExtensionDeclaration类型
        for (int i = 0; i < program.Count; i++)
        {
            Assert.IsType<ExtensionDeclaration>(program[i]);
        }

        // 验证目标类型名称
        Assert.Equal("int", ((ExtensionDeclaration)program[0]).TargetTypeName);
        Assert.Equal("string", ((ExtensionDeclaration)program[1]).TargetTypeName);
        Assert.Equal("list", ((ExtensionDeclaration)program[2]).TargetTypeName);
    }

    /// <summary>
    /// 测试缺少目标类型名称（应该报错）
    /// </summary>
    [Fact]
    public void ParseProgram_MissingTargetType_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
            extension {
                func test() -> void {
                }
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少花括号（应该报错）
    /// </summary>
    [Fact]
    public void ParseProgram_MissingBraces_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
            extension int
                func test() -> void {
                }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试扩展方法中使用this关键字
    /// </summary>
    [Fact]
    public void ParseProgram_ExtensionMethodWithThisKeyword_ParsesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func square() -> int {
                    return this * this
                }

                func isPositive() -> bool {
                    return this > 0
                }
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        Assert.IsType<ExtensionDeclaration>(program[0]);

        var extension = (ExtensionDeclaration)program[0];
        Assert.Equal(2, extension.ExtensionMethods.Count);
    }
}
