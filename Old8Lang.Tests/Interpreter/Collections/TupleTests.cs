using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 元组操作解释模式测试
/// </summary>
public class TupleTests
{
    [Fact]
    public void TupleCreation_EmptyTuple_CreatesEmptyTuple()
    {
        // Arrange
        var code = @"
            emptyTuple <- ()
            result <- emptyTuple.Length
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TupleCreation_WithElements_CreatesCorrectTuple()
    {
        // Arrange
        var code = @"
            tuple <- (1, 2, 3, 4, 5)
            result <- tuple.Length
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
    public void TupleCreation_MixedTypes_CreatesTupleWithDifferentTypes()
    {
        // Arrange
        var code = @"
            mixed <- (1, ""hello"", true, 3.14, 'A')
            result <- mixed.Length
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
    public void TupleAccess_ByIndex_ReturnsCorrectElement()
    {
        // Arrange
        var code = @"
            fruits <- (""apple"", ""banana"", ""cherry"", ""date"")
            result1 <- fruits[0]
            result2 <- fruits[2]
            result3 <- fruits[3]
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
        Assert.Equal("apple", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("cherry", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("date", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void TupleAssignment_ByIndex_ModifiesElement()
    {
        // Arrange
        var code = @"
            numbers <- (10, 20, 30, 40, 50)
            numbers[1] <- 25
            numbers[3] <- 45
            result1 <- numbers[1]
            result2 <- numbers[3]
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
        Assert.Equal(25, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(45, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void TupleLength_Property_ReturnsCorrectLength()
    {
        // Arrange
        var code = @"
            empty <- ()
            single <- (42,)
            multiple <- (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
            result1 <- empty.Length
            result2 <- single.Length
            result3 <- multiple.Length
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
        Assert.Equal(0, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(1, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(10, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void TupleContains_ChecksElementPresence()
    {
        // Arrange
        var code = @"
            items <- (""apple"", ""banana"", ""cherry"", ""date"")
            result1 <- items.Contains(""apple"")
            result2 <- items.Contains(""cherry"")
            result3 <- items.Contains(""grape"")
            result4 <- items.Contains("""")
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
        Assert.Equal(false, ((BoolLangValue)result3).Value);
    }

    [Fact]
    public void TupleFind_ReturnsFirstMatchingElement()
    {
        // Arrange
        var code = @"
            numbers <- (10, 20, 30, 40, 50, 60, 70, 80, 90, 100)
            result1 <- numbers.Find((x:int) -> x > 25)
            result2 <- numbers.Find((x:int) -> x > 75)
            result3 <- numbers.Find((x:int) -> x > 100)
            result4 <- numbers.Find((x:int) -> x == 45)
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
        Assert.Equal(30, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(80, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void TupleFilter_ReturnsElementsMatchingCondition()
    {
        // Arrange
        var code = @"
            numbers <- (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
            evens <- numbers.Filter((x:int) -> x % 2 == 0)
            odds <- numbers.Filter((x:int) -> x % 2 == 1)
            greaterThan5 <- numbers.Filter((x:int) -> x > 5)
            result1 <- evens.Length
            result2 <- odds.Length
            result3 <- greaterThan5.Length
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
        Assert.Equal(5, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void TupleMap_TransformsAllElements()
    {
        // Arrange
        var code = @"
            numbers <- (1, 2, 3, 4, 5)
            doubled <- numbers.Map((x:int) -> x * 2)
            squared <- numbers.Map((x:int) -> x * x)
            toString <- numbers.Map((x:int) -> ""num: "" + x.ToStr())
            result1 <- doubled.Length
            result2 <- doubled[2]
            result3 <- squared[3]
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
        Assert.Equal(5, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(6, ((IntLangValue)result2).Value); // 3 * 2

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(16, ((IntLangValue)result3).Value); // 4 * 4
    }

    [Fact]
    public void TupleReduce_AggregatesElements()
    {
        // Arrange
        var code = @"
            numbers <- (1, 2, 3, 4, 5)
            sum <- numbers.Reduce((acc:int, x:int) -> acc + x, 0)
            product <- numbers.Reduce((acc:int, x:int) -> acc * x, 1)
            max <- numbers.Reduce((acc:int, x:int) -> if acc > x then acc else x, numbers[0])
            min <- numbers.Reduce((acc:int, x:int) -> if acc < x then acc else x, numbers[0])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        var product = interpreter.Manager.GetValue(new LangId("product"));
        var max = interpreter.Manager.GetValue(new LangId("max"));
        var min = interpreter.Manager.GetValue(new LangId("min"));

        Assert.NotNull(sum);
        Assert.IsType<IntLangValue>(sum);
        Assert.Equal(15, ((IntLangValue)sum).Value);

        Assert.NotNull(product);
        Assert.IsType<IntLangValue>(product);
        Assert.Equal(120, ((IntLangValue)product).Value);

        Assert.NotNull(max);
        Assert.IsType<IntLangValue>(max);
        Assert.Equal(5, ((IntLangValue)max).Value);

        Assert.NotNull(min);
        Assert.IsType<IntLangValue>(min);
        Assert.Equal(1, ((IntLangValue)min).Value);
    }

    [Fact]
    public void TupleForEach_ExecutesActionOnEachElement()
    {
        // Arrange
        var code = @"
            sum <- 0
            doubledList <- {}
            numbers <- (1, 2, 3, 4, 5)

            numbers.ForEach((x:int) -> {
                sum <- sum + x
                doubledList.Push(x * 2)
            })

            result1 <- sum
            result2 <- doubledList.Length
            result3 <- doubledList[2]
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
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(6, ((IntLangValue)result3).Value); // 3 * 2
    }

    [Fact]
    public void TupleSort_OrdersElementsCorrectly()
    {
        // Arrange
        var code = @"
            unsorted <- (5, 2, 8, 1, 9, 3, 7, 4, 6)
            sorted <- unsorted.Sort()
            reverseSorted <- unsorted.Sort((a:int, b:int) -> b - a)
            result1 <- sorted[0]
            result2 <- sorted[4]
            result3 <- sorted[8]
            result4 <- reverseSorted[0]
            result5 <- reverseSorted[8]
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
        Assert.Equal(1, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(9, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(9, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(1, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void TupleReverse_ReversesElementOrder()
    {
        // Arrange
        var code = @"
            original <- (1, 2, 3, 4, 5)
            reversed <- original.Reverse()
            result1 <- original[0]
            result2 <- original[4]
            result3 <- reversed[0]
            result4 <- reversed[4]
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
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(1, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void TupleSlice_ReturnsSubtuple()
    {
        // Arrange
        var code = @"
            numbers <- (0, 1, 2, 3, 4, 5, 6, 7, 8, 9)
            slice1 <- numbers.Slice(2, 5)
            slice2 <- numbers.Slice(0, 3)
            slice3 <- numbers.Slice(7, 10)
            result1 <- slice1.Length
            result2 <- slice1[0]
            result3 <- slice2.Length
            result4 <- slice3[2]
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
        Assert.Equal(3, ((IntLangValue)result1).Value); // elements 2, 3, 4

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(9, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void TupleJoin_ConcatenatesToString()
    {
        // Arrange
        var code = @"
            words <- (""Hello"", ""World"", ""from"", ""Old8Lang"")
            result1 <- words.Join("" "")
            result2 <- words.Join("", "")
            result3 <- words.Join("" - "")
            numbers <- (1, 2, 3, 4, 5)
            result4 <- numbers.Join(""|"")
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
        Assert.Equal("Hello World from Old8Lang", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("HelloWorldfromOld8Lang", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Hello - World - from - Old8Lang", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void TupleConcat_CombinesTwoTuples()
    {
        // Arrange
        var code = @"
            tuple1 <- (1, 2, 3)
            tuple2 <- (4, 5, 6)
            combined <- tuple1.Concat(tuple2)
            result1 <- combined.Length
            result2 <- combined[2]
            result3 <- combined[3]
            result4 <- combined[5]
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
        Assert.Equal(6, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(3, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(4, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(6, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void TupleIndex_ReturnsElementPosition()
    {
        // Arrange
        var code = @"
            items <- (10, 20, 30, 40, 50, 60, 70, 80, 90, 100)
            result1 <- items.IndexOf(10)
            result2 <- items.IndexOf(50)
            result3 <- items.IndexOf(100)
            result4 <- items.IndexOf(55)
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
        Assert.Equal(4, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(9, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void TupleEquality_ComaresTwoTuples()
    {
        // Arrange
        var code = @"
            tuple1 <- (1, 2, 3, 4, 5)
            tuple2 <- (1, 2, 3, 4, 5)
            tuple3 <- (1, 2, 3, 4, 6)
            tuple4 <- (1, 2, 3, 4)
            result1 <- tuple1 == tuple2
            result2 <- tuple1 == tuple3
            result3 <- tuple1 == tuple4
            result4 <- tuple1 != tuple3
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
        Assert.IsType<BoolLangValue>(result1);
        Assert.Equal(true, ((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(false, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(false, ((BoolLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.Equal(true, ((BoolLangValue)result4).Value);
    }

    [Fact]
    public void TupleWithNestedTuples_HandlesMultiDimensionalData()
    {
        // Arrange
        var code = @"
            point3d <- ((1, 2), 3)
            nested <- ((1, 2, 3), (4, 5, 6), (7, 8, 9))
            result1 <- point3d[0][0]
            result2 <- point3d[0][1]
            result3 <- point3d[1]
            result4 <- nested[1][1]
            result5 <- nested[2][2]
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
        Assert.Equal(1, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(5, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(9, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void TupleDestructuring_AssignsToMultipleVariables()
    {
        // Arrange
        var code = @"
            person <- (""Alice"", 25, ""Engineer"")
            name <- person[0]
            age <- person[1]
            profession <- person[2]
            coordinates <- ((10, 20), 30)
            x <- coordinates[0][0]
            y <- coordinates[0][1]
            z <- coordinates[1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var name = interpreter.Manager.GetValue(new LangId("name"));
        var age = interpreter.Manager.GetValue(new LangId("age"));
        var profession = interpreter.Manager.GetValue(new LangId("profession"));
        var x = interpreter.Manager.GetValue(new LangId("x"));
        var y = interpreter.Manager.GetValue(new LangId("y"));
        var z = interpreter.Manager.GetValue(new LangId("z"));

        Assert.NotNull(name);
        Assert.IsType<StringLangValue>(name);
        Assert.Equal("Alice", ((StringLangValue)name).Value);

        Assert.NotNull(age);
        Assert.IsType<IntLangValue>(age);
        Assert.Equal(25, ((IntLangValue)age).Value);

        Assert.NotNull(profession);
        Assert.IsType<StringLangValue>(profession);
        Assert.Equal("Engineer", ((StringLangValue)profession).Value);

        Assert.NotNull(x);
        Assert.IsType<IntLangValue>(x);
        Assert.Equal(10, ((IntLangValue)x).Value);

        Assert.NotNull(y);
        Assert.IsType<IntLangValue>(y);
        Assert.Equal(20, ((IntLangValue)y).Value);

        Assert.NotNull(z);
        Assert.IsType<IntLangValue>(z);
        Assert.Equal(30, ((IntLangValue)z).Value);
    }

    [Fact]
    public void TupleRange_CreatesRangeTuple()
    {
        // Arrange
        var code = @"
            range1 <- 1..5
            range2 <- 10..15
            range3 <- -3..3
            result1 <- range1[0]
            result2 <- range1[4]
            result3 <- range1.Length
            result4 <- range2[2]
            result5 <- range3[3]
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
        Assert.Equal(1, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(12, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(0, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void TupleWithFunctions_ReturnsMultipleValues()
    {
        // Arrange
        var code = @"
            func GetMinMax(numbers:[int]) -> (int, int) {
                if numbers.Length == 0 {
                    return (0, 0)
                }
                min <- numbers[0]
                max <- numbers[0]
                for n in numbers {
                    if n < min { min <- n }
                    if n > max { max <- n }
                }
                return (min, max)
            }

            func DivideAndRemainder(dividend:int, divisor:int) -> (int, int) {
                quotient <- dividend / divisor
                remainder <- dividend % divisor
                return (quotient, remainder)
            }

            nums <- {3, 7, 1, 9, 2, 8}
            minMax <- GetMinMax(nums)
            divRem <- DivideAndRemainder(17, 5)

            min <- minMax[0]
            max <- minMax[1]
            quotient <- divRem[0]
            remainder <- divRem[1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var min = interpreter.Manager.GetValue(new LangId("min"));
        var max = interpreter.Manager.GetValue(new LangId("max"));
        var quotient = interpreter.Manager.GetValue(new LangId("quotient"));
        var remainder = interpreter.Manager.GetValue(new LangId("remainder"));

        Assert.NotNull(min);
        Assert.IsType<IntLangValue>(min);
        Assert.Equal(1, ((IntLangValue)min).Value);

        Assert.NotNull(max);
        Assert.IsType<IntLangValue>(max);
        Assert.Equal(9, ((IntLangValue)max).Value);

        Assert.NotNull(quotient);
        Assert.IsType<IntLangValue>(quotient);
        Assert.Equal(3, ((IntLangValue)quotient).Value);

        Assert.NotNull(remainder);
        Assert.IsType<IntLangValue>(remainder);
        Assert.Equal(2, ((IntLangValue)remainder).Value);
    }
}