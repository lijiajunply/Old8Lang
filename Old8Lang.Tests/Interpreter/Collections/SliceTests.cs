using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 切片操作解释模式测试
/// </summary>
public class SliceTests
{
    [Fact]
    public void Slice_BasicArraySlice_ReturnsCorrectSubarray()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            slice1 <- numbers[2:5]
            slice2 <- numbers[0:3]
            slice3 <- numbers[7:10]
            result1 <- len(slice1)
            result2 <- slice1[0]
            result3 <- slice1[2]
            result4 <- len(slice2)
            result5 <- slice3[2]
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
        Assert.Equal(3, ((IntLangValue)result1).Value); // elements 2, 3, 4

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(3, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(3, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(10, ((IntLangValue)result5).Value); // slice3 = [8, 9, 10], so slice3[2] = 10
    }

    [Fact]
    public void Slice_WithStep_ReturnsElementsWithStep()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            step2 <- numbers[0:10:2]
            step3 <- numbers[1:10:3]
            reverse <- numbers[9:0:-1]
            result1 <- len(step2)
            result2 <- step2[2]
            result3 <- len(step3)
            result4 <- step3[2]
            result5 <- reverse[0]
            result6 <- reverse[4]
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
        Assert.Equal(5, ((IntLangValue)result1).Value); // 1, 3, 5, 7, 9

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value); // 2, 5, 8

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(8, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(10, ((IntLangValue)result5).Value);

        Assert.NotNull(result6);
        Assert.IsType<IntLangValue>(result6);
        Assert.Equal(6, ((IntLangValue)result6).Value); // reverse = [10,9,8,7,6,5,4,3,2], so reverse[4] = 6
    }

    [Fact]
    public void Slice_OpenEndedSlice_HandlesMissingStartOrEnd()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            fromStart <- numbers[:5]
            toEnd <- numbers[5:]
            all <- numbers[:]
            result1 <- len(fromStart)
            result2 <- fromStart[4]
            result3 <- len(toEnd)
            result4 <- toEnd[0]
            result5 <- len(all)
            result6 <- all[7]
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
        Assert.Equal(5, ((IntLangValue)result1).Value); // 1, 2, 3, 4, 5

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value); // 6, 7, 8, 9, 10

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(6, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(10, ((IntLangValue)result5).Value);

        Assert.NotNull(result6);
        Assert.IsType<IntLangValue>(result6);
        Assert.Equal(8, ((IntLangValue)result6).Value);
    }

    [Fact]
    public void Slice_ListSlice_ReturnsCorrectSublist()
    {
        // Arrange
        var code = @"
            fruits <- {""apple"", ""banana"", ""cherry"", ""date"", ""elderberry"", ""fig"", ""grape""}
            slice1 <- fruits[1:4]
            slice2 <- fruits[3:6]
            slice3 <- fruits[:3]
            result1 <- len(slice1)
            result2 <- slice1[1]
            result3 <- slice2[2]
            result4 <- slice3[0]
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
        Assert.Equal(3, ((IntLangValue)result1).Value); // banana, cherry, date

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("cherry", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("fig", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("apple", ((StringLangValue)result4).Value);
    }

    [Fact]
    public void Slice_TupleSlice_ReturnsCorrectSubtuple()
    {
        // Arrange
        var code = @"
            numbers <- (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
            slice1 <- numbers[2:6]
            slice2 <- numbers[4:8]
            slice3 <- numbers[1:9:2]
            result1 <- len(slice1)
            result2 <- slice1[0]
            result3 <- slice1[3]
            result4 <- slice2[1]
            result5 <- len(slice3)
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
        Assert.Equal(4, ((IntLangValue)result1).Value); // 3, 4, 5, 6

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(3, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(6, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(6, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(4, ((IntLangValue)result5).Value); // 2, 4, 6, 8
    }

    [Fact]
    public void Slice_StringSlice_ReturnsSubstring()
    {
        // Arrange
        var code = @"
            text <- ""Hello, World! Welcome to Old8Lang""
            slice1 <- text[0:5]
            slice2 <- text[7:12]
            slice3 <- text[14:21]
            slice4 <- text[25:]
            result1 <- slice1
            result2 <- slice2
            result3 <- slice3
            result4 <- slice4
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
        Assert.Equal("Hello", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("World", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Welcome", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("Old8Lang", ((StringLangValue)result4).Value);
    }

    [Fact]
    public void Slice_StringWithStep_ReturnsSteppedSubstring()
    {
        // Arrange
        var code = @"
            text <- ""ABCDEFGHIJKLMNO""
            everyOther <- text[0:15:2]
            everyThird <- text[0:15:3]
            reverse <- text[14::-1]
            result1 <- everyOther
            result2 <- everyThird
            result3 <- reverse
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
        Assert.Equal("ACEGIKMO", ((StringLangValue)result1).Value); // indices: 0,2,4,6,8,10,12,14

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("ADGJM", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("ONMLKJIHGFEDCBA", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void Slice_NegativeIndices_HandlesNegativeIndexing()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            lastThree <- numbers[-3:10]
            exceptLast <- numbers[0:-1]
            middle <- numbers[2:-2]
            lastOne <- numbers[-1:]
            result1 <- len(lastThree)
            result2 <- lastThree[0]
            result3 <- len(exceptLast)
            result4 <- middle[0]
            result5 <- lastOne[0]
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
        Assert.Equal(3, ((IntLangValue)result1).Value); // 8, 9, 10

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(8, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(9, ((IntLangValue)result3).Value); // 1-9

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(3, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(10, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void Slice_OutOfBounds_HandlesBoundaryConditions()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            overEnd <- numbers[2:20]
            beforeStart <- numbers[-10:3]
            empty1 <- numbers[5:5]
            empty2 <- numbers[3:2]
            result1 <- len(overEnd)
            result2 <- overEnd[2]
            result3 <- len(beforeStart)
            result4 <- len(empty1)
            result5 <- len(empty2)
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
        Assert.Equal(3, ((IntLangValue)result1).Value); // 3, 4, 5

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value); // 1, 2, 3

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(0, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(0, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void Slice_MultiDimensionalArray_HandlesNestedSlices()
    {
        // Arrange
        var code = @"
            matrix <- [
                [1, 2, 3, 4],
                [5, 6, 7, 8],
                [9, 10, 11, 12],
                [13, 14, 15, 16]
            ]
            rows <- matrix[1:3]
            rowSlice <- matrix[1][1:3]
            subMatrix <- {}
            for i <- 1, i < 3, i++ {
                subMatrix.Add(matrix[i][1:3])
            }
            result1 <- len(rows)
            result2 <- rowSlice[0]
            result3 <- rowSlice[1]
            result4 <- subMatrix[0][0]
            result5 <- subMatrix[1][1]
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
        Assert.Equal(2, ((IntLangValue)result1).Value); // rows 1 and 2

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(6, ((IntLangValue)result2).Value); // matrix[1][1]

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(7, ((IntLangValue)result3).Value); // matrix[1][2]

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(6, ((IntLangValue)result4).Value); // matrix[1][1]

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(11, ((IntLangValue)result5).Value); // matrix[2][2]
    }

    [Fact]
    public void Slice_Assignment_ModifiesSliceRange()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            numbers[2:5] <- [30, 40, 50]
            numbers[7:9] <- [80, 90]
            result1 <- numbers[2]
            result2 <- numbers[3]
            result3 <- numbers[4]
            result4 <- numbers[7]
            result5 <- numbers[8]
            result6 <- len(numbers)
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
        Assert.Equal(30, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(40, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(50, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(80, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(90, ((IntLangValue)result5).Value);

        Assert.NotNull(result6);
        Assert.IsType<IntLangValue>(result6);
        Assert.Equal(10, ((IntLangValue)result6).Value);
    }

    [Fact]
    public void Slice_Deletion_RemovesSliceRange()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            numbers[2:5] <- {}
            numbers[2:4] <- {}
            result1 <- len(numbers)
            result2 <- numbers[1]
            result3 <- numbers[2]
            result4 <- numbers[3]
            result5 <- numbers[4]
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
        Assert.Equal(5, ((IntLangValue)result1).Value); // After removing [2:5] and then [2:4]

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(8, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(9, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(10, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void Slice_WithVariables_UsesDynamicIndices()
    {
        // Arrange
        var code = @"
            start <- 2
            end <- 7
            step <- 2
            numbers <- [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            slice1 <- numbers[start:end]
            slice2 <- numbers[0:end:step]
            slice3 <- numbers[start:len(numbers)]
            result1 <- len(slice1)
            result2 <- slice1[0]
            result3 <- slice2[2]
            result4 <- slice3[3]
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
        Assert.Equal(5, ((IntLangValue)result1).Value); // 2, 3, 4, 5, 6

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(4, ((IntLangValue)result3).Value); // 0, 2, 4, 6

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(5, ((IntLangValue)result4).Value); // 2, 3, 4, 5, 6, 7, 8, 9, 10
    }

    [Fact]
    public void Slice_WithExpressions_EvaluatesComplexSlices()
    {
        // Arrange
        var code = @"
            numbers <- [10, 20, 30, 40, 50, 60, 70, 80, 90, 100]
            middleThird <- numbers[3:7]
            firstHalf <- numbers[0:len(numbers)/2]
            lastQuarter <- numbers[len(numbers)*3/4:len(numbers)]
            evens <- numbers[1:len(numbers):2]
            result1 <- len(middleThird)
            result2 <- middleThird[0]
            result3 <- len(firstHalf)
            result4 <- lastQuarter[0]
            result5 <- len(evens)
            result6 <- evens[2]
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
        Assert.Equal(4, ((IntLangValue)result1).Value); // 40, 50, 60, 70

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(40, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value); // 10, 20, 30, 40, 50

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(80, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(5, ((IntLangValue)result5).Value); // evens = [20, 40, 60, 80, 100], 5 elements

        Assert.NotNull(result6);
        Assert.IsType<IntLangValue>(result6);
        Assert.Equal(60, ((IntLangValue)result6).Value);
    }

    [Fact]
    public void Slice_SlicingStrings_MaintainsCharacterTypes()
    {
        // Arrange
        var code = @"
            text <- ""12345ABCdef!@#$%""
            numbers <- text[0:5]
            letters <- text[5:8]
            mixed <- text[8:]
            symbols <- text[-4:]
            result1 <- numbers
            result2 <- letters
            result3 <- mixed
            result4 <- symbols
            result5 <- text[0:len(text)]
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
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("12345", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("ABC", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("def!@#$%", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("@#$%", ((StringLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<StringLangValue>(result5);
        Assert.Equal("12345ABCdef!@#$%", ((StringLangValue)result5).Value);
    }
}