using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 列表操作解释模式测试
/// </summary>
public class ListTests
{
    [Fact]
    public void ListCreation_EmptyList_CreatesEmptyList()
    {
        // Arrange
        var code = @"
            emptyList <- {}
            result <- len(emptyList)
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
    public void ListCreation_WithElements_CreatesCorrectList()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            result <- len(numbers)
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
    public void ListCreation_MixedTypes_CreatesListWithDifferentTypes()
    {
        // Arrange
        var code = @"
            mixed <- {1, ""hello"", true, 3.14, 'A'}
            result <- len(mixed)
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
    public void ListAccess_ByIndex_ReturnsCorrectElement()
    {
        // Arrange
        var code = @"
            fruits <- {""apple"", ""banana"", ""cherry"", ""date""}
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
    public void ListAssignment_ByIndex_ModifiesElement()
    {
        // Arrange
        var code = @"
            numbers <- {10, 20, 30, 40, 50}
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
    public void ListLength_Property_ReturnsCorrectLength()
    {
        // Arrange
        var code = @"
            empty <- {}
            single <- {42}
            multiple <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            result1 <- len(empty)
            result2 <- len(single)
            result3 <- len(multiple)
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
    public void ListPush_AddsElementToEnd()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 3}
            items.Add(4)
            items.Add(5)
            result1 <- len(items)
            result2 <- items[3]
            result3 <- items[4]
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
        Assert.Equal(4, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void ListPop_RemovesAndReturnsLastElement()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 3, 4, 5}
            popped1 <- items.Pop()
            popped2 <- items.Pop()
            result1 <- len(items)
            result2 <- popped1
            result3 <- popped2
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
        Assert.Equal(3, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(4, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void ListClear_RemovesAllElements()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            items.Clear()
            result <- len(items)
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
    public void ListContains_ChecksElementPresence()
    {
        // Arrange
        var code = @"
            items <- {""apple"", ""banana"", ""cherry"", ""date""}
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
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.True(((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.False(((BoolLangValue)result3).Value);
    }

    [Fact]
    public void ListFind_ReturnsFirstMatchingElement()
    {
        // Arrange
        var code = @"
            numbers <- {10, 20, 30, 40, 50, 60, 70, 80, 90, 100}
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
    public void ListFilter_ReturnsElementsMatchingCondition()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            evens <- numbers.Filter((x:int) -> x % 2 == 0)
            odds <- numbers.Filter((x:int) -> x % 2 == 1)
            greaterThan5 <- numbers.Filter((x:int) -> x > 5)
            result1 <- len(evens)
            result2 <- len(odds)
            result3 <- len(greaterThan5)
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
    public void ListMap_TransformsAllElements()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            doubled <- numbers.Map((x:int) -> x * 2)
            squared <- numbers.Map((x:int) -> x * x)
            toString <- numbers.Map((x:int) -> ""num: "" + x.ToStr())
            result1 <- len(doubled)
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
    public void ListReduce_AggregatesElements()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
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
    public void ListForEach_ExecutesActionOnEachElement()
    {
        // Arrange
        var code = @"
            sum <- 0
            doubledList <- {}
            numbers <- {1, 2, 3, 4, 5}

            numbers.ForEach((x:int) -> {
                sum <- sum + x
                doubledList.Push(x * 2)
            })

            result1 <- sum
            result2 <- len(doubledList)
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
    public void ListSort_OrdersElementsCorrectly()
    {
        // Arrange
        var code = @"
            unsorted <- {5, 2, 8, 1, 9, 3, 7, 4, 6}
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
    public void ListReverse_ReversesElementOrder()
    {
        // Arrange
        var code = @"
            original <- {1, 2, 3, 4, 5}
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
    public void ListSlice_ReturnsSublist()
    {
        // Arrange
        var code = @"
            numbers <- {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
            slice1 <- numbers.Slice(2, 5)
            slice2 <- numbers.Slice(0, 3)
            slice3 <- numbers.Slice(7, 10)
            result1 <- len(slice1)
            result2 <- slice1[0]
            result3 <- len(slice2)
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
    public void ListJoin_ConcatenatesToString()
    {
        // Arrange
        var code = @"
            words <- {""Hello"", ""World"", ""from"", ""Old8Lang""}
            result1 <- words.Join("" "")
            result2 <- words.Join("", "")
            result3 <- words.Join("" - "")
            numbers <- {1, 2, 3, 4, 5}
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
    public void ListConcat_CombinesTwoLists()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {4, 5, 6}
            combined <- list1.Concat(list2)
            result1 <- len(combined)
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
    public void ListInsert_AddsElementAtIndex()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 4, 5}
            items.Insert(2, 3)
            items.Insert(0, 0)
            items.Insert(6, 6)
            result1 <- len(items)
            result2 <- items[2]
            result3 <- items[3]
            result4 <- items[0]
            result5 <- items[6]
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
        Assert.Equal(7, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(3, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(4, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(0, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(6, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void ListRemove_RemovesElementAtIndex()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 3, 4, 5, 6, 7}
            removed1 <- items.Remove(2)
            removed2 <- items.Remove(3)
            result1 <- removed1
            result2 <- removed2
            result3 <- len(items)
            result4 <- items[2]
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
        Assert.Equal(3, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(4, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(5, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void ListIndexOf_ReturnsElementPosition()
    {
        // Arrange
        var code = @"
            items <- {10, 20, 30, 40, 50, 60, 70, 80, 90, 100}
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
    public void ListEquality_ComaresTwoLists()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3, 4, 5}
            list2 <- {1, 2, 3, 4, 5}
            list3 <- {1, 2, 3, 4, 6}
            list4 <- {1, 2, 3, 4}
            result1 <- list1 == list2
            result2 <- list1 == list3
            result3 <- list1 == list4
            result4 <- list1 != list3
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
        Assert.True(((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.False(((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.False(((BoolLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.True(((BoolLangValue)result4).Value);
    }

    [Fact]
    public void ListWithNestedLists_HandlesMultiDimensionalData()
    {
        // Arrange
        var code = @"
            matrix <- {
                {1, 2, 3},
                {4, 5, 6},
                {7, 8, 9}
            }
            result1 <- matrix[1][1]
            result2 <- matrix[0][2]
            result3 <- matrix[2][0]
            row <- matrix[1]
            result4 <- row[2]
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
        Assert.Equal(5, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(3, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(7, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(6, ((IntLangValue)result4).Value);
    }
}