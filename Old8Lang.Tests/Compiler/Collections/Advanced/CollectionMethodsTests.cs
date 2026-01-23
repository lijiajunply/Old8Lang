using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

/// <summary>
/// 编译器模式下的高级集合功能测试 - 集合方法
/// </summary>
public class CollectionMethodsTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void ListMapMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            doubled <- numbers.Map((x:int) -> x * 2)
            
            Assert.Equal(5, doubled.Count())
            Assert.Equal({2, 4, 6, 8, 10}, doubled)
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
    public void ListFilterMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            evens <- numbers.Filter((x:int) -> x % 2 == 0)
            
            Assert.Equal(5, evens.Count())
            Assert.Equal({2, 4, 6, 8, 10}, evens)
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
    public void ListReduceMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            sum <- numbers.Reduce(0, (acc:int, x:int) -> acc + x)
            product <- numbers.Reduce(1, (acc:int, x:int) -> acc * x)
            
            Assert.Equal(15, sum)      // 0 + 1 + 2 + 3 + 4 + 5 = 15
            Assert.Equal(120, product) // 1 * 1 * 2 * 3 * 4 * 5 = 120
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
    public void ListFindMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {10, 20, 30, 40, 50}
            
            firstEven <- numbers.Find((x:int) -> x % 2 == 0)
            firstGreater <- numbers.Find((x:int) -> x > 25)
            firstSmall <- numbers.Find((x:int) -> x < 5)
            
            Assert.Equal(10, firstEven)    // 第一个偶数
            Assert.Equal(30, firstGreater) // 第一个大于25的
            Assert.Equal(null, firstSmall)  // 没有小于5的
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
    public void ListEveryMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            evens <- {2, 4, 6, 8, 10}
            mixed <- {1, 2, 3, 4, 5}
            odds <- {1, 3, 5, 7, 9}
            
            allEvens <- evens.Every((x:int) -> x % 2 == 0)  // 全是偶数
            allMixed <- mixed.Every((x:int) -> x % 2 == 0)  // 不全是偶数
            allOdds <- odds.Every((x:int) -> x % 2 != 0)    // 全是奇数
            
            Assert.Equal(true, allEvens)
            Assert.Equal(false, allMixed)
            Assert.Equal(true, allOdds)
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
    public void ListSomeMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            evens <- {2, 4, 6, 8, 10}
            mixed <- {1, 2, 3, 4, 5}
            odds <- {1, 3, 5, 7, 9}
            
            someEvens <- evens.Some((x:int) -> x % 2 == 0)  // 有偶数
            someMixed <- mixed.Some((x:int) -> x % 2 == 0)  // 有偶数
            someOdds <- odds.Some((x:int) -> x % 2 == 0)    // 没有偶数
            
            Assert.Equal(true, someEvens)
            Assert.Equal(true, someMixed)
            Assert.Equal(false, someOdds)
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
    public void ListJoinMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            fruits <- {""apple"", ""banana"", ""cherry""}
            numbers <- {1, 2, 3, 4, 5}
            empty <- {}
            
            joinedFruits <- fruits.Join("", "")
            joinedNumbers <- numbers.Join("", "", "")
            joinedWithSeparator <- fruits.Join("", "", "")
            emptyJoin <- empty.Join("", "")
            
            Assert.Equal(""applebananacherry"", joinedFruits)
            Assert.Equal(""1, 2, 3, 4, 5"", joinedNumbers)
            Assert.Equal(""apple, banana, cherry"", joinedWithSeparator)
            Assert.Equal("""", emptyJoin)
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
    public void ListSortMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            unsorted <- {5, 2, 8, 1, 9, 3, 7, 4, 6}
            strings <- {""z"", ""a"", ""x"", ""b"", ""y"", ""c""}
            
            sortedNumbers <- unsorted.Sort()
            sortedStrings <- strings.Sort()
            
            Assert.Equal({1, 2, 3, 4, 5, 6, 7, 8, 9}, sortedNumbers)
            Assert.Equal({""a"", ""b"", ""c"", ""x"", ""y"", ""z""}, sortedStrings)
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
    public void ListReverseMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            original <- {1, 2, 3, 4, 5}
            reversed <- original.Reverse()
            
            Assert.Equal(5, reversed.Count())
            Assert.Equal({5, 4, 3, 2, 1}, reversed)
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
    public void DictionaryKeysMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            scores <- {""Alice"": 95, ""Bob"": 87, ""Charlie"": 92}
            keys <- scores.Keys()
            
            // 检查所有键都存在
            hasAlice <- keys.Contains(""Alice"")
            hasBob <- keys.Contains(""Bob"")
            hasCharlie <- keys.Contains(""Charlie"")
            hasDave <- keys.Contains(""Dave"")
            
            Assert.Equal(3, keys.Count())
            Assert.Equal(true, hasAlice)
            Assert.Equal(true, hasBob)
            Assert.Equal(true, hasCharlie)
            Assert.Equal(false, hasDave)
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
    public void DictionaryValuesMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            scores <- {""Alice"": 95, ""Bob"": 87, ""Charlie"": 92}
            values <- scores.Values()
            
            // 检查所有值都存在
            sum <- 0
            i <- 0
            while i < values.Count() {
                sum <- sum + values[i]
                i <- i + 1
            }
            
            Assert.Equal(3, values.Count())
            Assert.Equal(274, sum)  // 95 + 87 + 92 = 274
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
    public void ArrayMapMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            squared <- numbers.Map((x:int) -> x * x)
            
            Assert.Equal(5, squared.Length)
            Assert.Equal([1, 4, 9, 16, 25], squared)
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
    public void ArrayFilterMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            evens <- numbers.Filter((x:int) -> x % 2 == 0)
            
            Assert.Equal(5, evens.Length)
            Assert.Equal([2, 4, 6, 8, 10], evens)
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
    public void ArraySortMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            unsorted <- [5, 2, 8, 1, 9, 3, 7, 4, 6]
            sorted <- unsorted.Sort()
            
            Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], sorted)
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
    public void ComplexCollectionOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 复杂的数据处理流水线
            rawData <- {5, 12, 8, 15, 3, 20, 7, 18, 2, 10}
            
            // 1. 过滤出大于等于5的数字
            filtered <- rawData.Filter((x:int) -> x >= 5)  // {5, 12, 8, 15, 20, 7, 18, 10}
            
            // 2. 将每个数字乘以2
            doubled <- filtered.Map((x:int) -> x * 2)  // {10, 24, 16, 30, 40, 14, 36, 20}
            
            // 3. 按升序排序
            sorted <- doubled.Sort()  // {10, 14, 16, 20, 24, 30, 36, 40}
            
            // 4. 计算总和
            total <- sorted.Reduce(0, (acc:int, x:int) -> acc + x)
            
            Assert.Equal(8, filtered.Count())
            Assert.Equal(8, doubled.Count())
            Assert.Equal(8, sorted.Length)
            Assert.Equal(190, total)  // 10+14+16+20+24+30+36+40 = 190
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
    public void CollectionChaining_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 链式调用
            result <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
                      .Filter((x:int) -> x % 2 == 0)
                      .Map((x:int) -> x * x)
                      .Sort()
            
            // 过滤偶数: {2, 4, 6, 8, 10}
            // 平方: {4, 16, 36, 64, 100}
            // 排序: {4, 16, 36, 64, 100} (已经有序)
            
            Assert.Equal({4, 16, 36, 64, 100}, result)
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
    public void CollectionWithCustomComparator_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 自定义排序：按字符串长度排序
            strings <- {""apple"", ""banana"", ""kiwi"", ""cherry"", ""fig""}
            
            // 假设有比较器函数
            sortedByLength <- strings.Sort((a:string, b:string) -> a.Length() - b.Length())
            
            // 长度：fig(3), kiwi(4), apple(5), cherry(6), banana(6)
            // 长度相同的保持原序
            
            Assert.Equal(""fig"", sortedByLength[0])
            Assert.Equal(""kiwi"", sortedByLength[1])
            Assert.Equal(""apple"", sortedByLength[2])
            Assert.Equal(5, sortedByLength.Count())
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
    public void CollectionWithNestedData_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 嵌套数据处理
            people <- {
                {""name"": ""Alice"", ""age"": 30, ""scores"": [85, 92, 88]},
                {""name"": ""Bob"", ""age"": 25, ""scores"": [90, 87, 93]},
                {""name"": ""Charlie"", ""age"": 35, ""scores"": [78, 95, 82]}
            }
            
            // 获取每个人平均分大于85的人
            qualifiedPeople <- people.Filter((person:dict) -> {
                scores <- person[""scores""]
                sum <- scores.Reduce(0, (acc:int, x:int) -> acc + x)
                avg <- sum / scores.Length
                return avg > 85
            })
            
            // 提取名字
            names <- qualifiedPeople.Map((person:dict) -> person[""name""])
            
            // Alice: (85+92+88)/3 = 88.33 > 85
            // Bob: (90+87+93)/3 = 90 > 85  
            // Charlie: (78+95+82)/3 = 85 <= 85
            
            Assert.Equal(2, qualifiedPeople.Count())
            Assert.Equal({""Alice"", ""Bob""}, names)
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
    public void CollectionErrorHandling_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 空集合的安全操作
            emptyList <- {}
            emptyDict <- {}
            
            // 空集合的 Reduce 应该使用初始值
            emptySum <- emptyList.Reduce(0, (acc:int, x:int) -> acc + x)
            
            // 空集合的 Find 应该返回 null
            notFound <- emptyList.Find((x:int) -> x > 0)
            
            // 空集合的 Every 应该返回 true
            allEmpty <- emptyList.Every((x:int) -> false)  // 没有元素，所以 true
            
            // 空集合的 Some 应该返回 false
            someEmpty <- emptyList.Some((x:int) -> true)  // 没有元素，所以 false
            
            Assert.Equal(0, emptySum)
            Assert.Equal(null, notFound)
            Assert.Equal(true, allEmpty)
            Assert.Equal(false, someEmpty)
            
            // 空字典的操作
            emptyKeys <- emptyDict.Keys()
            emptyValues <- emptyDict.Values()
            
            Assert.Equal(0, emptyKeys.Count())
            Assert.Equal(0, emptyValues.Count())
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