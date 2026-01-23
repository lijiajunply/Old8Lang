using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Classes;

/// <summary>
/// 虚拟机类声明测试
/// </summary>
public class VMClassDeclarationTests
{
    [Fact]
    public void ClassDeclaration_SimpleClass_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string
                public age:int
            }

            p <- Person()
            p.name <- ""Alice""
            p.age <- 30
            result <- p.name
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void ClassDeclaration_WithConstructor_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string
                public age:int

                func init(n:string, a:int) -> void {
                    this.name <- n
                    this.age <- a
                }
            }

            p <- Person(""Bob"", 25)
            result <- p.age
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(25, result);
    }
}
