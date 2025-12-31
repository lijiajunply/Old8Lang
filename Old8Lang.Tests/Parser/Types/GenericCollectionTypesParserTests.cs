using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Types;

/// <summary>
/// 泛型集合类型解析测试
/// 测试 list<T>, array<T>, dict<K,V> 的语法解析
/// </summary>
[Collection("Sequential")]
public class GenericCollectionTypesParserTests
{
    #region 基本泛型列表解析

    /// <summary>
    /// 测试基本泛型列表语法 - list<int>
    /// </summary>
    [Fact]
    public void ParseProgram_BasicGenericList_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
items:list<int> <- {1, 2, 3, 4, 5}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多种类型的泛型列表
    /// </summary>
    [Fact]
    public void ParseProgram_GenericListWithVariousTypes_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
intList:list<int> <- {1, 2, 3}
stringList:list<string> <- {""a"", ""b"", ""c""}
doubleList:list<double> <- {1.5, 2.5, 3.5}
boolList:list<bool> <- {true, false, true}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试空泛型列表
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyGenericList_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
empty:list<int> <- {}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 基本泛型数组解析

    /// <summary>
    /// 测试基本泛型数组语法 - array<int>
    /// </summary>
    [Fact]
    public void ParseProgram_BasicGenericArray_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
arr:array<int> <- [1, 2, 3, 4, 5]
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多种类型的泛型数组
    /// </summary>
    [Fact]
    public void ParseProgram_GenericArrayWithVariousTypes_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
intArray:array<int> <- [1, 2, 3]
stringArray:array<string> <- [""Alice"", ""Bob""]
doubleArray:array<double> <- [1.5, 2.5]
boolArray:array<bool> <- [true, false]
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试空泛型数组
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyGenericArray_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
empty:array<string> <- []
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 基本泛型字典解析

    /// <summary>
    /// 测试基本泛型字典语法 - dict<string, int>
    /// </summary>
    [Fact]
    public void ParseProgram_BasicGenericDictionary_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ages:dict<string, int> <- {""Alice"": 30, ""Bob"": 25}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多种类型组合的泛型字典
    /// </summary>
    [Fact]
    public void ParseProgram_GenericDictionaryWithVariousTypes_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
stringIntDict:dict<string, int> <- {""a"": 1, ""b"": 2}
intStringDict:dict<int, string> <- {1: ""one"", 2: ""two""}
stringDoubleDict:dict<string, double> <- {""pi"": 3.14, ""e"": 2.71}
stringBoolDict:dict<string, bool> <- {""enabled"": true, ""debug"": false}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 嵌套泛型类型解析

    /// <summary>
    /// 测试嵌套泛型列表 - list<list<int>>
    /// </summary>
    [Fact]
    public void ParseProgram_NestedGenericList_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
matrix:list<list<int>> <- {{1, 2}, {3, 4}, {5, 6}}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试字典值为列表 - dict<string, list<int>>
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryWithListValue_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
groups:dict<string, list<int>> <- {
    ""even"": {2, 4, 6},
    ""odd"": {1, 3, 5}
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试列表元素为数组 - list<array<int>>
    /// </summary>
    [Fact]
    public void ParseProgram_ListWithArrayElement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
arrays:list<array<int>> <- {[1, 2], [3, 4], [5, 6]}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试三层嵌套泛型 - list<list<list<int>>>
    /// </summary>
    [Fact]
    public void ParseProgram_TripleNestedGenericList_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
cube:list<list<list<int>>> <- {
    {{1, 2}, {3, 4}},
    {{5, 6}, {7, 8}}
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 函数中的泛型集合类型

    /// <summary>
    /// 测试函数参数中的泛型集合类型
    /// </summary>
    [Fact]
    public void ParseProgram_GenericCollectionInFunctionParameter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func process(items:list<int>) -> int {
    return items.Count
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数返回值中的泛型集合类型
    /// </summary>
    [Fact]
    public void ParseProgram_GenericCollectionInFunctionReturnType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getNumbers() -> list<int> {
    return {1, 2, 3}
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数参数和返回值都使用泛型集合类型
    /// </summary>
    [Fact]
    public void ParseProgram_GenericCollectionInFunctionSignature_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func transform(input:list<int>) -> list<string> {
    result:list<string> <- {}
    for num in input {
        result.Add(num.ToStr())
    }
    return result
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 类中的泛型集合类型

    /// <summary>
    /// 测试类字段中的泛型集合类型
    /// </summary>
    [Fact]
    public void ParseProgram_GenericCollectionInClassField_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Container {
    private items:list<int>
    public names:list<string>
    protected data:dict<string, int>
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试类方法中的泛型集合类型
    /// </summary>
    [Fact]
    public void ParseProgram_GenericCollectionInClassMethod_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class DataProcessor {
    func processItems(items:list<int>) -> list<int> {
        result:list<int> <- {}
        for item in items {
            result.Add(item * 2)
        }
        return result
    }
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 混合类型注解

    /// <summary>
    /// 测试泛型集合与可空类型组合
    /// </summary>
    [Fact]
    public void ParseProgram_GenericCollectionWithNullable_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
nullableList:list<int>? <- null
items:list<int>? <- {1, 2, 3}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试泛型集合与联合类型组合（如果支持）
    /// </summary>
    [Fact]
    public void ParseProgram_GenericCollectionWithUnionType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
mixed:list<int | string> <- {1, ""hello"", 2}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 自定义类型泛型测试

    /// <summary>
    /// 测试自定义类型的泛型列表
    /// </summary>
    [Fact]
    public void ParseProgram_GenericListWithCustomType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Person {
    public name:string
    public age:int
}

people:list<Person> <- {}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试自定义类型的泛型数组
    /// </summary>
    [Fact]
    public void ParseProgram_GenericArrayWithCustomType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class User {
    public id:int
    public email:string
}

users:array<User> <- []
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试自定义类型的泛型字典
    /// </summary>
    [Fact]
    public void ParseProgram_GenericDictionaryWithCustomType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Product {
    public name:string
    public price:double
}

products:dict<string, Product> <- {}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套的自定义类型泛型
    /// </summary>
    [Fact]
    public void ParseProgram_NestedGenericWithCustomType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Item {
    public id:int
    public value:string
}

groups:dict<string, list<Item>> <- {}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数参数中的自定义类型泛型
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionWithCustomTypeGeneric_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Student {
    public name:string
    public score:int
}

func processStudents(students:list<Student>) -> int {
    return students.Count()
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}
