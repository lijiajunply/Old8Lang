using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Types;

/// <summary>
/// 虚拟机联合类型测试
/// 测试联合类型的声明、赋值和兼容性规则
/// </summary>
[Collection("Sequential")]
public class VMUnionTypeTests
{
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var vm = new VM(bytecodeFile);
            vm.Execute();
            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void UnionType_IntOrString_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func process(value:int | string) -> string {
                return ""Value: "" + value.ToStr()
            }

            result1 <- process(42)
            result2 <- process(""hello"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal("Value: 42", result1);
        Assert.Equal("Value: hello", result2);
    }

    [Fact]
    public void UnionType_MultipleTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func describe(value:int | string | bool) -> string {
                if value is int {
                    return ""Number: "" + value.ToStr()
                } elif value is string {
                    return ""Text: "" + value
                } elif value is bool {
                    return ""Boolean: "" + value.ToStr()
                } else {
                    return ""Unknown""
                }
            }

            result1 <- describe(42)
            result2 <- describe(""test"")
            result3 <- describe(true)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.Equal("Number: 42", result1);
        Assert.Equal("Text: test", result2);
        Assert.Equal("Boolean: True", result3);
    }

    [Fact]
    public void UnionType_WithNull_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func processOptional(value:int | null) -> string {
                if value == null {
                    return ""null""
                } else {
                    return ""Value: "" + value.ToStr()
                }
            }

            result1 <- processOptional(42)
            result2 <- processOptional(null)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal("Value: 42", result1);
        Assert.Equal("null", result2);
    }

    [Fact]
    public void UnionType_InList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {1, ""two"", 3, ""four"", 5}
            results <- {}

            for item in list {
                if item is int {
                    results.Add(""int: "" + item.ToStr())
                } else {
                    results.Add(""string: "" + item)
                }
            }

            result <- results.Count()
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
    public void UnionType_ReturnType_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getValue(flag:bool) -> int | string {
                if flag {
                    return 42
                } else {
                    return ""hello""
                }
            }

            result1 <- getValue(true)
            result2 <- getValue(false)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(42, result1);
        Assert.Equal("hello", result2);
    }

    [Fact]
    public void UnionType_WithArray_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func processArray(arr:array<int | string>) -> int {
                count <- 0
                for item in arr {
                    count <- count + 1
                }
                return count
            }

            arr <- [1, ""two"", 3, ""four""]
            result <- processArray(arr)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(4, result);
    }

    [Fact]
    public void UnionType_TypeGuard_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func processValue(value:int | string) -> string {
                if value is int {
                    doubled <- value * 2
                    return ""Doubled: "" + doubled.ToStr()
                } else {
                    return ""Uppercase: "" + value.ToUpper()
                }
            }

            result1 <- processValue(21)
            result2 <- processValue(""hello"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal("Doubled: 42", result1);
        Assert.Equal("Uppercase: HELLO", result2);
    }

    [Fact]
    public void UnionType_NestedUnion_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func process(value:(int | string) | bool) -> string {
                return ""Type: "" + value.GetType().Name
            }

            result1 <- process(42)
            result2 <- process(""test"")
            result3 <- process(true)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.Contains("Int", result1.ToString());
        Assert.Contains("String", result2.ToString());
        Assert.Contains("Bool", result3.ToString());
    }

    [Fact]
    public void UnionType_WithDictionary_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""age"": 25, ""name"": ""Alice"", ""active"": true}
            results <- {}

            for key in dict.Keys {
                value <- dict[key]
                results.Add(key + "": "" + value.ToStr())
            }

            result <- results.Count()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(3, result);
    }

    [Fact]
    public void UnionType_InFunction_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int | double, b:int | double) -> double {
                return a + b
            }

            result1 <- add(10, 20)
            result2 <- add(10.5, 20)
            result3 <- add(10, 20.5)
            result4 <- add(10.5, 20.5)
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
        Assert.Equal(30.0, Convert.ToDouble(result1));
        Assert.Equal(30.5, Convert.ToDouble(result2));
        Assert.Equal(30.5, Convert.ToDouble(result3));
        Assert.Equal(31.0, Convert.ToDouble(result4));
    }
}
