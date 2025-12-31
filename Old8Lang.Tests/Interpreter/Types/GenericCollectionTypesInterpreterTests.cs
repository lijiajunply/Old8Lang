using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Types;

/// <summary>
/// 泛型集合类型解释模式测试
/// 测试 list&lt;T&gt;, array&lt;T&gt;, dict&lt;K,V&gt; 在解释器模式下的运行时行为
/// </summary>
[Collection("Sequential")]
public class GenericCollectionTypesInterpreterTests
{
    #region 基本泛型列表运行时测试

    /// <summary>
    /// 测试基本泛型列表 - list&lt;int&gt;
    /// </summary>
    [Fact]
    public void Run_BasicGenericList_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
items:list<int> <- {1, 2, 3, 4, 5}
result <- items[0]
count <- len(items)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var count = interpreter.Manager.GetValue(new LangId("count"));

        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);

        Assert.NotNull(count);
        Assert.IsType<IntLangValue>(count);
        Assert.Equal(5, ((IntLangValue)count).Value);
    }

    /// <summary>
    /// 测试多种类型的泛型列表
    /// </summary>
    [Fact]
    public void Run_GenericListWithVariousTypes_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
intList:list<int> <- {1, 2, 3}
stringList:list<string> <- {""a"", ""b"", ""c""}
doubleList:list<double> <- {1.5, 2.5, 3.5}
boolList:list<bool> <- {true, false, true}
result <- len(intList) + len(stringList) + len(doubleList) + len(boolList)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(12, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试空泛型列表
    /// </summary>
    [Fact]
    public void Run_EmptyGenericList_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
empty:list<int> <- {}
result <- len(empty)
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

    #endregion

    #region 基本泛型数组运行时测试

    /// <summary>
    /// 测试基本泛型数组 - array&lt;int&gt;
    /// </summary>
    [Fact]
    public void Run_BasicGenericArray_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
arr:array<int> <- [1, 2, 3, 4, 5]
result <- arr[0]
count <- arr.Count()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var count = interpreter.Manager.GetValue(new LangId("count"));

        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);

        Assert.NotNull(count);
        Assert.IsType<IntLangValue>(count);
        Assert.Equal(5, ((IntLangValue)count).Value);
    }

    /// <summary>
    /// 测试数组 Length() 方法
    /// </summary>
    [Fact]
    public void Run_ArrayLengthMethod_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
arr:array<int> <- [1, 2, 3]
lengthResult <- arr.Length()
countResult <- arr.Count()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var lengthResult = interpreter.Manager.GetValue(new LangId("lengthResult"));
        var countResult = interpreter.Manager.GetValue(new LangId("countResult"));

        Assert.NotNull(lengthResult);
        Assert.IsType<IntLangValue>(lengthResult);
        Assert.Equal(3, ((IntLangValue)lengthResult).Value);

        Assert.NotNull(countResult);
        Assert.IsType<IntLangValue>(countResult);
        Assert.Equal(3, ((IntLangValue)countResult).Value);
    }

    /// <summary>
    /// 测试多种类型的泛型数组
    /// </summary>
    [Fact]
    public void Run_GenericArrayWithVariousTypes_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
intArray:array<int> <- [1, 2, 3]
stringArray:array<string> <- [""Alice"", ""Bob""]
result <- intArray[0] + len(stringArray)
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

    #endregion

    #region 基本泛型字典运行时测试

    /// <summary>
    /// 测试基本泛型字典 - dict&lt;string, int&gt;
    /// </summary>
    [Fact]
    public void Run_BasicGenericDictionary_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
ages:dict<string, int> <- {""Alice"": 30, ""Bob"": 25}
aliceAge <- ages[""Alice""]
bobAge <- ages[""Bob""]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var aliceAge = interpreter.Manager.GetValue(new LangId("aliceAge"));
        var bobAge = interpreter.Manager.GetValue(new LangId("bobAge"));

        Assert.NotNull(aliceAge);
        Assert.IsType<IntLangValue>(aliceAge);
        Assert.Equal(30, ((IntLangValue)aliceAge).Value);

        Assert.NotNull(bobAge);
        Assert.IsType<IntLangValue>(bobAge);
        Assert.Equal(25, ((IntLangValue)bobAge).Value);
    }

    /// <summary>
    /// 测试多种类型组合的泛型字典
    /// </summary>
    [Fact]
    public void Run_GenericDictionaryWithVariousTypes_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
stringIntDict:dict<string, int> <- {""a"": 1, ""b"": 2}
intStringDict:dict<int, string> <- {1: ""one"", 2: ""two""}
result <- stringIntDict[""a""] + len(intStringDict)
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

    #endregion

    #region 嵌套泛型类型运行时测试

    /// <summary>
    /// 测试嵌套泛型列表 - list&lt;list&lt;int&gt;&gt;
    /// </summary>
    [Fact]
    public void Run_NestedGenericList_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
matrix:list<list<int>> <- {{1, 2}, {3, 4}, {5, 6}}
firstRow <- matrix[0]
firstElement <- matrix[0][0]
secondRowFirstElement <- matrix[1][0]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var firstElement = interpreter.Manager.GetValue(new LangId("firstElement"));
        var secondRowFirstElement = interpreter.Manager.GetValue(new LangId("secondRowFirstElement"));

        Assert.NotNull(firstElement);
        Assert.IsType<IntLangValue>(firstElement);
        Assert.Equal(1, ((IntLangValue)firstElement).Value);

        Assert.NotNull(secondRowFirstElement);
        Assert.IsType<IntLangValue>(secondRowFirstElement);
        Assert.Equal(3, ((IntLangValue)secondRowFirstElement).Value);
    }

    /// <summary>
    /// 测试字典值为列表 - dict&lt;string, list&lt;int&gt;&gt;
    /// </summary>
    [Fact]
    public void Run_DictionaryWithListValue_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
groups:dict<string, list<int>> <- {
    ""even"": {2, 4, 6},
    ""odd"": {1, 3, 5}
}
evenFirst <- groups[""even""][0]
oddSecond <- groups[""odd""][1]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var evenFirst = interpreter.Manager.GetValue(new LangId("evenFirst"));
        var oddSecond = interpreter.Manager.GetValue(new LangId("oddSecond"));

        Assert.NotNull(evenFirst);
        Assert.IsType<IntLangValue>(evenFirst);
        Assert.Equal(2, ((IntLangValue)evenFirst).Value);

        Assert.NotNull(oddSecond);
        Assert.IsType<IntLangValue>(oddSecond);
        Assert.Equal(3, ((IntLangValue)oddSecond).Value);
    }

    /// <summary>
    /// 测试列表元素为数组 - list&lt;array&lt;int&gt;&gt;
    /// </summary>
    [Fact]
    public void Run_ListWithArrayElement_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
arrays:list<array<int>> <- {[1, 2], [3, 4], [5, 6]}
firstArray <- arrays[0]
firstElement <- arrays[0][0]
secondArraySecondElement <- arrays[1][1]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var firstElement = interpreter.Manager.GetValue(new LangId("firstElement"));
        var secondArraySecondElement = interpreter.Manager.GetValue(new LangId("secondArraySecondElement"));

        Assert.NotNull(firstElement);
        Assert.IsType<IntLangValue>(firstElement);
        Assert.Equal(1, ((IntLangValue)firstElement).Value);

        Assert.NotNull(secondArraySecondElement);
        Assert.IsType<IntLangValue>(secondArraySecondElement);
        Assert.Equal(4, ((IntLangValue)secondArraySecondElement).Value);
    }

    #endregion

    #region 向后兼容性测试

    /// <summary>
    /// 测试混合类型集合（向后兼容）
    /// 解释器模式下，类型注解不强制类型检查
    /// </summary>
    [Fact]
    public void Run_MixedTypeCollections_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
// 不带类型注解的混合类型列表（传统方式）
mixed <- {1, ""hello"", 3.14, true}
result1 <- len(mixed)

// 带类型注解的混合类型列表（解释器模式下也允许）
typedMixed:list<int> <- {1, ""world"", 2, false}
result2 <- len(typedMixed)

// 混合类型数组
arr <- [1, ""test"", true]
result3 <- arr.Count()
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
        Assert.Equal(4, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.Equal(4, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value);
    }

    /// <summary>
    /// 测试不带类型注解的集合（完全向后兼容）
    /// </summary>
    [Fact]
    public void Run_NonTypedCollections_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
// 传统列表语法
items <- {1, 2, 3}
result1 <- items[1]

// 传统数组语法
arr <- [10, 20, 30]
result2 <- arr[2]

// 传统字典语法
dict <- {""key"": ""value"", ""num"": 123}
result3 <- dict[""num""]
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
        Assert.Equal(2, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.Equal(30, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.Equal(123, ((IntLangValue)result3).Value);
    }

    #endregion

    #region 函数中的泛型集合类型

    /// <summary>
    /// 测试函数参数中的泛型集合类型
    /// </summary>
    [Fact]
    public void Run_GenericCollectionInFunctionParameter_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func process(items:list<int>) -> int {
    return len(items)
}

myList:list<int> <- {1, 2, 3}
result <- process(myList)
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

    /// <summary>
    /// 测试函数返回值中的泛型集合类型
    /// </summary>
    [Fact]
    public void Run_GenericCollectionInFunctionReturnType_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func getNumbers() -> list<int> {
    return {1, 2, 3}
}

result <- getNumbers()
count <- len(result)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var count = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(count);
        Assert.IsType<IntLangValue>(count);
        Assert.Equal(3, ((IntLangValue)count).Value);
    }

    #endregion

    #region 类中的泛型集合类型

    /// <summary>
    /// 测试类字段中的泛型集合类型
    /// </summary>
    [Fact]
    public void Run_GenericCollectionInClassField_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
class Container {
    private items:list<int>

    func setItems(newItems:list<int>) -> void {
        this.items <- newItems
    }

    func getCount() -> int {
        return len(this.items)
    }
}

container <- Container()
container.setItems({1, 2, 3, 4, 5})
result <- container.getCount()
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

    #endregion
}
