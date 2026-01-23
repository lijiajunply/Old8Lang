using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机集合操作测试
/// 测试虚拟机执行数组、列表、字典、元组等集合操作的正确性
/// </summary>
[Collection("Sequential")]
public class VMCollectionTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 数组测试

    [Fact]
    public void ArrayCreation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            PrintLine(arr.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("1", output);
        Assert.Contains("2", output);
        Assert.Contains("3", output);
        Assert.Contains("4", output);
        Assert.Contains("5", output);
    }

    [Fact]
    public void ArrayAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            first <- arr[0]
            third <- arr[2]
            last <- arr[4]
            PrintLine(first.ToStr())
            PrintLine(third.ToStr())
            PrintLine(last.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("10", lines[0]);
        Assert.Equal("30", lines[1]);
        Assert.Equal("50", lines[2]);
    }

    [Fact]
    public void ArrayModification_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            arr[1] <- 99
            PrintLine(arr[0].ToStr())
            PrintLine(arr[1].ToStr())
            PrintLine(arr[2].ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("1", lines[0]);
        Assert.Equal("99", lines[1]);
        Assert.Equal("3", lines[2]);
    }

    [Fact]
    public void EmptyArray_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- []
            PrintLine(arr.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotNull(output);
    }

    [Fact]
    public void MixedTypeArray_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, ""hello"", true, 3.14]
            PrintLine(arr[0].ToStr())
            PrintLine(arr[1].ToStr())
            PrintLine(arr[2].ToStr())
            PrintLine(arr[3].ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("1", lines[0]);
        Assert.Equal("hello", lines[1]);
        Assert.Equal("true", lines[2]);
        Assert.Equal("3.14", lines[3]);
    }

    #endregion

    #region 列表测试

    [Fact]
    public void ListCreation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            PrintLine(list.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("1", output);
        Assert.Contains("2", output);
        Assert.Contains("3", output);
        Assert.Contains("4", output);
        Assert.Contains("5", output);
    }

    [Fact]
    public void ListAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {100, 200, 300}
            first <- list[0]
            second <- list[1]
            third <- list[2]
            PrintLine(first.ToStr())
            PrintLine(second.ToStr())
            PrintLine(third.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("100", lines[0]);
        Assert.Equal("200", lines[1]);
        Assert.Equal("300", lines[2]);
    }

    [Fact]
    public void EmptyList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {}
            PrintLine(list.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotNull(output);
    }

    [Fact]
    public void ListWithStrings_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {""apple"", ""banana"", ""cherry""}
            PrintLine(list[0].ToStr())
            PrintLine(list[1].ToStr())
            PrintLine(list[2].ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("apple", lines[0]);
        Assert.Equal("banana", lines[1]);
        Assert.Equal("cherry", lines[2]);
    }

    #endregion

    #region 字典测试

    [Fact]
    public void DictionaryCreation_ExecutesCorrectly()
    {
        // Arrange
        var code = """
                   dict <- {"name": "张三", "age": 25}
                   PrintLine(dict.ToStr())
                   """;

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("name", output);
        Assert.Contains("张三", output);
        Assert.Contains("age", output);
        Assert.Contains("25", output);
    }

    [Fact]
    public void DictionaryAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""x"": 10, ""y"": 20, ""z"": 30}
            x_val <- dict[""x""]
            y_val <- dict[""y""]
            z_val <- dict[""z""]
            PrintLine(x_val.ToStr())
            PrintLine(y_val.ToStr())
            PrintLine(z_val.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("10", lines[0]);
        Assert.Equal("20", lines[1]);
        Assert.Equal("30", lines[2]);
    }

    [Fact]
    public void DictionaryModification_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""count"": 1}
            dict[""count""] <- 99
            dict[""new_key""] <- ""new_value""
            PrintLine(dict[""count""].ToStr())
            PrintLine(dict[""new_key""].ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("99", lines[0]);
        Assert.Equal("new_value", lines[1]);
    }

    [Fact]
    public void EmptyDictionary_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {}
            PrintLine(dict.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotNull(output);
    }

    [Fact]
    public void DictionaryWithMixedTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""number"": 42, ""text"": ""hello"", ""flag"": true}
            PrintLine(dict[""number""].ToStr())
            PrintLine(dict[""text""].ToStr())
            PrintLine(dict[""flag""].ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("42", lines[0]);
        Assert.Equal("hello", lines[1]);
        Assert.Equal("true", lines[2]);
    }

    #endregion

    #region 元组测试

    [Fact]
    public void TupleCreation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            tuple <- (10, ""hello"")
            PrintLine(tuple.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("10", output);
        Assert.Contains("hello", output);
    }

    [Fact]
    public void TupleAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            tuple <- (100, 200)
            first <- tuple.Item1
            second <- tuple.Item2
            PrintLine(first.ToStr())
            PrintLine(second.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("100", lines[0]);
        Assert.Equal("200", lines[1]);
    }

    [Fact]
    public void TupleWithDifferentTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            tuple <- (""name"", 25)
            name <- tuple.Item1
            age <- tuple.Item2
            PrintLine(name.ToStr())
            PrintLine(age.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("name", lines[0]);
        Assert.Equal("25", lines[1]);
    }

    #endregion

    #region 嵌套集合测试

    [Fact]
    public void NestedArrays_ExecuteCorrectly()
    {
        // Arrange
        var code = @"
            matrix <- [[1, 2], [3, 4], [5, 6]]
            first_row <- matrix[0]
            element <- first_row[1]
            PrintLine(element.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("2", output);
    }

    [Fact]
    public void ArrayOfDictionaries_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            people <- [{""name"": ""Alice"", ""age"": 30}, {""name"": ""Bob"", ""age"": 25}]
            first_person <- people[0]
            name <- first_person[""name""]
            PrintLine(name.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Alice", output);
    }

    [Fact]
    public void DictionaryWithArrayValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            data <- {""numbers"": [1, 2, 3], ""letters"": [""a"", ""b"", ""c""]}
            numbers <- data[""numbers""]
            first_number <- numbers[0]
            PrintLine(first_number.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("1", output);
    }

    #endregion

    #region 集合操作综合测试

    [Fact]
    public void CollectionIteration_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            for item in arr {
                sum <- sum + item
            }
            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("15", output); // 1 + 2 + 3 + 4 + 5 = 15
    }

    [Fact]
    public void CollectionWithExpressions_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            arr <- [a + b, a - b, a * b]
            PrintLine(arr[0].ToStr())
            PrintLine(arr[1].ToStr())
            PrintLine(arr[2].ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("30", lines[0]); // 10 + 20
        Assert.Equal("-10", lines[1]); // 10 - 20
        Assert.Equal("200", lines[2]); // 10 * 20
    }

    [Fact]
    public void ComplexCollectionOperations_ExecuteCorrectly()
    {
        // Arrange
        var code = @"
            data <- {
                ""users"": [
                    {""name"": ""Alice"", ""scores"": [85, 90, 88]},
                    {""name"": ""Bob"", ""scores"": [78, 82, 80]}
                ]
            }
            users <- data[""users""]
            alice <- users[0]
            alice_scores <- alice[""scores""]
            alice_first_score <- alice_scores[0]
            PrintLine(alice_first_score.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("85", output);
    }

    #endregion
}