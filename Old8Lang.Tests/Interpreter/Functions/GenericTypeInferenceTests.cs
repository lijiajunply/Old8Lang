using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 泛型类型推断测试
/// 测试自动从函数调用参数推断泛型类型参数
/// </summary>
public class GenericTypeInferenceTests
{
    [Fact]
    public void GenericFunction_InferSingleTypeParameter_FromIntArgument()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            // 不显式指定类型，应该自动推断为 int
            result <- identity(42)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_InferSingleTypeParameter_FromStringArgument()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            // 不显式指定类型，应该自动推断为 string
            result <- identity(""hello"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("hello", ((StringLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_InferSingleTypeParameter_FromDoubleArgument()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            // 不显式指定类型，应该自动推断为 double
            result <- identity(3.14)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14, ((DoubleLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_InferSingleTypeParameter_FromBoolArgument()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            // 不显式指定类型，应该自动推断为 bool
            result <- identity(true)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_InferMultipleTypeParameters_FromArguments()
    {
        // Arrange
        var code = @"
            func makePair<K, V>(key:K, value:V) -> string {
                return key.ToStr() + "":"" + value.ToStr()
            }

            // 不显式指定类型，应该自动推断 K=string, V=int
            result <- makePair(""age"", 25)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("age:25", ((StringLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_InferSameTypeParameter_FromMultipleArguments()
    {
        // Arrange
        var code = @"
            func add<T>(a:T, b:T) -> string {
                return a.ToStr() + ""+"" + b.ToStr()
            }

            // 不显式指定类型，应该自动推断 T=int
            result <- add(10, 20)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("10+20", ((StringLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_InferTypeFromVariable()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            x <- 100
            // 从变量推断类型
            result <- identity(x)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_ExplicitTypeStillWorks()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            // 显式指定类型仍然应该工作
            result1 <- identity<int>(42)

            // 自动推断类型
            result2 <- identity(42)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(42, ((IntLangValue)result1!).Value);

        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(42, ((IntLangValue)result2!).Value);
    }

    [Fact]
    public void GenericFunction_InferWithNestedCall()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            func wrap<T>(value:T) -> string {
                return ""["" + value.ToStr() + ""]""
            }

            // 嵌套调用，都应该能推断类型
            result <- wrap(identity(42))
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("[42]", ((StringLangValue)result!).Value);
    }

    [Fact]
    public void GenericFunction_InferWithMixedTypes()
    {
        // Arrange
        var code = @"
            func combine<A, B, C>(a:A, b:B, c:C) -> string {
                return a.ToStr() + ""-"" + b.ToStr() + ""-"" + c.ToStr()
            }

            // 应该推断 A=int, B=string, C=double
            result <- combine(1, ""two"", 3.0)
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
    public void GenericFunction_InferReturnsCorrectType()
    {
        // Arrange
        var code = @"
            func getFirst<T>(a:T, b:T) -> T {
                return a
            }

            // 推断 T=int，返回值应该是 int
            intResult <- getFirst(100, 200)

            // 推断 T=string，返回值应该是 string
            stringResult <- getFirst(""first"", ""second"")
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
}
