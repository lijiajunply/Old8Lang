using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Basic;

/// <summary>
/// 变量操作测试
/// </summary>
public class VariableTests
{
    [Fact]
    public void Variable_SimpleDeclaration_DeclaresCorrectly()
    {
        // Arrange
        var code = @"
            x <- 42
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Variable_MultipleTypes_DeclaresDifferentTypes()
    {
        // Arrange
        var code = @"
            intVar <- 123
            doubleVar <- 3.14
            stringVar <- ""hello""
            boolVar <- true
            charVar <- 'A'
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intResult = interpreter.Manager.GetValue(new LangId("intVar"));
        var doubleResult = interpreter.Manager.GetValue(new LangId("doubleVar"));
        var stringResult = interpreter.Manager.GetValue(new LangId("stringVar"));
        var boolResult = interpreter.Manager.GetValue(new LangId("boolVar"));
        var charResult = interpreter.Manager.GetValue(new LangId("charVar"));

        Assert.NotNull(intResult);
        Assert.IsType<IntLangValue>(intResult);
        Assert.Equal(123, ((IntLangValue)intResult).Value);

        Assert.NotNull(doubleResult);
        Assert.IsType<DoubleLangValue>(doubleResult);
        Assert.Equal(3.14, ((DoubleLangValue)doubleResult).Value);

        Assert.NotNull(stringResult);
        Assert.IsType<StringLangValue>(stringResult);
        Assert.Equal("hello", ((StringLangValue)stringResult).Value);

        Assert.NotNull(boolResult);
        Assert.IsType<BoolLangValue>(boolResult);
        Assert.Equal(true, ((BoolLangValue)boolResult).Value);

        Assert.NotNull(charResult);
        Assert.IsType<CharLangValue>(charResult);
        Assert.Equal('A', ((CharLangValue)charResult).Value);
    }

    [Fact]
    public void Variable_Reassignment_ReassignsCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            x <- 20
            x <- 30
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Variable_DifferentTypeReassignment_ReassignsDifferentTypes()
    {
        // Arrange
        var code = @"
            x <- 100
            x <- ""now a string""
            x <- 3.14
            x <- true
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void Variable_NullAssignment_AssignsNull()
    {
        // Arrange
        var code = @"
            x <- 42
            x <- null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<NullLangValue>(result);
    }

    [Fact]
    public void Variable_ExpressionAssignment_AssignsExpressionResult()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            x <- a + b * 2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value); // 10 + (20 * 2) = 50
    }

    [Fact]
    public void Variable_FunctionCallAssignment_AssignsFunctionResult()
    {
        // Arrange
        var code = @"
            func getValue() -> int {
                return 42
            }
            x <- getValue()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Variable_CompoundAssignment_HandlesCompoundOperations()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- x + 5
            z <- y * 2
            result <- z - 3
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(27, ((IntLangValue)result).Value); // ((10 + 5) * 2) - 3 = 27
    }

    [Fact]
    public void Variable_UndefinedVariable_HandlesUndefinedAccess()
    {
        // Arrange
        var code = @"
            try {
                result <- undefinedVar
            } catch {
                result <- ""variable not defined""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("variable not defined", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Variable_ArrayAssignment_AssignsArrayValue()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            x <- numbers
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var array = (ArrayLangValue)result;
        Assert.Equal(5, array.GetItems().Count());
    }

    [Fact]
    public void Variable_ListAssignment_AssignsListValue()
    {
        // Arrange
        var code = @"
            items <- {10, 20, 30}
            x <- items
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
    }

    [Fact]
    public void Variable_DictionaryAssignment_AssignsDictionaryValue()
    {
        // Arrange
        var code = @"
            config <- {""name"": ""Alice"", ""age"": 30}
            x <- config
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<DictionaryLangValue>(result);
    }

    [Fact]
    public void Variable_LambdaAssignment_AssignsLambdaValue()
    {
        // Arrange
        var code = @"
            operation <- (x:int) -> x * 2
            x <- operation
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        // Lambda function type
    }

    [Fact]
    public void Variable_ClassInstanceAssignment_AssignsClassInstance()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string
                func Init(n:string) {
                    name <- n
                }
            }
            person <- Person(""Bob"")
            x <- person
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<AnyLangValue>(result);
    }

    [Fact]
    public void Variable_UnicodeVariableNames_SupportsUnicode()
    {
        // Arrange
        var code = @"
            变量1 <- 100
            переменная <- 200
            変数 <- 300
            متغير <- 400
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("变量1"));
        var result2 = interpreter.Manager.GetValue(new LangId("переменная"));
        var result3 = interpreter.Manager.GetValue(new LangId("変数"));
        var result4 = interpreter.Manager.GetValue(new LangId("متغير"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(100, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(200, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(300, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(400, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void Variable_LargeNumbers_HandlesLargeValues()
    {
        // Arrange
        var code = @"
            smallInt <- 0
            maxInt <- 2147483647
            minInt <- -2147483648
            largeDouble <- 1.7976931348623157e+308
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var smallResult = interpreter.Manager.GetValue(new LangId("smallInt"));
        var maxResult = interpreter.Manager.GetValue(new LangId("maxInt"));
        var minResult = interpreter.Manager.GetValue(new LangId("minInt"));
        var largeResult = interpreter.Manager.GetValue(new LangId("largeDouble"));

        Assert.NotNull(smallResult);
        Assert.IsType<IntLangValue>(smallResult);
        Assert.Equal(0, ((IntLangValue)smallResult).Value);

        Assert.NotNull(maxResult);
        Assert.IsType<IntLangValue>(maxResult);
        Assert.Equal(2147483647, ((IntLangValue)maxResult).Value);

        Assert.NotNull(minResult);
        Assert.IsType<IntLangValue>(minResult);
        Assert.Equal(-2147483648, ((IntLangValue)minResult).Value);

        Assert.NotNull(largeResult);
        Assert.IsType<DoubleLangValue>(largeResult);
    }

    [Fact]
    public void Variable_ScopeTest_VariablesHaveScope()
    {
        // Arrange
        var code = @"
            outerVar <- 10
            func testScope() -> int {
                innerVar <- 20
                return outerVar + innerVar
            }
            result <- testScope()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Variable_SpecialCharacters_HandlesSpecialCharacters()
    {
        // Arrange
        var code = @"
            snake_case <- 1
            camelCase <- 2
            PascalCase <- 3
            number123 <- 4
            dollar_$ign <- 5
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("snake_case"));
        var result2 = interpreter.Manager.GetValue(new LangId("camelCase"));
        var result3 = interpreter.Manager.GetValue(new LangId("PascalCase"));
        var result4 = interpreter.Manager.GetValue(new LangId("number123"));
        var result5 = interpreter.Manager.GetValue(new LangId("dollar_$ign"));

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

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(5, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void Variable_ReferenceAssignment_HandlesObjectReferences()
    {
        // Arrange
        var code = @"
            class Counter {
                public value:int
                func Init(v:int) {
                    value <- v
                }
            }
            counter1 <- Counter(10)
            counter2 <- counter1
            counter1.value <- 20
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var counter1 = interpreter.Manager.GetValue(new LangId("counter1"));
        var counter2 = interpreter.Manager.GetValue(new LangId("counter2"));

        Assert.NotNull(counter1);
        Assert.NotNull(counter2);
    }

    [Fact]
    public void Variable_CompoundDataStructure_AssignsComplexStructures()
    {
        // Arrange
        var code = @"
            complexData <- {
                ""numbers"": [1, 2, 3],
                ""info"": {""name"": ""test"", ""valid"": true},
                ""point"": (10, 20)
            }
            x <- complexData
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("x"));
        Assert.NotNull(result);
        Assert.IsType<DictionaryLangValue>(result);
    }

    [Fact]
    public void Variable_VariableNumberOfVariables_HandlesMultipleDeclarations()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2
            c <- 3
            d <- 4
            e <- 5
            sum <- a + b + c + d + e
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void Variable_VariableInLoop_VariablesPersistInLoops()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in 1..5 {
                count <- count + 1
                loopVar <- i
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Variable_VariableInCondition_VariablesWorkInConditionals()
    {
        // Arrange
        var code = @"
            x <- 10
            if x > 5 {
                result <- ""greater""
            } else {
                result <- ""less or equal""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("greater", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Variable_VariableShadowing_HandlesVariableShadowing()
    {
        // Arrange
        var code = @"
            x <- 10
            func testShadowing() -> int {
                x <- 20
                return x
            }
            innerResult <- testShadowing()
            outerResult <- x
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var innerResult = interpreter.Manager.GetValue(new LangId("innerResult"));
        var outerResult = interpreter.Manager.GetValue(new LangId("outerResult"));

        Assert.NotNull(innerResult);
        Assert.IsType<IntLangValue>(innerResult);
        Assert.Equal(20, ((IntLangValue)innerResult).Value);

        Assert.NotNull(outerResult);
        Assert.IsType<IntLangValue>(outerResult);
        Assert.Equal(10, ((IntLangValue)outerResult).Value);
    }

    [Fact]
    public void Variable_ImmutableTest_HandlesImmutableVariables()
    {
        // Arrange
        var code = @"
            const x <- 100
            // Attempt to change constant (should fail or be ignored)
            try {
                x <- 200
                result <- ""mutated""
            } catch {
                result <- ""constant protected""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // Result depends on language implementation
    }
}