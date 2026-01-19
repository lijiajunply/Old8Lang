using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Types;

/// <summary>
/// 虚拟机静态成员测试
/// 测试静态字段、静态方法和静态构造函数
/// </summary>
[Collection("Sequential")]
public class VMStaticMemberTests
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
    public void StaticField_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Counter {
                public static count:int <- 0

                public func constructor() -> void {
                    Counter.count <- Counter.count + 1
                }
            }

            obj1 <- Counter()
            obj2 <- Counter()
            obj3 <- Counter()

            result <- Counter.count
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
    public void StaticMethod_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Math {
                public static func add(a:int, b:int) -> int {
                    return a + b
                }

                public static func multiply(a:int, b:int) -> int {
                    return a * b
                }
            }

            result1 <- Math.add(10, 20)
            result2 <- Math.multiply(5, 6)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(30, result1);
        Assert.Equal(30, result2);
    }

    [Fact]
    public void StaticField_SharedAcrossInstances_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Config {
                public static appName:string <- ""MyApp""
                public static version:string <- ""1.0""

                public func getInfo() -> string {
                    return Config.appName + "" v"" + Config.version
                }
            }

            obj1 <- Config()
            result1 <- obj1.getInfo()

            Config.version <- ""2.0""

            obj2 <- Config()
            result2 <- obj2.getInfo()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal("MyApp v1.0", result1);
        Assert.Equal("MyApp v2.0", result2);
    }

    [Fact]
    public void StaticMethod_WithStaticField_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Logger {
                private static logCount:int <- 0

                public static func log(message:string) -> void {
                    Logger.logCount <- Logger.logCount + 1
                    PrintLine(""["" + Logger.logCount.ToStr() + ""] "" + message)
                }

                public static func getLogCount() -> int {
                    return Logger.logCount
                }
            }

            Logger.log(""First message"")
            Logger.log(""Second message"")
            Logger.log(""Third message"")

            result <- Logger.getLogCount()
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
    public void StaticMethod_Factory_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                private name:string
                private age:int

                private func constructor(n:string, a:int) -> void {
                    this.name <- n
                    this.age <- a
                }

                public static func create(n:string, a:int) -> Person {
                    return new Person(n, a)
                }

                public func getName() -> string {
                    return this.name
                }

                public func getAge() -> int {
                    return this.age
                }
            }

            person <- Person.create(""Alice"", 25)
            result1 <- person.getName()
            result2 <- person.getAge()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal("Alice", result1);
        Assert.Equal(25, result2);
    }

    [Fact]
    public void StaticField_Constants_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Constants {
                public static PI:double <- 3.14159
                public static E:double <- 2.71828
                public static MAX_SIZE:int <- 100
            }

            result1 <- Constants.PI * 2
            result2 <- Constants.E + 1
            result3 <- Constants.MAX_SIZE / 2
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.Equal(6.28318, Convert.ToDouble(result1), 5);
        Assert.Equal(3.71828, Convert.ToDouble(result2), 5);
        Assert.Equal(50, result3);
    }

    [Fact]
    public void StaticMethod_Utility_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class StringUtils {
                public static func reverse(str:string) -> string {
                    result <- """"
                    for i in [str.Length - 1~0] {
                        result <- result + str[i]
                    }
                    return result
                }

                public static func toUpperCase(str:string) -> string {
                    return str.ToUpper()
                }

                public static func toLowerCase(str:string) -> string {
                    return str.ToLower()
                }
            }

            result1 <- StringUtils.reverse(""hello"")
            result2 <- StringUtils.toUpperCase(""hello"")
            result3 <- StringUtils.toLowerCase(""HELLO"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.Equal("olleh", result1);
        Assert.Equal("HELLO", result2);
        Assert.Equal("hello", result3);
    }

    [Fact]
    public void StaticField_Singleton_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Singleton {
                private static instance:Singleton? <- null
                private value:int

                private func constructor() -> void {
                    this.value <- 42
                }

                public static func getInstance() -> Singleton {
                    if Singleton.instance == null {
                        Singleton.instance <- new Singleton()
                    }
                    return Singleton.instance
                }

                public func getValue() -> int {
                    return this.value
                }
            }

            obj1 <- Singleton.getInstance()
            obj2 <- Singleton.getInstance()

            result1 <- obj1.getValue()
            result2 <- obj2.getValue()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(42, result1);
        Assert.Equal(42, result2);
    }

    [Fact]
    public void StaticMethod_ChainedCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public static func add(a:int, b:int) -> int {
                    return a + b
                }

                public static func multiply(a:int, b:int) -> int {
                    return a * b
                }

                public static func square(x:int) -> int {
                    return Calculator.multiply(x, x)
                }
            }

            result <- Calculator.square(Calculator.add(3, 2))
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(25, result); // (3 + 2)^2 = 25
    }

    [Fact]
    public void StaticField_WithList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Registry {
                public static items:List<string> <- {}

                public static func register(item:string) -> void {
                    Registry.items.Add(item)
                }

                public static func getCount() -> int {
                    return Registry.items.Count()
                }
            }

            Registry.register(""Item1"")
            Registry.register(""Item2"")
            Registry.register(""Item3"")

            result <- Registry.getCount()
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
}
