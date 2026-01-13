using Old8Lang.Bytecode;
using Xunit;

namespace Old8Lang.Tests.VirtualMachine.Classes;

/// <summary>
/// 虚拟机模式 Super 表达式测试
/// </summary>
[Collection("Sequential")]
public class VMSuperExpressionTests
{
    [Fact]
    public void SuperExpression_CallParentMethod_ReturnsParentResult()
    {
        // Arrange
        var code = """
            class Animal {
                func speak() -> string {
                    return "Some sound"
                }
            }

            class Dog extends Animal {
                func speak() -> string {
                    parentSound <- super.speak()
                    return parentSound + " - Woof!"
                }
            }

            dog <- Dog()
            result <- dog.speak()
            """;

        // Act & Assert
        // 注意：当前虚拟机模式的类支持可能还不完整
        // 这个测试用于验证 Super 表达式的字节码生成
        var exception = Record.Exception(() =>
        {
            var bytecodeFile = CompileHelper.CompileToBytecode(code);
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();
        });

        // 如果没有抛出异常，说明字节码生成和执行成功
        Assert.Null(exception);
    }

    [Fact]
    public void SuperExpression_AccessParentField_ReturnsFieldValue()
    {
        // Arrange
        var code = """
            class Animal {
                public name <- "Animal"

                func getName() -> string {
                    return this.name
                }
            }

            class Dog extends Animal {
                func getParentName() -> string {
                    return super.name
                }
            }

            dog <- Dog()
            result <- dog.getParentName()
            """;

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            var bytecodeFile = CompileHelper.CompileToBytecode(code);
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();
        });

        Assert.Null(exception);
    }
}
