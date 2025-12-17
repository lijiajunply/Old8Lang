using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 函数重载测试
/// </summary>
public class FunctionOverloadTests
{
    [Fact]
    public void FunctionOverload_DifferentParameterCounts_OverloadsByParameterCount()
    {
        // Arrange
        var code = @"
            func calculate() -> int {
                return 0
            }
            func calculate(x:int) -> int {
                return x * 2
            }
            func calculate(x:int, y:int) -> int {
                return x + y
            }
            func calculate(x:int, y:int, z:int) -> int {
                return x * y * z
            }
            result1 <- calculate()
            result2 <- calculate(5)
            result3 <- calculate(3, 7)
            result4 <- calculate(2, 3, 4)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(0, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(10, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(10, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(24, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void FunctionOverload_DifferentParameterTypes_OverloadsByParameterType()
    {
        // Arrange
        var code = @"
            func process(value:int) -> string {
                return ""Integer: "" + value.ToStr()
            }
            func process(value:string) -> string {
                return ""String: "" + value
            }
            func process(value:double) -> string {
                return ""Double: "" + value.ToStr()
            }
            func process(value:bool) -> string {
                return ""Boolean: "" + value.ToStr()
            }
            result1 <- process(42)
            result2 <- process(""hello"")
            result3 <- process(3.14)
            result4 <- process(true)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Integer: 42", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("String: hello", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Double: 3.14", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("Boolean: true", ((StringLangValue)result4).Value);
    }

    [Fact]
    public void FunctionOverload_DifferentReturnTypes_OverloadsByReturnType()
    {
        // Arrange
        var code = @"
            func getValue() -> int {
                return 100
            }
            func getValue() -> string {
                return ""text""
            }
            func getValue() -> double {
                return 2.5
            }
            func getValue() -> bool {
                return false
            }
            intResult <- getValue() as int
            stringResult <- getValue() as string
            doubleResult <- getValue() as double
            boolResult <- getValue() as bool
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intResult = interpreter.Manager.GetValue(new LangId("intResult"));
        var stringResult = interpreter.Manager.GetValue(new LangId("stringResult"));
        var doubleResult = interpreter.Manager.GetValue(new LangId("doubleResult"));
        var boolResult = interpreter.Manager.GetValue(new LangId("boolResult"));

        Assert.NotNull(intResult);
        Assert.IsType<IntLangValue>(intResult);
        Assert.Equal(100, ((IntLangValue)intResult).Value);

        Assert.NotNull(stringResult);
        Assert.IsType<StringLangValue>(stringResult);
        Assert.Equal("text", ((StringLangValue)stringResult).Value);

        Assert.NotNull(doubleResult);
        Assert.IsType<DoubleLangValue>(doubleResult);
        Assert.Equal(2.5, ((DoubleLangValue)doubleResult).Value);

        Assert.NotNull(boolResult);
        Assert.IsType<BoolLangValue>(boolResult);
        Assert.Equal(false, ((BoolLangValue)boolResult).Value);
    }

    [Fact]
    public void FunctionOverload_MixedParameters_OverloadsByMixedParameters()
    {
        // Arrange
        var code = @"
            func display(x:int, y:string) -> string {
                return x.ToStr() + "" "" + y
            }
            func display(x:string, y:int) -> string {
                return x + "" "" + y.ToStr()
            }
            func display(x:int, y:int) -> string {
                return ""Numbers: "" + x.ToStr() + "", "" + y.ToStr()
            }
            func display(x:string, y:string) -> string {
                return ""Strings: "" + x + "", "" + y
            }
            result1 <- display(10, ""hello"")
            result2 <- display(""world"", 20)
            result3 <- display(5, 7)
            result4 <- display(""foo"", ""bar"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("10 hello", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("world 20", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Numbers: 5, 7", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("Strings: foo, bar", ((StringLangValue)result4).Value);
    }

    [Fact]
    public void FunctionOverload_WithArrays_OverloadsArrayParameters()
    {
        // Arrange
        var code = @"
            func sum(numbers:[int]) -> int {
                total <- 0
                for num in numbers {
                    total <- total + num
                }
                return total
            }
            func sum(numbers:[double]) -> double {
                total <- 0.0
                for num in numbers {
                    total <- total + num
                }
                return total
            }
            func sum(numbers:[string]) -> string {
                result <- """"
                for str in numbers {
                    result <- result + str
                }
                return result
            }
            intArray <- [1, 2, 3, 4, 5]
            doubleArray <- [1.1, 2.2, 3.3]
            stringArray <- [""A"", ""B"", ""C""]
            result1 <- sum(intArray)
            result2 <- sum(doubleArray)
            result3 <- sum(stringArray)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(15, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(6.6, ((DoubleLangValue)result2).Value, 0.1);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("ABC", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void FunctionOverload_WithLists_OverloadsListParameters()
    {
        // Arrange
        var code = @"
            func first(list:{int}) -> int {
                return list[0]
            }
            func first(list:{string}) -> string {
                return list[0]
            }
            func first(list:{double}) -> double {
                return list[0]
            }
            intList <- {10, 20, 30}
            stringList <- {""x"", ""y"", ""z""}
            doubleList <- {1.5, 2.5, 3.5}
            result1 <- first(intList)
            result2 <- first(stringList)
            result3 <- first(doubleList)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(10, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("x", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<DoubleLangValue>(result3);
        Assert.Equal(1.5, ((DoubleLangValue)result3).Value);
    }

    [Fact]
    public void FunctionOverload_WithDefaultParameters_OverloadsWithDefaults()
    {
        // Arrange
        var code = @"
            func greet(name:string, greeting:string) -> string {
                return greeting + "", "" + name
            }
            func greet(name:string, greeting:string, title:string) -> string {
                return greeting + "", "" + title + "" "" + name
            }
            func greet(name:string) -> string {
                return ""Hello, "" + name
            }
            result1 <- greet(""Alice"", ""Hi"")
            result2 <- greet(""Bob"", ""Hello"", ""Dr."")
            result3 <- greet(""Charlie"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Hi, Alice", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Hello, Dr. Bob", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Hello, Charlie", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void FunctionOverload_WithComplexTypes_OverloadsClassParameters()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string
                public age:int
                func Init(n:string, a:int) {
                    name <- n
                    age <- a
                }
            }
            class Animal {
                public species:string
                public sound:string
                func Init(s:string, snd:string) {
                    species <- s
                    sound <- snd
                }
            }
            func describe(obj:Person) -> string {
                return ""Person: "" + obj.name + "", age "" + obj.age.ToStr()
            }
            func describe(obj:Animal) -> string {
                return ""Animal: "" + obj.species + "", says "" + obj.sound
            }
            person <- Person(""Alice"", 30)
            animal <- Animal(""Dog"", ""Woof"")
            result1 <- describe(person)
            result2 <- describe(animal)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Person: Alice, age 30", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Animal: Dog, says Woof", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void FunctionOverload_WithOptionalParameters_OverloadsOptionalParams()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int, operation:string) -> int {
                if operation == ""add"" {
                    return x + y
                } else if operation == ""multiply"" {
                    return x * y
                } else {
                    return x - y
                }
            }
            func calculate(x:int, y:int) -> int {
                return x + y
            }
            func calculate(x:int) -> int {
                return x * 2
            }
            result1 <- calculate(10, 5)
            result2 <- calculate(10, 5, ""multiply"")
            result3 <- calculate(7)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(15, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(50, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(14, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void FunctionOverload_WithVariadicParameters_OverloadsVariableParams()
    {
        // Arrange
        var code = @"
            func sum() -> int {
                return 0
            }
            func sum(a:int) -> int {
                return a
            }
            func sum(a:int, b:int) -> int {
                return a + b
            }
            func sum(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            func sum(numbers:{int}) -> int {
                total <- 0
                for num in numbers {
                    total <- total + num
                }
                return total
            }
            result1 <- sum()
            result2 <- sum(10)
            result3 <- sum(5, 15)
            result4 <- sum(3, 7, 10)
            result5 <- sum({1, 2, 3, 4, 5})
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));
        var result5 = interpreter.Manager.GetValue(new LangId("result5"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(0, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(10, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(20, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(20, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(15, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void FunctionOverload_WithGenerics_OverloadsGenericTypes()
    {
        // Arrange
        var code = @"
            func compare(a:int, b:int) -> bool {
                return a > b
            }
            func compare(a:string, b:string) -> bool {
                return a.Length > b.Length
            }
            func compare(a:double, b:double) -> bool {
                return a > b
            }
            result1 <- compare(10, 5)
            result2 <- compare(""hello"", ""hi"")
            result3 <- compare(3.14, 2.71)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.Equal(true, ((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(true, ((BoolLangValue)result3).Value);
    }

    [Fact]
    public void FunctionOverload_WithBooleanParameters_OverloadsByBooleanTypes()
    {
        // Arrange
        var code = @"
            func configure(setting:string, value:bool) -> string {
                return setting + "" is "" + value.ToStr()
            }
            func configure(setting:string, value:int) -> string {
                return setting + "" = "" + value.ToStr()
            }
            func configure(setting:string, value:string) -> string {
                return setting + "" = '"" + value + ""'""
            }
            result1 <- configure(""debug"", true)
            result2 <- configure(""timeout"", 30)
            result3 <- configure(""mode"", ""production"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("debug is true", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("timeout = 30", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("mode = 'production'", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void FunctionOverload_WithCharacterParameters_OverloadsByCharTypes()
    {
        // Arrange
        var code = @"
            func process(ch:char) -> int {
                return ch.ToInt32()
            }
            func process(s:string) -> int {
                return s.Length
            }
            func process(i:int) -> int {
                return i * 2
            }
            result1 <- process('A')
            result2 <- process(""hello"")
            result3 <- process(21)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(65, ((IntLangValue)result1).Value); // ASCII value of 'A'

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(42, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void FunctionOverload_WithNullParameters_OverloadsNullHandling()
    {
        // Arrange
        var code = @"
            func safeGet(value:int, defaultValue:int) -> int {
                if value == null {
                    return defaultValue
                }
                return value
            }
            func safeGet(value:string, defaultValue:string) -> string {
                if value == null {
                    return defaultValue
                }
                return value
            }
            func safeGet(value:bool, defaultValue:bool) -> bool {
                if value == null {
                    return defaultValue
                }
                return value
            }
            result1 <- safeGet(null, 10)
            result2 <- safeGet(null, ""default"")
            result3 <- safeGet(null, true)
            result4 <- safeGet(5, 0)
            result5 <- safeGet(""hello"", ""world"")
            result6 <- safeGet(false, true)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));
        var result5 = interpreter.Manager.GetValue(new LangId("result5"));
        var result6 = interpreter.Manager.GetValue(new LangId("result6"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(10, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("default", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(true, ((BoolLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(5, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<StringLangValue>(result5);
        Assert.Equal("hello", ((StringLangValue)result5).Value);

        Assert.NotNull(result6);
        Assert.IsType<BoolLangValue>(result6);
        Assert.Equal(false, ((BoolLangValue)result6).Value);
    }

    [Fact]
    public void FunctionOverload_WithNestedOverloads_RecursiveOverloadCalls()
    {
        // Arrange
        var code = @"
            func calculateDepth() -> int {
                return 1
            }
            func calculateDepth(x:int) -> int {
                return 1 + calculateDepth()
            }
            func calculateDepth(x:int, y:int) -> int {
                return 1 + calculateDepth(x)
            }
            func calculateDepth(x:int, y:int, z:int) -> int {
                return 1 + calculateDepth(x, y)
            }
            result1 <- calculateDepth()
            result2 <- calculateDepth(10)
            result3 <- calculateDepth(10, 20)
            result4 <- calculateDepth(10, 20, 30)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(1, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(4, ((IntLangValue)result4).Value);
    }
}