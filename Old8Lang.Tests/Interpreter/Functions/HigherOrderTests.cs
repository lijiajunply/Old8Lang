using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 高阶函数测试
/// </summary>
public class HigherOrderTests
{
    [Fact]
    public void HigherOrder_FunctionAsParameter_AcceptsFunctionParameter()
    {
        // Arrange
        var code = @"
            func applyOperation(x:int, y:int, operation:func) -> int {
                return operation(x, y)
            }
            func add(a:int, b:int) -> int {
                return a + b
            }
            result <- applyOperation(10, 20, add)
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
    public void HigherOrder_FunctionAsParameter_WithLambda_PassesLambdaAsArgument()
    {
        // Arrange
        var code = @"
            func calculate(numbers:{int}, transformer:func) -> {int} {
                results <- {}
                for num in numbers {
                    transformed <- transformer(num)
                    results.Add(transformed)
                }
                return results
            }
            result <- calculate({1, 2, 3, 4, 5}, (x:int) -> x * 2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultList = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(resultList);
        Assert.IsType<ListLangValue>(resultList);
        var list = (ListLangValue)resultList;
        Assert.Equal(5, list.Value.Count);
        Assert.Equal(2, ((IntLangValue)list.Value[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Value[1]).Value);
        Assert.Equal(6, ((IntLangValue)list.Value[2]).Value);
        Assert.Equal(8, ((IntLangValue)list.Value[3]).Value);
        Assert.Equal(10, ((IntLangValue)list.Value[4]).Value);
    }

    [Fact]
    public void HigherOrder_ReturnFunction_ReturnsNewFunction()
    {
        // Arrange
        var code = @"
            func createMultiplier(factor:int) -> func {
                return (x:int) -> x * factor
            }
            doubler <- createMultiplier(2)
            tripler <- createMultiplier(3)
            result1 <- doubler(10)
            result2 <- tripler(10)
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
        Assert.Equal(20, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(30, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void HigherOrder_MapFunction_ImplementsMapOperation()
    {
        // Arrange
        var code = @"
            func map(collection:{int}, mapper:func) -> {int} {
                result <- {}
                for item in collection {
                    mappedItem <- mapper(item)
                    result.Add(mappedItem)
                }
                return result
            }
            numbers <- {1, 2, 3, 4, 5}
            squares <- map(numbers, (x:int) -> x * x)
            result <- squares
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
        Assert.Equal(1, ((IntLangValue)list.Value[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Value[1]).Value);
        Assert.Equal(9, ((IntLangValue)list.Value[2]).Value);
        Assert.Equal(16, ((IntLangValue)list.Value[3]).Value);
        Assert.Equal(25, ((IntLangValue)list.Value[4]).Value);
    }

    [Fact]
    public void HigherOrder_FilterFunction_ImplementsFilterOperation()
    {
        // Arrange
        var code = @"
            func filter(collection:{int}, predicate:func) -> {int} {
                result <- {}
                for item in collection {
                    if predicate(item) {
                        result.Add(item)
                    }
                }
                return result
            }
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            evens <- filter(numbers, (x:int) -> x % 2 == 0)
            result <- evens
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
    public void HigherOrder_ReduceFunction_ImplementsReduceOperation()
    {
        // Arrange
        var code = @"
            func reduce(collection:{int}, accumulator:func, initialValue:int) -> int {
                result <- initialValue
                for item in collection {
                    result <- accumulator(result, item)
                }
                return result
            }
            numbers <- {1, 2, 3, 4, 5}
            sum <- reduce(numbers, (acc:int, x:int) -> acc + x, 0)
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
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void HigherOrder_ComposeFunction_ComposesTwoFunctions()
    {
        // Arrange
        var code = @"
            func compose(f:func, g:func) -> func {
                return (x:int) -> f(g(x))
            }
            addFive <- (x:int) -> x + 5
            multiplyTwo <- (x:int) -> x * 2
            addFiveThenMultiply <- compose(multiplyTwo, addFive)
            result <- addFiveThenMultiply(10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value); // (10 + 5) * 2 = 30
    }

    [Fact]
    public void HigherOrder_CurryFunction_ImplementsCurrying()
    {
        // Arrange
        var code = @"
            func curry(binaryFunc:func) -> func {
                return (a:int) -> {
                    return (b:int) -> binaryFunc(a, b)
                }
            }
            func add(a:int, b:int) -> int {
                return a + b
            }
            curriedAdd <- curry(add)
            addTen <- curriedAdd(10)
            result <- addTen(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 10 + 5 = 15
    }

    [Fact]
    public void HigherOrder_PipeFunction_ImplementsFunctionChaining()
    {
        // Arrange
        var code = @"
            func pipe(value:int, functions:{func}) -> int {
                result <- value
                for func in functions {
                    result <- func(result)
                }
                return result
            }
            addOne <- (x:int) -> x + 1
            multiplyThree <- (x:int) -> x * 3
            subtractTwo <- (x:int) -> x - 2
            operations <- {addOne, multiplyThree, subtractTwo}
            result <- pipe(10, operations)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(31, ((IntLangValue)result).Value); // ((10 + 1) * 3) - 2 = 31
    }

    [Fact]
    public void HigherOrder_UntilFunction_RepeatsUntilCondition()
    {
        // Arrange
        var code = @"
            func until(condition:func, action:func, initialValue:int) -> int {
                value <- initialValue
                while not condition(value) {
                    value <- action(value)
                }
                return value
            }
            isGreaterThan100 <- (x:int) -> x > 100
            double <- (x:int) -> x * 2
            result <- until(isGreaterThan100, double, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(160, ((IntLangValue)result).Value); // 5, 10, 20, 40, 80, 160
    }

    [Fact]
    public void HigherOrder_MemoizeFunction_CachesFunctionResults()
    {
        // Arrange
        var code = @"
            func memoize(func:func) -> func {
                cache <- {}
                return (x:int) -> {
                    if cache.ContainsKey(x.ToStr()) {
                        return cache[x.ToStr()]
                    } else {
                        result <- func(x)
                        cache[x.ToStr()] <- result
                        return result
                    }
                }
            }
            callCount <- 0
            func expensiveOperation(n:int) -> int {
                callCount <- callCount + 1
                // Simulate expensive computation
                return n * n
            }
            memoizedOp <- memoize(expensiveOperation)

            result1 <- memoizedOp(10)
            result2 <- memoizedOp(10)
            result3 <- memoizedOp(20)
            result4 <- memoizedOp(20)

            finalCallCount <- callCount
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
        var callCount = interpreter.Manager.GetValue(new LangId("finalCallCount"));

        Assert.NotNull(result1);
        Assert.Equal(100, ((IntLangValue)result1).Value);
        Assert.NotNull(result2);
        Assert.Equal(100, ((IntLangValue)result2).Value);
        Assert.NotNull(result3);
        Assert.Equal(400, ((IntLangValue)result3).Value);
        Assert.NotNull(result4);
        Assert.Equal(400, ((IntLangValue)result4).Value);

        // Should only call the expensive operation twice (for 10 and 20)
        Assert.NotNull(callCount);
        Assert.Equal(2, ((IntLangValue)callCount).Value);
    }

    [Fact]
    public void HigherOrder_ThrottleFunction_LimitsFunctionExecutionRate()
    {
        // Arrange
        var code = @"
            func throttle(func:func, delayMs:int) -> func {
                lastExecution <- 0
                return (x:int) -> {
                    currentTime <- 1000000 // Simulate current time
                    if currentTime - lastExecution >= delayMs {
                        lastExecution <- currentTime
                        return func(x)
                    } else {
                        return -1 // Indicates throttled
                    }
                }
            }
            func processValue(value:int) -> int {
                return value * 2
            }
            throttledProcess <- throttle(processValue, 500)

            result1 <- throttledProcess(10)
            result2 <- throttledProcess(20) // Should be throttled
            result3 <- throttledProcess(30) // Should be throttled
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
        Assert.Equal(20, ((IntLangValue)result1).Value);
        Assert.NotNull(result2);
        Assert.Equal(-1, ((IntLangValue)result2).Value); // Throttled
        Assert.NotNull(result3);
        Assert.Equal(-1, ((IntLangValue)result3).Value); // Throttled
    }

    [Fact]
    public void HigherOrder_PartialFunction_ImplementsPartialApplication()
    {
        // Arrange
        var code = @"
            func partial(func:func, firstArg:string) -> func {
                return (secondArg:int) -> func(firstArg, secondArg)
            }
            func greet(name:string, age:int) -> string {
                return ""Hello "" + name + "", you are "" + age.ToStr() + "" years old""
            }
            greetAlice <- partial(greet, ""Alice"")
            result <- greetAlice(25)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello Alice, you are 25 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void HigherOrder_FlipFunction_SwapsFunctionParameters()
    {
        // Arrange
        var code = @"
            func flip(func:func) -> func {
                return (a:int, b:string) -> func(b, a)
            }
            func repeat(text:string, count:int) -> string {
                result <- """"
                for i in 1..count {
                    result <- result + text
                }
                return result
            }
            flippedRepeat <- flip(repeat)
            result <- flippedRepeat(3, ""Hi"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("HiHiHi", ((StringLangValue)result).Value);
    }

    [Fact]
    public void HigherOrder_ZipFunction_CombinesTwoCollections()
    {
        // Arrange
        var code = @"
            func zip(list1:{int}, list2:{string}, combiner:func) -> {string} {
                result <- {}
                minLength <- if list1.Length < list2.Length then list1.Length else list2.Length
                for i in 0..minLength-1 {
                    combined <- combiner(list1[i], list2[i])
                    result.Add(combined)
                }
                return result
            }
            numbers <- {1, 2, 3}
            names <- {""Alice"", ""Bob"", ""Charlie""}
            combined <- zip(numbers, names, (num:int, name:string) -> name + num.ToStr())
            result <- combined
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
        Assert.Equal(3, list.Value.Count);
        Assert.Equal("Alice1", ((StringLangValue)list.Value[0]).Value);
        Assert.Equal("Bob2", ((StringLangValue)list.Value[1]).Value);
        Assert.Equal("Charlie3", ((StringLangValue)list.Value[2]).Value);
    }

    [Fact]
    public void HigherOrder_ChainFunctions_BuildsProcessingChain()
    {
        // Arrange
        var code = @"
            func chain(value:int, processors:{func}) -> int {
                result <- value
                for processor in processors {
                    result <- processor(result)
                }
                return result
            }

            validate <- (x:int) -> if x < 0 then 0 else x
            normalize <- (x:int) -> if x > 100 then 100 else x
            scale <- (x:int) -> x * 10
            offset <- (x:int) -> x + 5

            processors <- {validate, normalize, scale, offset}
            result1 <- chain(50, processors)
            result2 <- chain(150, processors)
            result3 <- chain(-10, processors)
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
        Assert.Equal(505, ((IntLangValue)result1).Value); // 50 -> 50 -> 50 -> 500 -> 505
        Assert.NotNull(result2);
        Assert.Equal(1005, ((IntLangValue)result2).Value); // 150 -> 150 -> 100 -> 1000 -> 1005
        Assert.NotNull(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value); // -10 -> 0 -> 0 -> 0 -> 5
    }

    [Fact]
    public void HigherOrder_FoldRightFunction_ImplementsRightFold()
    {
        // Arrange
        var code = @"
            func foldRight(collection:{string}, accumulator:func, initial:string) -> string {
                result <- initial
                for i in collection.Length-1..0 {
                    item <- collection[i]
                    result <- accumulator(item, result)
                }
                return result
            }
            words <- {""Hello"", "" "", ""World"", ""!""}
            result <- foldRight(words, (word:string, acc:string) -> word + acc, """")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void HigherOrder_GroupByFunction_GroupsByPredicate()
    {
        // Arrange
        var code = @"
            func groupBy(collection:{int}, keySelector:func) -> {{int}} {
                groups <- {}
                for item in collection {
                    key <- keySelector(item)
                    if not groups.ContainsKey(key.ToStr()) {
                        groups[key.ToStr()] <- {}
                    }
                    groups[key.ToStr()].Add(item)
                }
                return groups
            }
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            isEven <- (x:int) -> x % 2
            grouped <- groupBy(numbers, isEven)

            // Get even numbers group (key = 0)
            evenCount <- grouped[""0""].Length
            result <- evenCount
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value); // 5 even numbers: 2, 4, 6, 8, 10
    }

    [Fact]
    public void HigherOrder_DebounceFunction_DelaysFunctionExecution()
    {
        // Arrange
        var code = @"
            func debounce(func:func, delayMs:int) -> func {
                timeoutId <- -1
                return (x:int) -> {
                    // Simulate debounce logic
                    if timeoutId != -1 {
                        // Cancel previous timeout
                        timeoutId <- -1
                    }
                    // Set new timeout
                    timeoutId <- 1
                    return func(x)
                }
            }
            func process(value:int) -> int {
                return value * 3
            }
            debouncedProcess <- debounce(process, 300)

            // Multiple rapid calls
            result1 <- debouncedProcess(10)
            result2 <- debouncedProcess(20)
            result3 <- debouncedProcess(30)
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
        Assert.Equal(30, ((IntLangValue)result1).Value);
        Assert.NotNull(result2);
        Assert.Equal(60, ((IntLangValue)result2).Value);
        Assert.NotNull(result3);
        Assert.Equal(90, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void HigherOrder_AccumulateFunction_AccumulatesWithOperation()
    {
        // Arrange
        var code = @"
            func accumulate(collection:{int}, operation:func) -> {int} {
                result <- {}
                if collection.Length > 0 {
                    runningTotal <- collection[0]
                    result.Add(runningTotal)
                    for i in 1..collection.Length-1 {
                        runningTotal <- operation(runningTotal, collection[i])
                        result.Add(runningTotal)
                    }
                }
                return result
            }
            numbers <- {1, 2, 3, 4, 5}
            runningSums <- accumulate(numbers, (acc:int, x:int) -> acc + x)
            result <- runningSums
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
        Assert.Equal(1, ((IntLangValue)list.Value[0]).Value);
        Assert.Equal(3, ((IntLangValue)list.Value[1]).Value); // 1+2
        Assert.Equal(6, ((IntLangValue)list.Value[2]).Value); // 3+3
        Assert.Equal(10, ((IntLangValue)list.Value[3]).Value); // 6+4
        Assert.Equal(15, ((IntLangValue)list.Value[4]).Value); // 10+5
    }
}