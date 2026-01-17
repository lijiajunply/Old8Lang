using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Classes;

/// <summary>
/// 虚拟机模式 Super 表达式测试
/// </summary>
[Collection("Sequential")]
public class VMSuperExpressionTests
{
    [Fact]
    public void SuperExpression_BasicInheritance_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Animal {
                public name:string

                func init(n:string) -> void {
                    this.name <- n
                }

                func speak() -> string {
                    return ""Animal sound""
                }
            }

            class Dog extends Animal {
                func speak() -> string {
                    return ""Woof""
                }
            }

            dog <- Dog(""Buddy"")
            result <- dog.speak()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Woof", result);
    }

    [Fact]
    public void SuperExpression_CallParentMethod_ReturnsParentResult()
    {
        // Arrange
        var code = @"
            class Animal {
                func speak() -> string {
                    return ""Animal sound""
                }
            }

            class Dog extends Animal {
                func speak() -> string {
                    return ""Woof""
                }

                func callParentSpeak() -> string {
                    return super.speak()
                }
            }

            dog <- Dog()
            result <- dog.callParentSpeak()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Animal sound", result);
    }

    [Fact]
    public void SuperExpression_AccessParentField_ReturnsFieldValue()
    {
        // Arrange
        var code = @"
            class Animal {
                public species:string

                func init(s:string) -> void {
                    this.species <- s
                }
            }

            class Dog extends Animal {
                func getSpecies() -> string {
                    return super.species
                }
            }

            dog <- Dog(""Canine"")
            result <- dog.getSpecies()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Canine", result);
    }

    [Fact]
    public void SuperExpression_CallParentConstructor_InitializesCorrectly()
    {
        // Arrange
        var code = @"
            class Animal {
                public name:string

                func init(n:string) -> void {
                    this.name <- n
                }
            }

            class Dog extends Animal {
                public breed:string

                func init(n:string, b:string) -> void {
                    super.init(n)
                    this.breed <- b
                }
            }

            dog <- Dog(""Buddy"", ""Golden Retriever"")
            result <- dog.name
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Buddy", result);
    }

    [Fact]
    public void SuperExpression_MultiLevelInheritance_CallsGrandparentMethod()
    {
        // Arrange
        var code = @"
            class Animal {
                func makeSound() -> string {
                    return ""Generic sound""
                }
            }

            class Mammal extends Animal {
                func makeSound() -> string {
                    return ""Mammal sound""
                }
            }

            class Dog extends Mammal {
                func callGrandparent() -> string {
                    return super.makeSound()
                }
            }

            dog <- Dog()
            result <- dog.callGrandparent()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Mammal sound", result);
    }

    [Fact]
    public void SuperExpression_CombineParentAndChildResults_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            class Animal {
                func speak() -> string {
                    return ""Animal""
                }
            }

            class Dog extends Animal {
                func speak() -> string {
                    parentSound <- super.speak()
                    return parentSound + "" - Woof""
                }
            }

            dog <- Dog()
            result <- dog.speak()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Animal - Woof", result);
    }

    [Fact]
    public void SuperExpression_SetParentField_ModifiesFieldCorrectly()
    {
        // Arrange
        var code = @"
            class Animal {
                public age:int

                func init() -> void {
                    this.age <- 0
                }
            }

            class Dog extends Animal {
                func setAge(a:int) -> void {
                    super.age <- a
                }

                func getAge() -> int {
                    return this.age
                }
            }

            dog <- Dog()
            dog.setAge(5)
            result <- dog.getAge()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, result);
    }

    [Fact]
    public void SuperExpression_WithParameters_PassesCorrectly()
    {
        // Arrange
        var code = @"
            class Animal {
                func greet(name:string) -> string {
                    return ""Hello, "" + name
                }
            }

            class Dog extends Animal {
                func greetOwner(owner:string) -> string {
                    return super.greet(owner) + "" from Dog""
                }
            }

            dog <- Dog()
            result <- dog.greetOwner(""Alice"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello, Alice from Dog", result);
    }
}
