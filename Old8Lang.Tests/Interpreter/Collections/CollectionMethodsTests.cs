using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 集合方法测试
/// </summary>
public class CollectionMethodsTests
{
    [Fact]
    public void CollectionMethods_ListAdd_AddsElementsToList()
    {
        // Arrange
        var code = @"
            numbers <- {}
            numbers.Add(1)
            numbers.Add(2)
            numbers.Add(3)
            result <- numbers
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
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
    }

    [Fact]
    public void CollectionMethods_ListRemove_RemovesElementsFromList()
    {
        // Arrange
        var code = @"
            items <- {10, 20, 30, 40, 50}
            items.Remove(30)
            result <- items
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
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(10, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(20, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(40, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(50, ((IntLangValue)list.Values[3]).Value);
    }

    [Fact]
    public void CollectionMethods_ListInsert_InsertsElementAtIndex()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 4, 5}
            items.Insert(2, 3)
            result <- items
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
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[4]).Value);
    }

    [Fact]
    public void CollectionMethods_ListIndexOf_FindsElementIndex()
    {
        // Arrange
        var code = @"
            items <- {""apple"", ""banana"", ""cherry"", ""date""}
            index1 <- items.IndexOf(""cherry"")
            index2 <- items.IndexOf(""grape"")
            result <- ""cherry: "" + index1.ToStr() + "", grape: "" + index2.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("cherry: 2, grape: -1", ((StringLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_ListSort_SortsListElements()
    {
        // Arrange
        var code = @"
            numbers <- {5, 2, 8, 1, 9, 3}
            numbers.Sort()
            result <- numbers
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
        Assert.Equal(6, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[4]).Value);
        Assert.Equal(9, ((IntLangValue)list.Values[5]).Value);
    }

    [Fact]
    public void CollectionMethods_ListReverse_ReversesList()
    {
        // Arrange
        var code = @"
            items <- {""first"", ""second"", ""third""}
            items.Reverse()
            result <- items
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
        Assert.Equal(3, list.Values.Count);
        Assert.Equal("third", ((StringLangValue)list.Values[0]).Value);
        Assert.Equal("second", ((StringLangValue)list.Values[1]).Value);
        Assert.Equal("first", ((StringLangValue)list.Values[2]).Value);
    }

    [Fact]
    public void CollectionMethods_ListClear_ClearsList()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 3, 4, 5}
            items.Clear()
            result <- items.Length
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
    public void CollectionMethods_DictionaryAdd_AddsKeyValuePair()
    {
        // Arrange
        var code = @"
            scores <- {}
            scores.Add(""Alice"", 95)
            scores.Add(""Bob"", 87)
            scores.Add(""Charlie"", 92)
            result <- scores
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DictionaryLangValue>(result);
    }

    [Fact]
    public void CollectionMethods_DictionaryRemove_RemovesKey()
    {
        // Arrange
        var code = @"
            data <- {""name"": ""Alice"", ""age"": 30, ""city"": ""New York""}
            data.Remove(""age"")
            hasAge <- data.ContainsKey(""age"")
            result <- hasAge
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(false, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_DictionaryContainsKey_ChecksKeyExists()
    {
        // Arrange
        var code = @"
            config <- {""timeout"": 30, ""retries"": 3, ""debug"": true}
            hasTimeout <- config.ContainsKey(""timeout"")
            hasLogging <- config.ContainsKey(""logging"")
            result <- ""timeout: "" + hasTimeout.ToStr() + "", logging: "" + hasLogging.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("timeout: true, logging: false", ((StringLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_DictionaryGetOrElse_GetsValueWithDefault()
    {
        // Arrange
        var code = @"
            settings <- {""theme"": ""dark"", ""language"": ""en""}
            theme <- settings.GetOrElse(""theme"", ""light"")
            fontSize <- settings.GetOrElse(""fontSize"", 12)
            result <- ""theme: "" + theme + "", fontSize: "" + fontSize.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("theme: dark, fontSize: 12", ((StringLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_DictionaryKeys_GetsAllKeys()
    {
        // Arrange
        var code = @"
            data <- {""a"": 1, ""b"": 2, ""c"": 3}
            keys <- data.Keys
            keyCount <- keys.Length
            result <- keyCount
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
    public void CollectionMethods_DictionaryValues_GetsAllValues()
    {
        // Arrange
        var code = @"
            numbers <- {""one"": 1, ""two"": 2, ""three"": 3}
            values <- numbers.Values
            sum <- 0
            for val in values {
                sum <- sum + val
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
        Assert.Equal(6, ((IntLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_ArraySort_SortsArray()
    {
        // Arrange
        var code = @"
            numbers <- [8, 3, 5, 1, 9, 2]
            sortedArray <- numbers.Sort()
            result <- sortedArray
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
        Assert.Equal(6, array.GetItems().Count());
        Assert.Equal(1, ((IntLangValue)array.GetItems().ElementAt(0)).Value);
        Assert.Equal(2, ((IntLangValue)array.GetItems().ElementAt(1)).Value);
        Assert.Equal(3, ((IntLangValue)array.GetItems().ElementAt(2)).Value);
        Assert.Equal(5, ((IntLangValue)array.GetItems().ElementAt(3)).Value);
        Assert.Equal(8, ((IntLangValue)array.GetItems().ElementAt(4)).Value);
        Assert.Equal(9, ((IntLangValue)array.GetItems().ElementAt(5)).Value);
    }

    [Fact]
    public void CollectionMethods_ArrayFilter_FiltersArrayElements()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            evenNumbers <- numbers.Filter((x:int) -> x % 2 == 0)
            result <- evenNumbers
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
        Assert.Equal(5, array.GetItems().Count());
        Assert.Equal(2, ((IntLangValue)array.GetItems().ElementAt(0)).Value);
        Assert.Equal(4, ((IntLangValue)array.GetItems().ElementAt(1)).Value);
        Assert.Equal(6, ((IntLangValue)array.GetItems().ElementAt(2)).Value);
        Assert.Equal(8, ((IntLangValue)array.GetItems().ElementAt(3)).Value);
        Assert.Equal(10, ((IntLangValue)array.GetItems().ElementAt(4)).Value);
    }

    [Fact]
    public void CollectionMethods_ArrayMap_TransformsArrayElements()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            squares <- numbers.Map((x:int) -> x * x)
            result <- squares
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
        Assert.Equal(5, array.GetItems().Count());
        Assert.Equal(1, ((IntLangValue)array.GetItems().ElementAt(0)).Value);
        Assert.Equal(4, ((IntLangValue)array.GetItems().ElementAt(1)).Value);
        Assert.Equal(9, ((IntLangValue)array.GetItems().ElementAt(2)).Value);
        Assert.Equal(16, ((IntLangValue)array.GetItems().ElementAt(3)).Value);
        Assert.Equal(25, ((IntLangValue)array.GetItems().ElementAt(4)).Value);
    }

    [Fact]
    public void CollectionMethods_ArrayReduce_ReducesArrayToSingleValue()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            sum <- numbers.Reduce((acc:int, x:int) -> acc + x, 0)
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
    public void CollectionMethods_TupleJoin_JoinsTupleElements()
    {
        // Arrange
        var code = @"
            parts <- (""Hello"", "" "", ""World"", ""!"")
            result <- parts.Join("""")
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
    public void CollectionMethods_StringSplit_SplitsStringIntoList()
    {
        // Arrange
        var code = @"
            text <- ""apple,banana,cherry,date""
            fruits <- text.Split("","")
            result <- fruits
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
        Assert.Equal(4, list.Values.Count);
        Assert.Equal("apple", ((StringLangValue)list.Values[0]).Value);
        Assert.Equal("banana", ((StringLangValue)list.Values[1]).Value);
        Assert.Equal("cherry", ((StringLangValue)list.Values[2]).Value);
        Assert.Equal("date", ((StringLangValue)list.Values[3]).Value);
    }

    [Fact]
    public void CollectionMethods_ListForEach_PerformsActionOnEachElement()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            sum <- 0
            numbers.ForEach((x:int) -> {
                sum <- sum + x
            })
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
    public void CollectionMethods_DictionaryForEach_PerformsActionOnEachItem()
    {
        // Arrange
        var code = @"
            data <- {""a"": 1, ""b"": 2, ""c"": 3}
            keys <- """"
            values <- """"
            data.ForEach((key:string, value:int) -> {
                keys <- keys + key
                values <- values + value.ToStr()
            })
            result <- ""keys: "" + keys + "", values: "" + values
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
        Assert.Contains("keys:", resultString);
        Assert.Contains("values:", resultString);
    }

    [Fact]
    public void CollectionMethods_ListContains_ChecksElementExists()
    {
        // Arrange
        var code = @"
            items <- {""apple"", ""banana"", ""cherry""}
            hasApple <- items.Contains(""apple"")
            hasGrape <- items.Contains(""grape"")
            result <- ""apple: "" + hasApple.ToStr() + "", grape: "" + hasGrape.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("apple: true, grape: false", ((StringLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_ListFind_FindsFirstMatchingElement()
    {
        // Arrange
        var code = @"
            numbers <- {10, 25, 30, 45, 60}
            found <- numbers.Find((x:int) -> x > 30)
            result <- found
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(45, ((IntLangValue)result).Value); // First element > 30
    }

    [Fact]
    public void CollectionMethods_ListAny_ChecksAnyElementSatisfiesCondition()
    {
        // Arrange
        var code = @"
            numbers <- {1, 3, 5, 7, 9}
            hasEven <- numbers.Any((x:int) -> x % 2 == 0)
            hasGreaterThan8 <- numbers.Any((x:int) -> x > 8)
            result <- ""even: "" + hasEven.ToStr() + "", >8: "" + hasGreaterThan8.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("even: false, >8: true", ((StringLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_ListAll_ChecksAllElementsSatisfyCondition()
    {
        // Arrange
        var code = @"
            numbers <- {2, 4, 6, 8, 10}
            allEven <- numbers.All((x:int) -> x % 2 == 0)
            allLessThan10 <- numbers.All((x:int) -> x < 10)
            result <- ""all even: "" + allEven.ToStr() + "", all <10: "" + allLessThan10.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("all even: true, all <10: false", ((StringLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_ListAggregate_AggregatesListElements()
    {
        // Arrange
        var code = @"
            words <- {""Hello"", "", "", ""World"", ""!""}
            sentence <- words.Aggregate((acc:string, word:string) -> acc + word, """")
            result <- sentence
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
    public void CollectionMethods_DictionaryUpdate_UpdatesDictionaryValue()
    {
        // Arrange
        var code = @"
            settings <- {""volume"": 50, ""brightness"": 70}
            settings.Update(""volume"", 80)
            updatedVolume <- settings[""volume""]
            result <- updatedVolume
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(80, ((IntLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_DictionaryMerge_MergesTwoDictionaries()
    {
        // Arrange
        var code = @"
            dict1 <- {""a"": 1, ""b"": 2}
            dict2 <- {""c"": 3, ""d"": 4}
            merged <- dict1.Merge(dict2)
            resultCount <- merged.Count
            aValue <- merged[""a""]
            dValue <- merged[""d""]
            result <- ""count: "" + resultCount.ToStr() + "", a: "" + aValue.ToStr() + "", d: "" + dValue.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("count: 4, a: 1, d: 4", ((StringLangValue)result).Value);
    }

    [Fact]
    public void CollectionMethods_ArrayDistinct_RemovesDuplicates()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 2, 3, 4, 4, 4, 5]
            uniqueNumbers <- numbers.Distinct()
            result <- uniqueNumbers
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
        Assert.Equal(5, array.GetItems().Count());
    }

    [Fact]
    public void CollectionMethods_ListSkip_SipsFirstNElements()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            skipped <- numbers.Skip(5)
            result <- skipped
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
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(6, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(7, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(9, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(10, ((IntLangValue)list.Values[4]).Value);
    }

    [Fact]
    public void CollectionMethods_ListTake_TakesFirstNElements()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            taken <- numbers.Take(4)
            result <- taken
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
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[3]).Value);
    }

    [Fact]
    public void CollectionMethods_ListConcat_ConcatenatesLists()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {4, 5, 6}
            combined <- list1.Concat(list2)
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
        Assert.Equal(6, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[4]).Value);
        Assert.Equal(6, ((IntLangValue)list.Values[5]).Value);
    }
}