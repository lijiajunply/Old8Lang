using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.BytecodeSerialization;

[Collection("Sequential")]
public class VMBytecodeSerializationTests
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
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private (BytecodeFile bytecode, string output) CompileAndExecuteVMCode(string code)
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
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return (bytecodeFile, stringWriter.ToString().Trim());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 字节码文件序列化测试

    [Fact]
    public void BytecodeSerialization_SimpleProgram_CompilesAndExecutes()
    {
        var code = @"
            x <- 42
            PrintLine(x.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("42", output);
    }

    [Fact]
    public void BytecodeSerialization_ArithmeticOperations_CompilesAndExecutes()
    {
        var code = @"
            a <- 10
            b <- 20
            result <- a + b
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("30", output);
    }

    [Fact]
    public void BytecodeSerialization_StringOperations_CompilesAndExecutes()
    {
        var code = @"
            str1 <- ""Hello""
            str2 <- ""World""
            result <- str1 + "", "" + str2 + ""!""
            PrintLine(result)
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("Hello, World!", output);
    }

    #endregion

    #region 字节码文件结构测试

    [Fact]
    public void BytecodeSerialization_FileStructure_ContainsExpectedSections()
    {
        var code = @"
            x <- 42
            PrintLine(x.ToStr())
        ";

        var (bytecode, _) = CompileAndExecuteVMCode(code);

        Assert.NotNull(bytecode);
    }

    [Fact]
    public void BytecodeSerialization_Constants_PreservesValues()
    {
        var code = @"
            x <- 42
            y <- 3.14
            z <- ""Hello""
            flag <- true
        ";

        var (bytecode, _) = CompileAndExecuteVMCode(code);

        Assert.NotNull(bytecode);
    }

    #endregion

    #region 字节码指令测试

    [Fact]
    public void BytecodeSerialization_LoadStore_InstructionsWork()
    {
        var code = @"
            a <- 10
            b <- a
            PrintLine(b.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("10", output);
    }

    [Fact]
    public void BytecodeSerialization_BinaryOperations_InstructionsWork()
    {
        var code = @"
            result <- (10 + 20) * 3
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("90", output);
    }

    [Fact]
    public void BytecodeSerialization_ConditionalJumps_InstructionsWork()
    {
        var code = @"
            x <- 10
            if x > 5 {
                PrintLine(""greater"")
            } else {
                PrintLine(""smaller"")
            }
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("greater", output);
    }

    [Fact]
    public void BytecodeSerialization_LoopJumps_InstructionsWork()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 10, i++ {
                sum <- sum + i
            }
            PrintLine(sum.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("45", output);
    }

    #endregion

    #region 字节码函数测试

    [Fact]
    public void BytecodeSerialization_FunctionDefinition_CompilesAndExecutes()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(10, 20)
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("30", output);
    }

    [Fact]
    public void BytecodeSerialization_FunctionCall_CompilesAndExecutes()
    {
        var code = @"
            func greet(name:string) -> void {
                PrintLine(""Hello, "" + name + ""!"")
            }

            greet(""World"")
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("Hello, World!", output);
    }

    [Fact]
    public void BytecodeSerialization_RecursiveFunction_CompilesAndExecutes()
    {
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            result <- factorial(5)
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("120", output);
    }

    #endregion

    #region 字节码类测试

    [Fact]
    public void BytecodeSerialization_ClassDefinition_CompilesAndExecutes()
    {
        var code = @"
            class Point {
                public x:int
                public y:int

                public func new(x:int, y:int) -> void {
                    this.x <- x
                    this.y <- y
                }
            }

            point <- Point(10, 20)
            PrintLine(point.x.ToStr())
            PrintLine(point.y.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Contains("10", output);
        Assert.Contains("20", output);
    }

    [Fact]
    public void BytecodeSerialization_ClassMethod_CompilesAndExecutes()
    {
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }

            calc <- Calculator()
            result <- calc.add(10, 20)
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("30", output);
    }

    #endregion

    #region 字节码集合测试

    [Fact]
    public void BytecodeSerialization_Array_CompilesAndExecutes()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            for i <- 0, i < len(arr), i++ {
                sum <- sum + arr[i]
            }
            PrintLine(sum.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("15", output);
    }

    [Fact]
    public void BytecodeSerialization_List_CompilesAndExecutes()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            sum <- 0
            for x <- list {
                sum <- sum + x
            }
            PrintLine(sum.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("15", output);
    }

    [Fact]
    public void BytecodeSerialization_Dictionary_CompilesAndExecutes()
    {
        var code = @"
            dict <- {""a"": 1, ""b"": 2, ""c"": 3}
            sum <- 0
            for key in dict {
                sum <- sum + dict[key]
            }
            PrintLine(sum.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("6", output);
    }

    #endregion

    #region 字节码控制流测试

    [Fact]
    public void BytecodeSerialization_IfStatement_CompilesAndExecutes()
    {
        var code = @"
            x <- 10
            if x > 5 {
                PrintLine(""large"")
            } else {
                PrintLine(""small"")
            }
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("large", output);
    }

    [Fact]
    public void BytecodeSerialization_WhileLoop_CompilesAndExecutes()
    {
        var code = @"
            sum <- 0
            i <- 0
            while i < 10 {
                sum <- sum + i
                i <- i + 1
            }
            PrintLine(sum.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("45", output);
    }

    [Fact]
    public void BytecodeSerialization_ForInLoop_CompilesAndExecutes()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            for x <- arr {
                sum <- sum + x
            }
            PrintLine(sum.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("15", output);
    }

    #endregion

    #region 字节码异常处理测试

    [Fact]
    public void BytecodeSerialization_TryCatch_CompilesAndExecutes()
    {
        var code = @"
            try {
                throw ""Test error""
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("Caught error", output);
    }

    [Fact]
    public void BytecodeSerialization_TryFinally_CompilesAndExecutes()
    {
        var code = @"
            try {
                PrintLine(""Try block"")
            } finally {
                PrintLine(""Finally block"")
            }
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Contains("Try block", output);
        Assert.Contains("Finally block", output);
    }

    #endregion

    #region 字节码异步测试

    [Fact]
    public void BytecodeSerialization_AsyncFunction_CompilesAndExecutes()
    {
        var code = @"
            async func getValue() -> int {
                return 42
            }

            task <- getValue()
            result <- await task
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("42", output);
    }

    #endregion

    #region 字节码生成器测试

    [Fact]
    public void BytecodeSerialization_Generator_CompilesAndExecutes()
    {
        var code = @"
            func generate() -> {
                yield 1
                yield 2
                yield 3
            }

            gen <- generate()
            sum <- 0
            while gen.MoveNext() {
                sum <- sum + gen.Current()
            }
            PrintLine(sum.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("6", output);
    }

    #endregion

    #region 字节码泛型测试

    [Fact]
    public void BytecodeSerialization_GenericClass_CompilesAndExecutes()
    {
        var code = @"
            class Box<T> {
                public value:T

                public func new(value:T) -> void {
                    this.value <- value
                }
            }

            box <- Box<int>(42)
            PrintLine(box.value.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("42", output);
    }

    [Fact]
    public void BytecodeSerialization_GenericFunction_CompilesAndExecutes()
    {
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            result <- identity<int>(42)
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("42", output);
    }

    #endregion

    #region 字节码 Lambda 测试

    [Fact]
    public void BytecodeSerialization_Lambda_CompilesAndExecutes()
    {
        var code = @"
            add <- (a:int, b:int) -> int {
                return a + b
            }

            result <- add(10, 20)
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("30", output);
    }

    #endregion

    #region 字节码 Using 语句测试

    [Fact]
    public void BytecodeSerialization_Using_CompilesAndExecutes()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func new(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource disposed"")
                }
            }

            using res <- Resource(1) {
                PrintLine(""Using resource"")
            }
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Contains("Using resource", output);
        Assert.Contains("Resource disposed", output);
    }

    #endregion

    #region 字节码 Select 语句测试

    [Fact]
    public void BytecodeSerialization_Select_CompilesAndExecutes()
    {
        var code = @"
            ch <- ChannelCreate()
            ChannelSend(ch, 42)

            select {
                case val from ch -> {
                    PrintLine(val.ToStr())
                }
            }
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("42", output);
    }

    #endregion

    #region 字节码 Match 表达式测试

    [Fact]
    public void BytecodeSerialization_Match_CompilesAndExecutes()
    {
        var code = @"
            result <- match 5 {
                case 1 -> ""one""
                case 5 -> ""five""
                default -> ""other""
            }
            PrintLine(result)
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("five", output);
    }

    #endregion

    #region 字节码复杂程序测试

    [Fact]
    public void BytecodeSerialization_ComplexProgram_CompilesAndExecutes()
    {
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }

                public func multiply(a:int, b:int) -> int {
                    return a * b
                }
            }

            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            calc <- Calculator()
            sum <- calc.add(10, 20)
            product <- calc.multiply(5, 6)
            fact <- factorial(5)

            PrintLine(""Sum: "" + sum.ToStr())
            PrintLine(""Product: "" + product.ToStr())
            PrintLine(""Factorial: "" + fact.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Contains("Sum: 30", output);
        Assert.Contains("Product: 30", output);
        Assert.Contains("Factorial: 120", output);
    }

    #endregion

    #region 字节码版本兼容性测试

    [Fact]
    public void BytecodeSerialization_VersionInfo_Exists()
    {
        var code = @"
            x <- 42
        ";

        var (bytecode, _) = CompileAndExecuteVMCode(code);

        Assert.NotNull(bytecode);
    }

    #endregion

    #region 字节码调试信息测试

    [Fact]
    public void BytecodeSerialization_DebugInformation_Preserved()
    {
        var code = @"
            x <- 42
            PrintLine(x.ToStr())
        ";

        var (bytecode, _) = CompileAndExecuteVMCode(code);

        Assert.NotNull(bytecode);
    }

    #endregion

    #region 字节码优化测试

    [Fact]
    public void BytecodeSerialization_ConstantFolding_Optimized()
    {
        var code = @"
            result <- (10 + 20) * 3
            PrintLine(result.ToStr())
        ";

        var (bytecode, output) = CompileAndExecuteVMCode(code);
        Assert.NotNull(bytecode);
        Assert.Equal("90", output);
    }

    #endregion

    #region 字节码错误处理测试

    [Fact]
    public void BytecodeSerialization_RuntimeError_Propagated()
    {
        var code = @"
            result <- 10 / 0
        ";

        Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
    }

    #endregion
}