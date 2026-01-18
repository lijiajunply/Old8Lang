using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机字符串模板测试
/// 测试字符串模板（$"..."）的各种用法
/// </summary>
public class VMStringTemplateTests
{
    [Fact]
    public void StringTemplate_SimpleVariable_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            result <- $""Hello, {name}!""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public void StringTemplate_MultipleVariables_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            firstName <- ""John""
            lastName <- ""Doe""
            result <- $""{firstName} {lastName}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("John Doe", result);
    }

    [Fact]
    public void StringTemplate_WithExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 3
            result <- $""{x} + {y} = {x + y}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("5 + 3 = 8", result);
    }

    [Fact]
    public void StringTemplate_WithMethodCall_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getName() -> string {
                return ""Bob""
            }

            result <- $""User: {getName()}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("User: Bob", result);
    }

    [Fact]
    public void StringTemplate_WithNumberTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            intValue <- 42
            doubleValue <- 3.14
            result <- $""Int: {intValue}, Double: {doubleValue}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Int: 42, Double: 3.14", result);
    }

    [Fact]
    public void StringTemplate_WithBooleanType_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            flag <- true
            result <- $""Flag is: {flag}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Flag is: True", result);
    }

    [Fact]
    public void StringTemplate_WithObjectProperty_ExecutesCorrectly()
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

            person <- Person(""Alice"", 30)
            result <- $""{person.name} is {person.age} years old""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Alice is 30 years old", result);
    }

    [Fact]
    public void StringTemplate_NestedTemplate_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            greeting <- $""Hello, {name}""
            result <- $""Message: {greeting}!""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Message: Hello, Alice!", result);
    }

    [Fact]
    public void StringTemplate_WithListAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {10, 20, 30}
            result <- $""First: {numbers[0]}, Last: {numbers[2]}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("First: 10, Last: 30", result);
    }

    [Fact]
    public void StringTemplate_WithComplexExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            result <- $""Result: {(a + b) * 2 - 3}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Result: 27", result);
    }

    [Fact]
    public void StringTemplate_EmptyExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- $""No interpolation here""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("No interpolation here", result);
    }

    [Fact]
    public void StringTemplate_WithNullValue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            value <- null
            result <- $""Value: {value}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Contains("null", result.ToString().ToLower());
    }

    [Fact]
    public void StringTemplate_InLoop_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            results <- {}
            for i in [1~3] {
                results.Add($""Number: {i}"")
            }
            result <- results[1]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Number: 2", result);
    }

    [Fact]
    public void StringTemplate_WithToStrMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Point {
                public x:int
                public y:int

                func init(xVal:int, yVal:int) -> void {
                    this.x <- xVal
                    this.y <- yVal
                }

                func ToStr() -> string {
                    return $""({this.x}, {this.y})""
                }
            }

            point <- Point(10, 20)
            result <- $""Point: {point.ToStr()}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Point: (10, 20)", result);
    }
}