using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Classes;

/// <summary>
/// 虚拟机接口测试
/// </summary>
public class VMInterfaceTests
{
    [Fact]
    public void Interface_SimpleInterface_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            interface IDrawable {
                func draw() -> void
            }

            class Circle implements IDrawable {
                public radius:int

                func init(r:int) -> void {
                    this.radius <- r
                }

                func draw() -> void {
                    result <- ""Drawing circle with radius: "" + this.radius.ToStr()
                }
            }

            c <- Circle(5)
            c.draw()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Drawing circle with radius: 5", result);
    }

    [Fact]
    public void Interface_MultipleInterfaces_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            interface IDrawable {
                func draw() -> void
            }

            interface IResizable {
                func resize(factor:int) -> void
            }

            class Rectangle implements IDrawable, IResizable {
                public width:int
                public height:int

                func init(w:int, h:int) -> void {
                    this.width <- w
                    this.height <- h
                }

                func draw() -> void {
                    result1 <- ""Drawing rectangle: "" + this.width.ToStr() + ""x"" + this.height.ToStr()
                }

                func resize(factor:int) -> void {
                    this.width <- this.width * factor
                    this.height <- this.height * factor
                }
            }

            r <- Rectangle(10, 20)
            r.draw()
            r.resize(2)
            result2 <- r.width
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        Assert.NotNull(result1);
        Assert.Equal("Drawing rectangle: 10x20", result1);

        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result2);
        Assert.Equal(20, result2);
    }
}
