using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 字典操作解释模式测试
/// </summary>
public class DictionaryTests
{
    [Fact]
    public void DictionaryCreation_EmptyDictionary_CreatesEmptyDictionary()
    {
        // Arrange
        var code = @"
            emptyDict <- {}
            result <- emptyDict.Count
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
    public void DictionaryCreation_WithElements_CreatesCorrectDictionary()
    {
        // Arrange
        var code = @"
            person <- {""name"": ""Alice"", ""age"": 25, ""city"": ""New York""}
            result <- person.Count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void DictionaryAccess_ByKey_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            scores <- {""math"": 95, ""science"": 87, ""history"": 92, ""english"": 88}
            result1 <- scores[""math""]
            result2 <- scores[""science""]
            result3 <- scores[""english""]
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
        Assert.Equal(95, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(87, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(88, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void DictionaryAssignment_ByKey_ModifiesOrAddsValue()
    {
        // Arrange
        var code = @"
            config <- {""host"": ""localhost"", ""port"": 8080}
            config[""port""] <- 9090
            config[""ssl""] <- true
            result1 <- config[""port""]
            result2 <- config[""ssl""]
            result3 <- config.Count
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
        Assert.Equal(9090, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void DictionaryContainsKey_ChecksKeyPresence()
    {
        // Arrange
        var code = @"
            student <- {""name"": ""Bob"", ""grade"": ""A"", ""active"": true}
            result1 <- student.ContainsKey(""name"")
            result2 <- student.ContainsKey(""grade"")
            result3 <- student.ContainsKey(""age"")
            result4 <- student.ContainsKey("""")
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
    public void DictionaryRemove_RemovesKeyAndValue()
    {
        // Arrange
        var code = @"
            data <- {""a"": 1, ""b"": 2, ""c"": 3, ""d"": 4, ""e"": 5}
            removed1 <- data.Remove(""c"")
            removed2 <- data.Remove(""x"")
            result1 <- removed1
            result2 <- removed2
            result3 <- data.Count
            result4 <- data.ContainsKey(""c"")
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
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(4, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.Equal(false, ((BoolLangValue)result4).Value);
    }

    [Fact]
    public void DictionaryClear_RemovesAllEntries()
    {
        // Arrange
        var code = @"
            settings <- {""theme"": ""dark"", ""language"": ""en"", ""notifications"": true, ""autoSave"": false}
            settings.Clear()
            result1 <- settings.Count
            result2 <- settings.ContainsKey(""theme"")
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
        Assert.Equal(0, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(false, ((BoolLangValue)result2).Value);
    }

    [Fact]
    public void DictionaryKeys_ReturnsAllKeys()
    {
        // Arrange
        var code = @"
            user <- {""id"": 123, ""username"": ""alice"", ""email"": ""alice@example.com"", ""active"": true}
            keys <- user.Keys
            result1 <- len(keys)
            result2 <- keys.Contains(""username"")
            result3 <- keys.Contains(""password"")
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
        Assert.Equal(4, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(false, ((BoolLangValue)result3).Value);
    }

    [Fact]
    public void DictionaryValues_ReturnsAllValues()
    {
        // Arrange
        var code = @"
            grades <- {""math"": 95, ""science"": 87, ""history"": 92, ""english"": 88}
            values <- grades.Values
            result1 <- len(values)
            result2 <- values.Contains(95)
            result3 <- values.Contains(100)
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
        Assert.Equal(4, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(false, ((BoolLangValue)result3).Value);
    }

    [Fact]
    public void DictionaryTryGet_SafelyReturnsValue()
    {
        // Arrange
        var code = @"
            config <- {""host"": ""localhost"", ""port"": 8080, ""ssl"": false}
            value1 <- config.TryGet(""host"", ""default"")
            value2 <- config.TryGet(""timeout"", 30)
            value3 <- config.TryGet(""ssl"", true)
            value4 <- config.TryGet(""missing"", ""not found"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var value1 = interpreter.Manager.GetValue(new LangId("value1"));
        var value2 = interpreter.Manager.GetValue(new LangId("value2"));
        var value3 = interpreter.Manager.GetValue(new LangId("value3"));
        var value4 = interpreter.Manager.GetValue(new LangId("value4"));

        Assert.NotNull(value1);
        Assert.IsType<StringLangValue>(value1);
        Assert.Equal("localhost", ((StringLangValue)value1).Value);

        Assert.NotNull(value2);
        Assert.IsType<IntLangValue>(value2);
        Assert.Equal(30, ((IntLangValue)value2).Value);

        Assert.NotNull(value3);
        Assert.IsType<BoolLangValue>(value3);
        Assert.Equal(false, ((BoolLangValue)value3).Value);

        Assert.NotNull(value4);
        Assert.IsType<StringLangValue>(value4);
        Assert.Equal("not found", ((StringLangValue)value4).Value);
    }

    [Fact]
    public void DictionaryGetOrElse_ReturnsValueOrAlternative()
    {
        // Arrange
        var code = @"
            settings <- {""volume"": 75, ""brightness"": 60}
            volume <- settings.GetOrElse(""volume"", 50)
            contrast <- settings.GetOrElse(""contrast"", 40)
            language <- settings.GetOrElse(""language"", ""en"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var volume = interpreter.Manager.GetValue(new LangId("volume"));
        var contrast = interpreter.Manager.GetValue(new LangId("contrast"));
        var language = interpreter.Manager.GetValue(new LangId("language"));

        Assert.NotNull(volume);
        Assert.IsType<IntLangValue>(volume);
        Assert.Equal(75, ((IntLangValue)volume).Value);

        Assert.NotNull(contrast);
        Assert.IsType<IntLangValue>(contrast);
        Assert.Equal(40, ((IntLangValue)contrast).Value);

        Assert.NotNull(language);
        Assert.IsType<StringLangValue>(language);
        Assert.Equal("en", ((StringLangValue)language).Value);
    }

    [Fact]
    public void DictionaryMerge_CombinesTwoDictionaries()
    {
        // Arrange
        var code = @"
            dict1 <- {""a"": 1, ""b"": 2, ""c"": 3}
            dict2 <- {""d"": 4, ""e"": 5, ""f"": 6}
            merged <- dict1.Merge(dict2)
            result1 <- merged.Count
            result2 <- merged[""a""]
            result3 <- merged[""f""]
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
        Assert.Equal(6, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(1, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(6, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void DictionaryMap_TransformsValues()
    {
        // Arrange
        var code = @"
            scores <- {""math"": 85, ""science"": 92, ""english"": 78, ""history"": 88}
            curved <- scores.Map((value:any) -> value + 5)
            grades <- scores.Map((value:any) -> {
                if value >= 90 { return ""A"" }
                else if value >= 80 { return ""B"" }
                else if value >= 70 { return ""C"" }
                else { return ""F"" }
            })
            result1 <- curved[""math""]
            result2 <- curved[""science""]
            result3 <- grades[""math""]
            result4 <- grades[""science""]
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
        Assert.Equal(90, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(97, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("B", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("A", ((StringLangValue)result4).Value);
    }

    [Fact]
    public void DictionaryFilter_SelectsEntriesByCondition()
    {
        // Arrange
        var code = @"
            inventory <- {""apple"": 5, ""banana"": 0, ""orange"": 12, ""grape"": 3, ""pear"": 0}
            inStock <- inventory.Filter((key:string, value:any) -> value > 0)
            highStock <- inventory.Filter((key:string, value:any) -> value >= 10)
            result1 <- inStock.Count
            result2 <- inStock.ContainsKey(""apple"")
            result3 <- inStock.ContainsKey(""banana"")
            result4 <- highStock.Count
            result5 <- highStock.ContainsKey(""orange"")
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
        Assert.Equal(3, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(false, ((BoolLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(1, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<BoolLangValue>(result5);
        Assert.Equal(true, ((BoolLangValue)result5).Value);
    }

    [Fact]
    public void DictionaryForEach_ExecutesActionOnEachEntry()
    {
        // Arrange
        var code = @"
            sum <- 0
            count <- 0
            data <- {""a"": 10, ""b"": 20, ""c"": 30, ""d"": 40}

            data.ForEach((key:string, value:any) -> {
                sum <- sum + value
                count <- count + 1
            })

            result1 <- sum
            result2 <- count
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
        Assert.Equal(100, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(4, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void DictionaryEquality_ComparesTwoDictionaries()
    {
        // Arrange
        var code = @"
            dict1 <- {""a"": 1, ""b"": 2, ""c"": 3}
            dict2 <- {""a"": 1, ""b"": 2, ""c"": 3}
            dict3 <- {""a"": 1, ""b"": 2, ""c"": 4}
            dict4 <- {""a"": 1, ""b"": 2}
            result1 <- dict1 == dict2
            result2 <- dict1 == dict3
            result3 <- dict1 == dict4
            result4 <- dict1 != dict3
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
    public void DictionaryWithComplexTypes_HandlesNestedDictionaries()
    {
        // Arrange
        var code = @"
            user <- {
                ""name"": ""Alice"",
                ""profile"": {
                    ""age"": 25,
                    ""city"": ""New York"",
                    ""interests"": {""music"": true, ""sports"": false, ""reading"": true}
                },
                ""settings"": {
                    ""theme"": ""dark"",
                    ""notifications"": true,
                    ""privacy"": {
                        ""public"": false,
                        ""friends"": true
                    }
                }
            }

            result1 <- user[""name""]
            result2 <- user[""profile""].ContainsKey(""age"")
            result3 <- user[""profile""].ContainsKey(""city"")
            result4 <- user[""profile""].ContainsKey(""country"")
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
        Assert.Equal("Alice", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(true, ((BoolLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.Equal(false, ((BoolLangValue)result4).Value);
    }

    [Fact]
    public void DictionaryWithLists_HandlesListValues()
    {
        // Arrange
        var code = @"
            data <- {
                ""numbers"": {1, 2, 3, 4, 5},
                ""strings"": {""hello"", ""world"", ""test""},
                ""mixed"": {1, ""two"", true, 4.0}
            }

            result1 <- len(data[""numbers""])
            result2 <- len(data[""strings""])
            result3 <- len(data[""mixed""])
            result4 <- data[""numbers""][2]
            result5 <- data[""strings""][0]
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
        Assert.Equal(5, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(3, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(4, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(3, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<StringLangValue>(result5);
        Assert.Equal("hello", ((StringLangValue)result5).Value);
    }

    [Fact]
    public void DictionaryUpdate_ModifiesExistingEntries()
    {
        // Arrange
        var code = @"
            scores <- {""math"": 85, ""science"": 90, ""english"": 78}
            updates <- {""science"": 95, ""english"": 82, ""history"": 88}
            scores.Update(updates)
            result1 <- scores[""math""]
            result2 <- scores[""science""]
            result3 <- scores[""english""]
            result4 <- scores[""history""]
            result5 <- scores.Count
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
        Assert.Equal(85, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(95, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(82, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(88, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(4, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void DictionaryClone_CreatesIndependentCopy()
    {
        // Arrange
        var code = @"
            original <- {""a"": 1, ""b"": 2, ""c"": 3}
            copy <- original.Clone()
            copy[""d""] <- 4
            copy[""a""] <- 10
            result1 <- original.Count
            result2 <- copy.Count
            result3 <- original[""a""]
            result4 <- copy[""a""]
            result5 <- original.ContainsKey(""d"")
            result6 <- copy.ContainsKey(""d"")
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
        Assert.Equal(3, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(4, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(1, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(10, ((IntLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<BoolLangValue>(result5);
        Assert.Equal(false, ((BoolLangValue)result5).Value);

        Assert.NotNull(result6);
        Assert.IsType<BoolLangValue>(result6);
        Assert.Equal(true, ((BoolLangValue)result6).Value);
    }
}