using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 泛型函数测试
/// 测试泛型函数的基本功能，包括类型推断、函数调用等
/// </summary>
public class GenericFunctionTests
{
    [Fact]
    public void GenericFunction_SingleTypeParameter_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            intResult <- identity<int>(42)
            stringResult <- identity<string>(""hello"")
            doubleResult <- identity<double>(3.14)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intResult = interpreter.Manager.GetValue(new LangId("intResult"));
        var stringResult = interpreter.Manager.GetValue(new LangId("stringResult"));
        var doubleResult = interpreter.Manager.GetValue(new LangId("doubleResult"));

        Assert.IsType<IntLangValue>(intResult);
        Assert.Equal(42, ((IntLangValue)intResult!).Value);

        Assert.IsType<StringLangValue>(stringResult);
        Assert.Equal("hello", ((StringLangValue)stringResult!).Value);

        Assert.IsType<DoubleLangValue>(doubleResult);
        Assert.Equal(3.14, ((DoubleLangValue)doubleResult!).Value);
    }

    [Fact]
    public void GenericFunction_MultipleTypeParameters_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func makePair<K, V>(key:K, value:V) -> string {
                return key.ToStr() + "":"" + value.ToStr()
            }

            result1 <- makePair<string, int>(""age"", 25)
            result2 <- makePair<int, string>(1, ""first"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("age:25", ((StringLangValue)result1!).Value);

        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("1:first", ((StringLangValue)result2!).Value);
    }

    [Fact]
    public void GenericFunction_WithMultipleParameters_AllSameType()
    {
        // Arrange
        var code = @"
            func add<T>(a:T, b:T) -> string {
                return a.ToStr() + ""+"" + b.ToStr()
            }

            intResult <- add<int>(10, 20)
            stringResult <- add<string>(""hello"", ""world"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intResult = interpreter.Manager.GetValue(new LangId("intResult"));
        var stringResult = interpreter.Manager.GetValue(new LangId("stringResult"));

        Assert.IsType<StringLangValue>(intResult);
        Assert.Equal("10+20", ((StringLangValue)intResult!).Value);

        Assert.IsType<StringLangValue>(stringResult);
        Assert.Equal("hello+world", ((StringLangValue)stringResult!).Value);
    }

    [Fact]
    public void GenericFunction_ReturnsTypeParameter_PreservesType()
    {
        // Arrange
        var code = @"
            func getFirst<T>(a:T, b:T) -> T {
                return a
            }

            intResult <- getFirst<int>(100, 200)
            stringResult <- getFirst<string>(""first"", ""second"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intResult = interpreter.Manager.GetValue(new LangId("intResult"));
        var stringResult = interpreter.Manager.GetValue(new LangId("stringResult"));

        Assert.IsType<IntLangValue>(intResult);
        Assert.Equal(100, ((IntLangValue)intResult!).Value);

        Assert.IsType<StringLangValue>(stringResult);
        Assert.Equal("first", ((StringLangValue)stringResult!).Value);
    }

    [Fact]
    public void GenericFunction_CalledMultipleTimes_EachCallIndependent()
    {
        // Arrange
        var code = @"
            func wrap<T>(value:T) -> string {
                return ""["" + value.ToStr() + ""]""
            }

            call1 <- wrap<int>(42)
            call2 <- wrap<string>(""hello"")
            call3 <- wrap<double>(3.14)
            call4 <- wrap<int>(100)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var call1 = interpreter.Manager.GetValue(new LangId("call1"));
        var call2 = interpreter.Manager.GetValue(new LangId("call2"));
        var call3 = interpreter.Manager.GetValue(new LangId("call3"));
        var call4 = interpreter.Manager.GetValue(new LangId("call4"));

        Assert.Equal("[42]", ((StringLangValue)call1!).Value);
        Assert.Equal("[hello]", ((StringLangValue)call2!).Value);
        Assert.Equal("[3.14]", ((StringLangValue)call3!).Value);
        Assert.Equal("[100]", ((StringLangValue)call4!).Value);
    }

    [Fact]
    public void GenericFunction_WithConditionalLogic_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func compareToZero<T>(value:T) -> string {
                if value.ToStr() == ""0"" {
                    return ""zero""
                } else {
                    return ""non-zero""
                }
            }

            result1 <- compareToZero<int>(0)
            result2 <- compareToZero<int>(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.Equal("zero", ((StringLangValue)result1!).Value);
        Assert.Equal("non-zero", ((StringLangValue)result2!).Value);
    }

    [Fact]
    public void GenericFunction_ThreeTypeParameters_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func combine<A, B, C>(a:A, b:B, c:C) -> string {
                return a.ToStr() + ""-"" + b.ToStr() + ""-"" + c.ToStr()
            }

            result <- combine<int, string, double>(1, ""two"", 3.0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("1-two-3", ((StringLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_WithVoidReturn_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0

            func increment<T>(value:T) -> void {
                counter <- counter + 1
            }

            increment<int>(1)
            increment<string>(""test"")
            increment<double>(3.14)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<IntLangValue>(counter);
        Assert.Equal(3, ((IntLangValue)counter!).Value);
    }

    [Fact]
    public void GenericFunction_NestedCalls_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func double<T>(value:T) -> string {
                return value.ToStr() + value.ToStr()
            }

            func wrap<T>(value:T) -> string {
                return ""("" + value.ToStr() + "")""
            }

            temp <- double<int>(5)
            result <- wrap<string>(temp)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("(55)", ((StringLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_WithLocalVariables_ScopeHandledCorrectly()
    {
        // Arrange
        var code = @"
            func process<T>(input:T) -> string {
                temp <- input.ToStr()
                result <- ""processed: "" + temp
                return result
            }

            output1 <- process<int>(42)
            output2 <- process<string>(""hello"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var output1 = interpreter.Manager.GetValue(new LangId("output1"));
        var output2 = interpreter.Manager.GetValue(new LangId("output2"));

        Assert.Equal("processed: 42", ((StringLangValue)output1!).Value);
        Assert.Equal("processed: hello", ((StringLangValue)output2!).Value);
    }
}
