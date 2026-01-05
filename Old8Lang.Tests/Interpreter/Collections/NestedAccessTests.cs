using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 嵌套访问测试 - 测试嵌套索引和切片访问
/// </summary>
[Collection("Sequential")]
public class NestedAccessTests
{
    #region 嵌套索引访问

    /// <summary>
    /// 测试二维数组嵌套索引访问
    /// </summary>
    [Fact]
    public void Run_TwoDimensionalArrayNestedAccess_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
matrix <- [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
value1 <- matrix[0][0]
value2 <- matrix[1][1]
value3 <- matrix[2][2]
sum <- value1 + value2 + value3";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.IsType<IntLangValue>(sum);
        Assert.Equal(15, ((IntLangValue)sum).Value); // 1 + 5 + 9 = 15
    }

    /// <summary>
    /// 测试嵌套字典访问
    /// </summary>
    [Fact]
    public void Run_NestedDictionaryAccess_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
data <- {
    ""user"": {
        ""name"": ""Alice"",
        ""age"": 25,
        ""address"": {
            ""city"": ""Beijing"",
            ""zip"": ""100000""
        }
    }
}

userName <- data[""user""][""name""]
userCity <- data[""user""][""address""][""city""]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var userName = interpreter.Manager.GetValue(new LangId("userName"));
        var userCity = interpreter.Manager.GetValue(new LangId("userCity"));

        Assert.IsType<StringLangValue>(userName);
        Assert.Equal("Alice", ((StringLangValue)userName).Value);

        Assert.IsType<StringLangValue>(userCity);
        Assert.Equal("Beijing", ((StringLangValue)userCity).Value);
    }

    /// <summary>
    /// 测试列表嵌套访问
    /// </summary>
    [Fact]
    public void Run_NestedListAccess_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
lists <- {{1, 2}, {3, 4}, {5, 6}}
value1 <- lists[0][1]
value2 <- lists[2][0]
result <- value1 + value2";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(7, ((IntLangValue)result).Value); // 2 + 5 = 7
    }

    #endregion

    #region 混合嵌套访问

    /// <summary>
    /// 测试混合类型嵌套访问（列表中的字典）
    /// </summary>
    [Fact]
    public void Run_ListOfDictionariesNestedAccess_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
users <- {
    {""name"": ""Alice"", ""age"": 25},
    {""name"": ""Bob"", ""age"": 30},
    {""name"": ""Charlie"", ""age"": 35}
}

name1 <- users[0][""name""]
age2 <- users[1][""age""]
name3 <- users[2][""name""]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var name1 = interpreter.Manager.GetValue(new LangId("name1"));
        var age2 = interpreter.Manager.GetValue(new LangId("age2"));
        var name3 = interpreter.Manager.GetValue(new LangId("name3"));

        Assert.IsType<StringLangValue>(name1);
        Assert.Equal("Alice", ((StringLangValue)name1).Value);

        Assert.IsType<IntLangValue>(age2);
        Assert.Equal(30, ((IntLangValue)age2).Value);

        Assert.IsType<StringLangValue>(name3);
        Assert.Equal("Charlie", ((StringLangValue)name3).Value);
    }

    /// <summary>
    /// 测试混合类型嵌套访问（字典中的列表）
    /// </summary>
    [Fact]
    public void Run_DictionaryOfListsNestedAccess_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
data <- {
    ""scores"": {85, 90, 95},
    ""grades"": {""A"", ""B"", ""A""},
    ""years"": {2021, 2022, 2023}
}

score2 <- data[""scores""][1]
grade3 <- data[""grades""][2]
year1 <- data[""years""][0]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var score2 = interpreter.Manager.GetValue(new LangId("score2"));
        var grade3 = interpreter.Manager.GetValue(new LangId("grade3"));
        var year1 = interpreter.Manager.GetValue(new LangId("year1"));

        Assert.IsType<IntLangValue>(score2);
        Assert.Equal(90, ((IntLangValue)score2).Value);

        Assert.IsType<StringLangValue>(grade3);
        Assert.Equal("A", ((StringLangValue)grade3).Value);

        Assert.IsType<IntLangValue>(year1);
        Assert.Equal(2021, ((IntLangValue)year1).Value);
    }

    #endregion

    #region 嵌套切片访问

    /// <summary>
    /// 测试二维数组的嵌套切片
    /// </summary>
    [Fact]
    public void Run_TwoDimensionalArrayNestedSlice_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
matrix <- [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
row <- matrix[1]
subRow <- row[0:2]
result <- subRow[0] + subRow[1]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(9, ((IntLangValue)result).Value); // 4 + 5 = 9
    }

    #endregion

    #region 深度嵌套访问

    /// <summary>
    /// 测试三层嵌套访问
    /// </summary>
    [Fact]
    public void Run_ThreeLevelNestedAccess_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
data <- [
    [
        [1, 2],
        [3, 4]
    ],
    [
        [5, 6],
        [7, 8]
    ]
]

value1 <- data[0][0][1]
value2 <- data[1][1][0]
result <- value1 + value2";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(9, ((IntLangValue)result).Value); // 2 + 7 = 9
    }

    /// <summary>
    /// 测试四层嵌套字典访问
    /// </summary>
    [Fact]
    public void Run_FourLevelNestedDictionaryAccess_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
data <- {
    ""level1"": {
        ""level2"": {
            ""level3"": {
                ""level4"": ""Deep Value""
            }
        }
    }
}

value <- data[""level1""][""level2""][""level3""][""level4""]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var value = interpreter.Manager.GetValue(new LangId("value"));
        Assert.IsType<StringLangValue>(value);
        Assert.Equal("Deep Value", ((StringLangValue)value).Value);
    }

    #endregion

    #region 嵌套访问修改

    /// <summary>
    /// 测试嵌套索引修改
    /// </summary>
    [Fact]
    public void Run_NestedIndexModification_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
matrix <- [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
matrix[1][1] <- 100
result <- matrix[1][1]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试嵌套字典修改
    /// </summary>
    [Fact]
    public void Run_NestedDictionaryModification_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
data <- {
    ""user"": {
        ""name"": ""Alice"",
        ""age"": 25
    }
}

data[""user""][""age""] <- 26
result <- data[""user""][""age""]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(26, ((IntLangValue)result).Value);
    }

    #endregion
}
