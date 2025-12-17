using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Statements;

/// <summary>
/// 控制流综合测试
/// </summary>
public class ControlFlowTests
{
    [Fact]
    public void ControlFlow_IfForCombination_CombinesIfAndFor()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            evenNumbers <- {}
            for num in numbers {
                if num % 2 == 0 {
                    evenNumbers.Add(num)
                }
            }
            result <- evenNumbers
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(5, list.Value.Count);
        Assert.Equal(2, ((IntLangValue)list.Value[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Value[1]).Value);
        Assert.Equal(6, ((IntLangValue)list.Value[2]).Value);
        Assert.Equal(8, ((IntLangValue)list.Value[3]).Value);
        Assert.Equal(10, ((IntLangValue)list.Value[4]).Value);
    }

    [Fact]
    public void ControlFlow_NestedIfFor_NestedControlFlow()
    {
        // Arrange
        var code = @"
            matrix <- [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
            diagonalSum <- 0
            for i in 0..<matrix.Length {
                for j in 0..<matrix[i].Length {
                    if i == j {
                        diagonalSum <- diagonalSum + matrix[i][j]
                    }
                }
            }
            result <- diagonalSum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+5+9 = 15
    }

    [Fact]
    public void ControlFlow_WhileIfCombination_CombinesWhileAndIf()
    {
        // Arrange
        var code = @"
            counter <- 0
            sum <- 0
            while counter < 100 {
                counter <- counter + 1
                if counter % 5 == 0 {
                    continue
                }
                if counter > 50 {
                    break
                }
                sum <- sum + counter
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1040, ((IntLangValue)result).Value); // Sum of 1-50 excluding multiples of 5
    }

    [Fact]
    public void ControlFlow_SwitchForCombination_CombinesSwitchAndFor()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            result <- {}
            for num in numbers {
                switch num {
                    case 1:
                        result.Add(""one"")
                    case 2:
                        result.Add(""two"")
                    case 3:
                        result.Add(""three"")
                    case 4:
                        result.Add(""four"")
                    case 5:
                        result.Add(""five"")
                    default:
                        result.Add(""unknown"")
                }
            }
            finalResult <- result.Join("" "")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("one two three four five", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ControlFlow_IfElseForLoop_ConditionalLoopProcessing()
    {
        // Arrange
        var code = @"
            numbers <- {15, 8, 23, 12, 7, 19, 3}
            adults <- {}
            children <- {}
            for age in numbers {
                if age >= 18 {
                    adults.Add(""Adult: "" + age.ToStr())
                } else {
                    children.Add(""Child: "" + age.ToStr())
                }
            }
            result <- ""Adults: "" + adults.Length.ToStr() + "", Children: "" + children.Length.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Adults: 3, Children: 4", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ControlFlow_ComplexNested_ComplexNestedStructure()
    {
        // Arrange
        var code = @"
            data <- {
                ""users"": [
                    {""name"": ""Alice"", ""age"": 30, ""active"": true},
                    {""name"": ""Bob"", ""age"": 25, ""active"": false},
                    {""name"": ""Charlie"", ""age"": 35, ""active"": true},
                    {""name"": ""Diana"", ""age"": 28, ""active"": true}
                ]
            }
            activeUserCount <- 0
            totalAge <- 0

            if data.ContainsKey(""users"") {
                users <- data[""users""]
                for user in users {
                    if user[""active""] {
                        activeUserCount <- activeUserCount + 1
                        totalAge <- totalAge + user[""age""]
                    }
                }
            }

            averageAge <- if activeUserCount > 0 then totalAge / activeUserCount else 0
            result <- ""Active users: "" + activeUserCount.ToStr() + "", Average age: "" + averageAge.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Active users: 3, Average age: 31", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ControlFlow_FibonacciWithConditions_FibonacciWithConditionalLogic()
    {
        // Arrange
        var code = @"
            fibonacci <- {}
            a <- 0
            b <- 1
            for i in 0..10 {
                fibonacci.Add(a)

                if i % 2 == 0 {
                    // Even index: add next number
                    next <- a + b
                    a <- b
                    b <- next
                } else {
                    // Odd index: square current number
                    a <- a * a
                }

                if a > 1000 {
                    break
                }
            }
            result <- fibonacci
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.True(list.Value.Count > 0);
    }

    [Fact]
    public void ControlFlow_SearchWithMultipleConditions_MultiConditionSearch()
    {
        // Arrange
        var code = @"
            products <- [
                {""name"": ""Laptop"", ""price"": 999, ""category"": ""Electronics"", ""stock"": 10},
                {""name"": ""Mouse"", ""price"": 25, ""category"": ""Electronics"", ""stock"": 50},
                {""name"": ""Book"", ""price"": 15, ""category"": ""Education"", ""stock"": 100},
                {""name"": ""Desk"", ""price"": 200, ""category"": ""Furniture"", ""stock"": 5}
            ]
            expensiveElectronics <- {}
            affordableItems <- {}

            for product in products {
                price <- product[""price""]
                category <- product[""category""]
                stock <- product[""stock""]

                if category == ""Electronics"" and price > 100 {
                    expensiveElectronics.Add(product[""name""])
                }

                if price < 50 and stock > 20 {
                    affordableItems.Add(product[""name""])
                }
            }

            result <- ""Expensive electronics: "" + expensiveElectronics.Length.ToStr() + ", Affordable items: "" + affordableItems.Length.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Expensive electronics: 1, Affordable items: 1", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ControlFlow_StateMachine_SimpleStateMachine()
    {
        // Arrange
        var code = @"
            state <- ""START""
            steps <- 0
            maxSteps <- 20

            while steps < maxSteps {
                steps <- steps + 1

                switch state {
                    case ""START"":
                        state <- ""PROCESSING""
                    case ""PROCESSING"":
                        if steps > 5 {
                            state <- ""COMPLETE""
                        }
                    case ""COMPLETE"":
                        break
                    default:
                        state <- ""START""
                }
            }

            result <- ""Final state: "" + state + "", Steps taken: "" + steps.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Final state: COMPLETE, Steps taken: 7", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ControlFlow_TreeTraversal_TreeTraversalWithConditions()
    {
        // Arrange
        var code = @"
            tree <- {
                ""value"": 1,
                ""left"": {
                    ""value"": 2,
                    ""left"": {""value"": 4},
                    ""right"": {""value"": 5}
                },
                ""right"": {
                    ""value"": 3,
                    ""left"": null,
                    ""right"": {""value"": 6}
                }
            }

            sum <- 0
            count <- 0
            stack <- {tree}

            while stack.Length > 0 {
                node <- stack.RemoveAt(0)

                if node != null {
                    value <- node[""value""]
                    count <- count + 1

                    if value % 2 == 0 {
                        sum <- sum + value
                    }

                    left <- node[""left""]
                    right <- node[""right""]

                    if right != null {
                        stack.Insert(0, right)
                    }
                    if left != null {
                        stack.Insert(0, left)
                    }
                }
            }

            result <- ""Count: "" + count.ToStr() + ", Sum of evens: "" + sum.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Count: 6, Sum of evens: 12", ((StringLangValue)result).Value); // 2+4+6 = 12
    }

    [Fact]
    public void ControlFlow_FizzBuzzWithConditions_FizzBuzzWithComplexLogic()
    {
        // Arrange
        var code = @"
            result <- {}
            for i in 1..20 {
                output <- """"

                if i % 3 == 0 {
                    output <- output + ""Fizz""
                }

                if i % 5 == 0 {
                    output <- output + ""Buzz""
                }

                if output == """" {
                    output <- i.ToStr()
                }

                // Special condition for prime numbers
                isPrime <- true
                if i > 1 {
                    for j in 2..i-1 {
                        if i % j == 0 {
                            isPrime <- false
                            break
                        }
                    }
                } else {
                    isPrime <- false
                }

                if isPrime {
                    output <- output + "" (prime)""
                }

                result.Add(output)
            }
            finalResult <- result.Join("" "")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        var resultString = ((StringLangValue)result).Value;
        Assert.Contains("1 (prime)", resultString);
        Assert.Contains("FizzBuzz", resultString);
    }

    [Fact]
    public void ControlFlow_BubbleSortWithConditions_BubbleSortWithEarlyExit()
    {
        // Arrange
        var code = @"
            numbers <- [64, 34, 25, 12, 22, 11, 90]
            n <- numbers.Length

            for i in 0..<n {
                swapped <- false

                for j in 0..(n - i - 2) {
                    if numbers[j] > numbers[j + 1] {
                        // Swap elements
                        temp <- numbers[j]
                        numbers[j] <- numbers[j + 1]
                        numbers[j + 1] <- temp
                        swapped <- true
                    }
                }

                // If no swapping occurred, array is sorted
                if not swapped {
                    break
                }
            }

            result <- numbers
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var array = (ArrayLangValue)result;
        Assert.Equal(7, array.Value.Count);
        // Check if sorted (first element should be smallest)
        Assert.Equal(11, ((IntLangValue)array.Value[0]).Value);
        Assert.Equal(90, ((IntLangValue)array.Value[6]).Value);
    }

    [Fact]
    public void ControlFlow_BinarySearchWithConditions_BinarySearchAlgorithm()
    {
        // Arrange
        var code = @"
            sortedArray <- [2, 5, 8, 12, 16, 23, 38, 56, 72, 91]
            target <- 23
            found <- false
            index <- -1
            low <- 0
            high <- sortedArray.Length - 1
            iterations <- 0

            while low <= high and iterations < 20 {
                iterations <- iterations + 1
                mid <- (low + high) / 2
                value <- sortedArray[mid]

                if value == target {
                    found <- true
                    index <- mid
                    break
                } else if value < target {
                    low <- mid + 1
                } else {
                    high <- mid - 1
                }
            }

            result <- ""Found: "" + found.ToStr() + "", Index: "" + index.ToStr() + "", Iterations: "" + iterations.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Found: true, Index: 5, Iterations: 3", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ControlFlow_GradeCalculator_ComplexGradeCalculation()
    {
        // Arrange
        var code = @"
            students <- [
                {""name"": ""Alice"", ""scores"": [85, 92, 78, 95]},
                {""name"": ""Bob"", ""scores"": [76, 84, 81, 79]},
                {""name"": ""Charlie"", ""scores"": [95, 98, 92, 96]},
                {""name"": ""Diana"", ""scores"": [88, 91, 89, 93]}
            ]

            gradeStats <- {}

            for student in students {
                scores <- student[""scores""]
                sum <- 0
                count <- 0
                hasFailed <- false

                for score in scores {
                    sum <- sum + score
                    count <- count + 1

                    if score < 70 {
                        hasFailed <- true
                    }
                }

                average <- if count > 0 then sum / count else 0

                grade <- """"
                if average >= 90 {
                    grade <- ""A""
                } else if average >= 80 {
                    grade <- ""B""
                } else if average >= 70 {
                    grade <- ""C""
                } else {
                    grade <- ""F""
                }

                status <- if hasFailed then ""Needs Improvement"" else ""Pass""

                studentResult <- student[""name""] + "": " + average.ToStr() + " (" + grade + ") - " + status
                gradeStats.Add(studentResult)
            }

            result <- gradeStats.Join("" | "")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        var resultString = ((StringLangValue)result).Value;
        Assert.Contains("Alice", resultString);
        Assert.Contains("Bob", resultString);
        Assert.Contains("Charlie", resultString);
        Assert.Contains("Diana", resultString);
    }
}