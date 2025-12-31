using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

/// <summary>
/// 集合方法高级功能测试
/// </summary>
[Collection("Sequential")]
public class CollectionMethodsTests
{
    [Fact]
    public void ListBasicOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            empty_list <- {}
            count <- empty_list.Count()
            
            // Test Add method
            empty_list.Add(1)
            one_element <- empty_list.First()
            Assert.Equal(1, one_element)
            
            // Test Contains method
            list <- {1, 2, 3, 4, 5}
            has_2 <- list.Contains(2)
            has_6 <- list.Contains(6)
            not_has_10 <- list.Contains(10)
            
            Assert.True(has_2)
            Assert.False(has_6)
            Assert.False(has_10)
            Assert.Equal(5, count)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArrayBasicOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            length <- arr.Length()
            
            // Test Reverse method
            reversed <- arr.Reverse()
            
            // Test Sort method
            unsorted <- [5, 3, 1, 4, 2]
            sorted <- unsorted.Sort()
            
            Assert.Equal(5, length)
            Assert.Equal([5, 4, 3, 2, 1], reversed)
            Assert.Equal([1, 2, 3, 4, 5], sorted)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DictionaryBasicOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            empty_dict <- {}
            count <- empty_dict.Count()
            
            // Test Add method
            empty_dict.Add(""name"", ""Alice"")
            added_count <- empty_dict.Count()
            
            // Test ContainsKey method
            has_name <- empty_dict.ContainsKey(""name"")
            has_email <- empty_dict.ContainsKey(""email"")
            
            Assert.Equal(1, added_count)
            Assert.True(has_name)
            Assert.False(has_email)
            
            // Test GetOrElse method
            name <- empty_dict.GetOrElse(""name"", ""Unknown"")
            default_email <- empty_dict.GetOrElse(""email"", ""No email"")
            
            // Test Remove method
            empty_dict.Remove(""name"")
            name_removed <- empty_dict.GetOrElse(""name"", ""Removed"")
            removed_count <- empty_dict.Count()
            
            Assert.Equal(0, removed_count)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void StringBasicOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            text <- ""Hello, World!""
            length <- text.Length()
            
            // Test ToUpper method
            upper <- text.ToUpper()
            
            // Test ToLower method
            lower <- text.ToLower()
            
            // Test Substring method
            substring <- text.Substring(7, 5)
            
            // Test Contains method
            has_hello <- text.Contains(""Hello"")
            not_found <- text.Contains(""xyz"")
            
            Assert.Equal(12, length)
            Assert.Equal(""HELLO, WORLD!"", upper)
            Assert.Equal(""hello, world! this is a test string."", lower)
            Assert.True(has_hello)
            Assert.False(not_found)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void CollectionConversions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // Array to List conversion
            array_data <- [1, 2, 3, 4, 5]
            list_from_array <- {}
            i <- 0
            while i < array_data.Length {
                list_from_array.Add(array_data[i])
                i <- i + 1
            }
            
            // List to Dictionary conversion
            list_to_convert <- {""a"": 1, ""b"": 2, ""c"": 3}
            dict_from_list <- {}
            key_count <- 0
            for item in list_to_convert {
                if item % 2 == 1 {
                    dict_from_list[item.ToStr()] <- item
                    key_count <- key_count + 1
                }
            }
            
            // Dictionary to List conversion
            dict_data <- {""x"": 10, ""y"": 20, ""z"": 30}
            keys <- dict_data.Keys()
            values <- {}
            value_count <- 0
            for key in keys {
                values.Add(dict_data[key])
                value_count <- value_count + 1
            }
            
            Assert.Equal(5, list_from_array.Count())
            Assert.Equal(1, dict_from_list.Count())
            Assert.Equal(3, values.Count())
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}