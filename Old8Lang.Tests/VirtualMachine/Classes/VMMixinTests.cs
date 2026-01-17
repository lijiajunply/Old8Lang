using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Classes;

/// <summary>
/// 虚拟机Mixin测试
/// </summary>
public class VMMixinTests
{
    [Fact]
    public void Mixin_SimpleMixin_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            mixin Loggable {
                func log(message:string) -> void {
                    result <- ""[LOG] "" + message
                }
            }

            class User with Loggable {
                public name:string

                func init(n:string) -> void {
                    this.name <- n
                }
            }

            u <- User(""Alice"")
            u.log(""User created"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("[LOG] User created", result);
    }

    [Fact]
    public void Mixin_MultipleMixins_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            mixin Loggable {
                func log(message:string) -> void {
                    result1 <- ""[LOG] "" + message
                }
            }

            mixin Timestampable {
                func getTimestamp() -> string {
                    return ""2024-01-01""
                }
            }

            class Document with Loggable, Timestampable {
                public title:string

                func init(t:string) -> void {
                    this.title <- t
                }

                func info() -> void {
                    result2 <- this.title + "" - "" + this.getTimestamp()
                }
            }

            d <- Document(""Report"")
            d.log(""Document created"")
            d.info()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        Assert.NotNull(result1);
        Assert.Equal("[LOG] Document created", result1);

        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result2);
        Assert.Equal("Report - 2024-01-01", result2);
    }

    [Fact]
    public void Mixin_WithInterfaceAndMixin_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            interface IPrintable {
                func print() -> void
            }

            mixin Loggable {
                func log(message:string) -> void {
                    result1 <- ""[LOG] "" + message
                }
            }

            class Article implements IPrintable with Loggable {
                public content:string

                func init(c:string) -> void {
                    this.content <- c
                }

                func print() -> void {
                    result2 <- ""Printing: "" + this.content
                }
            }

            a <- Article(""Hello World"")
            a.log(""Article created"")
            a.print()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        Assert.NotNull(result1);
        Assert.Equal("[LOG] Article created", result1);

        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result2);
        Assert.Equal("Printing: Hello World", result2);
    }
}
