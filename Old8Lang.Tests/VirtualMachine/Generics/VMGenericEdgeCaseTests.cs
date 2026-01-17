using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机泛型边界测试 - 测试泛型的边界情况和特殊场景
/// </summary>
public class VMGenericEdgeCaseTests
{
    [Fact]
    public void GenericClass_SameTypeMultipleTimes_ExecutesCorrectly()
    {
        // Arrange - 同一类型多次实例化
        var code = @"
            class Box<T> {
                public value:T
            }

            box1 <- Box<int>()
            box1.value <- 10

            box2 <- Box<int>()
            box2.value <- 20

            box3 <- Box<int>()
            box3.value <- 30

            result <- box1.value + box2.value + box3.value
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.Equal(60, result);
    }

    [Fact]
    public void GenericClass_DifferentNumericTypes_ExecutesCorrectly()
    {
        // Arrange - 不同数值类型的泛型类
        var code = @"
            class Container<T> {
                public data:T
            }

            intBox <- Container<int>()
            intBox.data <- 42

            doubleBox <- Container<double>()
            doubleBox.data <- 3.14

            result1 <- intBox.data
            result2 <- doubleBox.data
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(42, result1);
        Assert.Equal(3.14, result2);
    }

    [Fact]
    public void GenericFunction_MultipleCallsWithDifferentTypes_ExecutesCorrectly()
    {
        // Arrange - 泛型函数多次调用不同类型参数
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            result1 <- identity<int>(100)
            result2 <- identity<string>(""test"")
            result3 <- identity<double>(2.5)
            result4 <- identity<bool>(true)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        var result4 = vm.GetGlobalVariable("result4");
        Assert.Equal(100, result1);
        Assert.Equal("test", result2);
        Assert.Equal(2.5, result3);
        Assert.Equal(true, result4);
    }

    [Fact]
    public void GenericClass_WithArrays_ExecutesCorrectly()
    {
        // Arrange - 泛型类与数组组合
        var code = @"
            class ArrayWrapper<T> {
                public data:object
            }

            intWrapper <- ArrayWrapper<int>()
            arr1 <- [1, 2, 3, 4, 5]
            intWrapper.data <- arr1

            stringWrapper <- ArrayWrapper<string>()
            arr2 <- [""a"", ""b"", ""c""]
            stringWrapper.data <- arr2

            temp1 <- intWrapper.data
            result1 <- temp1[2]

            temp2 <- stringWrapper.data
            result2 <- temp2[1]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(3, result1);
        Assert.Equal("b", result2);
    }

    [Fact]
    public void GenericFunction_ReturnsComplexType_ExecutesCorrectly()
    {
        // Arrange - 泛型函数返回复杂类型（字典）
        var code = @"
            func createPair<T>(key:string, value:T) -> object {
                result <- {""key"": key, ""value"": value}
                return result
            }

            intPair <- createPair<int>(""age"", 25)
            stringPair <- createPair<string>(""name"", ""Alice"")

            result1 <- intPair[""value""]
            result2 <- stringPair[""value""]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(25, result1);
        Assert.Equal("Alice", result2);
    }

    [Fact]
    public void GenericClass_NestedInstantiation_ExecutesCorrectly()
    {
        // Arrange - 泛型类的嵌套实例化
        var code = @"
            class Wrapper<T> {
                public content:T
            }

            inner <- Wrapper<int>()
            inner.content <- 100

            outer <- Wrapper<object>()
            outer.content <- inner

            temp <- outer.content
            result <- temp.content
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.Equal(100, result);
    }

    [Fact]
    public void GenericClassAndFunction_ComplexInteraction_ExecutesCorrectly()
    {
        // Arrange - 泛型类和泛型函数的复杂交互
        var code = @"
            class Container<T> {
                public data:T
            }

            func wrap<T>(value:T) -> object {
                container <- Container<T>()
                container.data <- value
                return container
            }

            func unwrap<T>(container:object) -> T {
                return container.data
            }

            wrapped1 <- wrap<int>(42)
            wrapped2 <- wrap<string>(""test"")

            result1 <- unwrap<int>(wrapped1)
            result2 <- unwrap<string>(wrapped2)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(42, result1);
        Assert.Equal("test", result2);
    }
}
