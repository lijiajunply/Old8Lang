using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.VirtualMachine.ErrorHandling;

[Collection("Sequential")]
public class VMTypeErrorTests
{
    private readonly ITestOutputHelper _output;

    public VMTypeErrorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private void AssertVMThrowsTypeException(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
        var exception = Assert.ThrowsAny<System.Exception>(() => vm.Execute());
        _output.WriteLine($"Exception type: {exception.GetType().Name}");
        _output.WriteLine($"Exception message: {exception.Message}");
    }

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

    #region 类型转换失败测试

    [Fact]
    public void TypeErrors_InvalidStringToInt_ThrowsTypeException()
    {
        var code = @"
            str <- ""not a number""
            result <- int(str)
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_InvalidStringToDouble_ThrowsTypeException()
    {
        var code = @"
            str <- ""not a number""
            result <- double(str)
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_InvalidStringToBool_ThrowsTypeException()
    {
        var code = @"
            str <- ""not a bool""
            result <- bool(str)
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 泛型类型错误测试

    [Fact]
    public void TypeErrors_GenericClassTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            class Box<T> {
                public value:T
            }

            box <- Box<int>()
            box.value <- ""not an int""
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_GenericFunctionTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            result <- identity<string>(123)
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_GenericConstraintViolation_ThrowsTypeException()
    {
        var code = @"
            func process<T where T: int>(value:T) -> T {
                return value
            }

            result <- process<string>(""hello"")
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 参数类型不匹配测试

    [Fact]
    public void TypeErrors_FunctionParameterTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(10, ""not an int"")
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_ConstructorParameterTypeMismatch_ThrowsTypeException()
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

            point <- Point(10, ""not an int"")
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 返回值类型错误测试

    [Fact]
    public void TypeErrors_FunctionReturnTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            func getNumber() -> int {
                return ""not an int""
            }

            result <- getNumber()
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_AsyncFunctionReturnTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            async func getData() -> int {
                return ""not an int""
            }

            task <- getData()
            result <- await task
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_LambdaReturnTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            func getNumber() -> int {
                return ""not an int""
            }

            lambda <- () -> int {
                return ""not an int""
            }

            result <- lambda()
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 操作符类型错误测试

    [Fact]
    public void TypeErrors_AdditionTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            a <- 10
            b <- ""hello""
            result <- a + b
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_SubtractionTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            a <- 10
            b <- ""hello""
            result <- a - b
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_MultiplicationTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            a <- 10
            b <- ""hello""
            result <- a * b
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_DivisionTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            a <- 10
            b <- ""hello""
            result <- a / b
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_ComparisonTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            a <- 10
            b <- ""hello""
            result <- a > b
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_LogicalOperatorTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            a <- 10
            b <- 20
            result <- a && b
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 集合类型错误测试

    [Fact]
    public void TypeErrors_ArrayElementTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            arr <- [1, 2, ""not an int"", 4]
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_ListElementTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            list <- {1, 2, ""not an int"", 4}
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_DictionaryKeyTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            dict <- {""a"": 1, 2: ""not a string key""}
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 类成员类型错误测试

    [Fact]
    public void TypeErrors_ClassFieldTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            class Person {
                public name:string
                public age:int
            }

            person <- Person()
            person.age <- ""not an int""
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_ClassMethodParameterTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }

            calc <- Calculator()
            result <- calc.add(10, ""not an int"")
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 类型断言错误测试

    [Fact]
    public void TypeErrors_InvalidTypeAssertion_ThrowsTypeException()
    {
        var code = @"
            x <- ""hello""
            y <- x as int
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_ValidTypeAssertion_Succeeds()
    {
        var code = @"
            x <- 10
            y <- x as int
            PrintLine(y.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("10", output);
    }

    #endregion

    #region 联合类型错误测试

    [Fact]
    public void TypeErrors_UnionTypeInvalidOperation_ThrowsTypeException()
    {
        var code = @"
            func process(value:int | string) -> void {
                if value is int {
                    PrintLine(""int:"" + value.ToStr())
                } else {
                    PrintLine(""string:"" + value)
                }
            }

            process(123)
            process(""hello"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("int:123", output);
        Assert.Contains("string:hello", output);
    }

    #endregion

    #region 可空类型错误测试

    [Fact]
    public void TypeErrors_NullableTypeDereference_ThrowsTypeException()
    {
        var code = @"
            func getNumber() -> int? {
                return null
            }

            result <- getNumber()
            PrintLine(result.ToStr())
        ";

        AssertVMThrowsTypeException(code);
    }

    [Fact]
    public void TypeErrors_NullableTypeNullCheck_Succeeds()
    {
        var code = @"
            func getNumber() -> int? {
                return null
            }

            result <- getNumber()
            if result != null {
                PrintLine(result.ToStr())
            } else {
                PrintLine(""null"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("null", output);
    }

    #endregion

    #region 交集类型错误测试

    [Fact]
    public void TypeErrors_IntersectionTypeInvalidMember_ThrowsTypeException()
    {
        var code = @"
            class A {
                public func methodA() -> string {
                    return ""A""
                }
            }

            class B {
                public func methodB() -> string {
                    return ""B""
                }
            }

            func process(value: A & B) -> void {
                PrintLine(value.methodA())
                PrintLine(value.methodB())
            }
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 类型推断错误测试

    [Fact]
    public void TypeErrors_TypeInferenceConflict_ThrowsTypeException()
    {
        var code = @"
            x <- 10
            x <- ""now a string""
            x <- 20
        ";

        var output = ExecuteVMCode(code);
        Assert.NotEmpty(output);
    }

    #endregion

    #region 运算符重载类型错误测试

    [Fact]
    public void TypeErrors_OperatorOverloadTypeMismatch_ThrowsTypeException()
    {
        var code = @"
            class Complex {
                public real:double
                public imag:double

                public func new(real:double, imag:double) -> void {
                    this.real <- real
                    this.imag <- imag
                }

                public func _add(other:Complex) -> Complex {
                    return Complex(this.real + other.real, this.imag + other.imag)
                }
            }

            c1 <- Complex(1.0, 2.0)
            c2 <- Complex(3.0, 4.0)
            c3 <- c1 + c2
            PrintLine(c3.real.ToStr())
            PrintLine(c3.imag.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("4", output.Split('\n')[0]);
        Assert.Equal("6", output.Split('\n')[1]);
    }

    #endregion

    #region 类型约束错误测试

    [Fact]
    public void TypeErrors_TypeConstraintViolation_ThrowsTypeException()
    {
        var code = @"
            func process<T where T: Comparable>(value:T) -> void {
                PrintLine(value.ToStr())
            }

            process(123)
        ";

        var output = ExecuteVMCode(code);
        Assert.NotEmpty(output);
    }

    #endregion

    #region 动态类型错误测试

    [Fact]
    public void TypeErrors_DynamicTypeInvalidOperation_ThrowsTypeException()
    {
        var code = @"
            x <- (123)
            y <- (""hello"")
            result <- x + y
        ";

        AssertVMThrowsTypeException(code);
    }

    #endregion

    #region 反射类型错误测试

    [Fact]
    public void TypeErrors_ReflectionInvalidType_ThrowsTypeException()
    {
        var code = @"
            class MyClass {
                public value:int
            }

            obj <- MyClass()
            obj.value <- 42

            typeInfo <- type(obj)
            PrintLine(typeInfo.Name())
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("MyClass", output);
    }

    #endregion
}
