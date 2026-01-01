using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 函数调用解释模式测试
/// </summary>
public class FunctionCallTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void FunctionCall_SimpleFunction_CallsAndReturnsCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            result <- add(3, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(8, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionCall_NoParameters_CallsFunctionWithoutArgs()
    {
        // Arrange
        var code = @"
            func getPi() -> double {
                return 3.14159
            }
            result <- getPi()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14159, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void FunctionCall_NoReturnType_CallsVoidFunction()
    {
        // Arrange
        var code = @"
            x <- 0
            func increment() {
                x <- x + 1
            }
            increment()
            increment()
            result <- x
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionCall_WithVariableParameters_PassesVariablesCorrectly()
    {
        // Arrange
        var code = @"
            func multiply(a:double, b:double) -> double {
                return a * b
            }
            x <- 3.5
            y <- 2.0
            result <- multiply(x, y)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(7.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void FunctionCall_NestedFunctionCalls_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func square(x:double) -> double {
                return x * x
            }
            func add(a:double, b:double) -> double {
                return a + b
            }
            result <- add(square(3.0), square(4.0))
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(25.0, ((DoubleLangValue)result).Value); // 9 + 16
    }

    [Fact]
    public void FunctionCall_WithMixedParameterTypes_HandlesDifferentTypes()
    {
        // Arrange
        var code = @"
            func createInfo(name:string, age:int, height:double, isActive:bool) -> string {
                status <- isActive ? ""active"" : ""inactive""
                return name + "" ("" + age.ToStr() + "", "" + height.ToStr() + "", "" + status + "")""
            }
            result <- createInfo(""Alice"", 25, 1.65, true)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Alice (25, 1.65, active)", ((StringLangValue)result).Value);
    }

    [Fact]
    public void FunctionCall_WithDefaultParameters_UsesDefaultsWhenNotProvided()
    {
        // Arrange
        var code = @"
            func greet(name:string, greeting: ""Hello"") -> string {
                return greeting + "", "" + name
            }
            result1 <- greet(""Alice"", ""Hi"")
            result2 <- greet(""Bob"")
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
        Assert.Equal("Hi, Alice", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Hello, Bob", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void FunctionCall_WithArrayParameters_PassesArraysCorrectly()
    {
        // Arrange
        var code = @"
            func sum(numbers:array) -> int {
                total <- 0
                for num in numbers {
                    total <- total + num
                }
                return total
            }
            nums <- [1, 2, 3, 4, 5]
            result <- sum(nums)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionCall_WithListParameters_PassesListsCorrectly()
    {
        // Arrange
        var code = @"
            func findMax(numbers:list) -> int {
                if len(numbers) == 0 {
                    return 0
                }
                max <- numbers[0]
                for num in numbers {
                    if num > max {
                        max <- num
                    }
                }
                return max
            }
            nums <- {3, 7, 2, 9, 1}
            result <- findMax(nums)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(9, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionCall_WithDictionaryParameters_PassesDictionariesCorrectly()
    {
        // Arrange
        var code = @"
            func getValue(data:dict, key:string, defaultValue) {
                if data.ContainsKey(key) {
                    return data[key]
                } else {
                    return defaultValue
                }
            }
            config <- {""host"": ""localhost"", ""port"": 8080}
            result1 <- getValue(config, ""host"", ""unknown"")
            result2 <- getValue(config, ""timeout"", 30)
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
        Assert.Equal("localhost", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(30, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void FunctionCall_WithTupleParameters_PassesTuplesCorrectly()
    {
        // Arrange
        var code = @"
            func getFirst(tuple:tuple) {
                if len(tuple) > 0 {
                    return tuple[0]
                } else {
                    return null
                }
            }
            numbers <- (10, 20)
            text <- (""hello"", ""world"")
            result1 <- getFirst(numbers)
            result2 <- getFirst(text)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(10, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("hello", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void FunctionCall_WithFunctionParameters_PassesFunctionsCorrectly()
    {
        // Arrange
        var code = @"
            func applyOperation(operation:func, a:int, b:int) -> int {
                return operation(a, b)
            }
            func add(x:int, y:int) -> int {
                return x + y
            }
            func multiply(x:int, y:int) -> int {
                return x * y
            }
            result1 <- applyOperation(add, 5, 3)
            result2 <- applyOperation(multiply, 4, 6)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(8, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(24, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void FunctionCall_WithLambdaParameters_PassesLambdasCorrectly()
    {
        // Arrange
        var code = @"
            func processNumbers1(transformer:function, numbers:list) -> list {
                result <- {}
                for num in numbers {
                    result.Add(transformer(num))
                }
                return result
            }
            func processNumbers2(transformer:function, numbers:list) -> list {
                result <- {}
                for num in numbers {
                    result.Add(transformer(num))
                }
                return result
            }
            nums <- {1, 2, 3, 4, 5}
            doubled <- processNumbers1((x:int) -> x * 2, nums)
            squared <- processNumbers2((x:int) -> x * x, nums)
            result1 <- doubled[2]
            result2 <- squared[3]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(6, ((IntLangValue)result1).Value); // 3 * 2

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(16, ((IntLangValue)result2).Value); // 4 * 4
    }

    [Fact]
    public void FunctionCall_RecursiveFunction_HandlesRecursionCorrectly()
    {
        // Arrange
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                } else {
                    return n * factorial(n - 1)
                }
            }
            func fibonacci(n:int) -> int {
                if n <= 1 {
                    return n
                } else {
                    return fibonacci(n - 1) + fibonacci(n - 2)
                }
            }
            result1 <- factorial(5)
            result2 <- factorial(0)
            result3 <- fibonacci(6)
            result4 <- fibonacci(1)
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
        Assert.Equal(120, ((IntLangValue)result1).Value); // 5! = 120

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(1, ((IntLangValue)result2).Value); // 0! = 1

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(8, ((IntLangValue)result3).Value); // fibonacci(6) = 8

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(1, ((IntLangValue)result4).Value); // fibonacci(1) = 1
    }

    [Fact]
    public void FunctionCall_WithEarlyReturn_ExitsEarlyCorrectly()
    {
        // Arrange
        var code = @"
            func findFirstNegative(numbers:array) -> int {
                for num in numbers {
                    if num < 0 {
                        return num
                    }
                }
                return 0
            }
            result1 <- findFirstNegative([1, 2, -3, 4, 5])
            result2 <- findFirstNegative([1, 2, 3, 4, 5])
            result3 <- findFirstNegative([-1, 2, 3])
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
        Assert.Equal(-3, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(0, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(-1, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void FunctionCall_WithComplexLogic_HandlesComplexOperations()
    {
        // Arrange
        var code = @"
            func analyzeString(text:string) {
                letters <- 0
                digits <- 0
                others <- 0
                for char in text {
                    if (char >= 'a' and char <= 'z') or (char >= 'A' and char <= 'Z') {
                        letters <- letters + 1
                    } elif char >= '0' and char <= '9' {
                        digits <- digits + 1
                    } else {
                        others <- others + 1
                    }
                }
                return [letters, digits, others]
            }
            result <- analyzeString(""Hello123!@#"")
            lettersCount <- result[0]
            digitsCount <- result[1]
            othersCount <- result[2]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        testOutputHelper.WriteLine($"Debug - Result: {result}");

        var lettersCount = interpreter.Manager.GetValue(new LangId("lettersCount"));
        var digitsCount = interpreter.Manager.GetValue(new LangId("digitsCount"));
        var othersCount = interpreter.Manager.GetValue(new LangId("othersCount"));

        testOutputHelper.WriteLine($"Debug - Letters: {lettersCount}, Digits: {digitsCount}, Others: {othersCount}");

        Assert.NotNull(lettersCount);
        Assert.IsType<IntLangValue>(lettersCount);
        Assert.Equal(5, ((IntLangValue)lettersCount).Value); // H e l l o

        Assert.NotNull(digitsCount);
        Assert.IsType<IntLangValue>(digitsCount);
        Assert.Equal(3, ((IntLangValue)digitsCount).Value); // 1 2 3

        Assert.NotNull(othersCount);
        Assert.IsType<IntLangValue>(othersCount);
        Assert.Equal(3, ((IntLangValue)othersCount).Value); // ! @ #
    }

    [Fact]
    public void FunctionCall_WithSideEffects_ModifiesGlobalState()
    {
        // Arrange
        var code = @"
            counter <- 0
            messages <- {}

            func incrementCounter() {
                counter <- counter + 1
            }

            func addMessage(msg:string) {
                messages.Add(msg)
            }

            func processAction(action:string) {
                incrementCounter()
                addMessage(action + "" processed "")
                incrementCounter()
            }

            processAction(""Task 1"")
            processAction(""Task 2"")

            counterResult <- counter
            messagesCount <- len(messages)
            lastMessage <- messages[-1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var counterResult = interpreter.Manager.GetValue(new LangId("counterResult"));
        var messagesCount = interpreter.Manager.GetValue(new LangId("messagesCount"));
        var lastMessage = interpreter.Manager.GetValue(new LangId("lastMessage"));

        Assert.NotNull(counterResult);
        Assert.IsType<IntLangValue>(counterResult);
        Assert.Equal(4, ((IntLangValue)counterResult).Value); // 函数可以修改全局变量，每次调用processAction增加2

        Assert.NotNull(messagesCount);
        Assert.IsType<IntLangValue>(messagesCount);
        Assert.Equal(2, ((IntLangValue)messagesCount).Value);

        Assert.NotNull(lastMessage);
        Assert.IsType<StringLangValue>(lastMessage);
        Assert.Equal("Task 2 processed ", ((StringLangValue)lastMessage).Value);
    }

    [Fact]
    public void FunctionCall_WithOptionalParameters_HandlesMissingArgs()
    {
        // Arrange
        var code = @"
                               func buildUrl(base:string, path: """", param: {}) -> string {
                                   url <- base
                                   if len(path) > 0 {
                                       url <- url + ""/"" + path
                                   }

                                   if len(param) > 0 {
                                       url <- url + ""?""
                                       first <- true
                                       for key in param.Keys {
                                           if not first {
                                               url <- url + ""&""
                                           }
                                           url <- url + key + ""="" + param[key].ToStr()
                                           first <- false
                                       }
                                   }
                                   return url
                               }

                               result1 <- buildUrl(""https://api.example.com"")
                               result2 <- buildUrl(""https://api.example.com"", ""users"")
                               queryParams <- {""page"": ""1"", ""limit"": ""10""}
                               result3 <- buildUrl(""https://api.example.com"", ""users"", queryParams)

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
        Assert.Equal("https://api.example.com", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("https://api.example.com/users", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Contains("https://api.example.com/users?", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void FunctionCall_WithExpressionParameters_EvaluatesExpressions()
    {
        // Arrange
        var code = @"
            func calculate(a:int, b:int, operation:string) -> int {
                if operation == ""add"" {
                    return a + b
                } else if operation == ""multiply"" {
                    return a * b
                } else if operation == ""power"" {
                    return a ^ b
                } else {
                    return 0
                }
            }

            x <- 5
            y <- 3
            result1 <- calculate(x + 2, y * 2, ""add"")
            result2 <- calculate(x * y, x - y, ""multiply"")
            result3 <- calculate(2, 3, ""power"")
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
        Assert.Equal(13, ((IntLangValue)result1).Value); // (5+2) + (3*2) = 7 + 6 = 13

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(30, ((IntLangValue)result2).Value); // (5*3) * (5-3) = 15 * 2 = 30

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(8, ((IntLangValue)result3).Value); // 2^3 = 8
    }

    [Fact]
    public void FunctionCall_WithComplexReturnTypes_HandlesVariousTypes()
    {
        // Arrange
        var code = @"
            func createComplexData() -> tuple {
                name <- ""Alice""
                age <- 25
                return (name, age)
            }

            result <- createComplexData()
            nameResult <- result[0]
            ageResult <- result[1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var nameResult = interpreter.Manager.GetValue(new LangId("nameResult"));
        var ageResult = interpreter.Manager.GetValue(new LangId("ageResult"));

        Assert.NotNull(nameResult);
        Assert.IsType<StringLangValue>(nameResult);
        Assert.Equal("Alice", ((StringLangValue)nameResult).Value);

        Assert.NotNull(ageResult);
        Assert.IsType<IntLangValue>(ageResult);
        Assert.Equal(25, ((IntLangValue)ageResult).Value);
    }
}