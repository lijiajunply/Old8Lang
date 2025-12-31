using Old8Lang.Interpreter;
using Old8Lang.Error;

namespace Old8Lang.Tests.Compiler.Types;

/// <summary>
/// 泛型集合类型编译模式测试
/// 测试 list&lt;T&gt;, array&lt;T&gt;, dict&lt;K,V&gt; 在编译器模式下的类型检查和 IL 生成
/// </summary>
[Collection("Sequential")]
public class GenericCollectionTypesCompilerTests
{
    #region 基本泛型列表编译测试

    /// <summary>
    /// 测试基本泛型列表 - list&lt;int&gt;
    /// </summary>
    [Fact]
    public void Compile_BasicGenericList_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    items:list<int> <- {1, 2, 3, 4, 5}
    return items[0]
}

Assert.True(test() == 1)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试多种类型的泛型列表
    /// </summary>
    [Fact]
    public void Compile_GenericListWithVariousTypes_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    intList:list<int> <- {1, 2, 3}
    stringList:list<string> <- {""a"", ""b"", ""c""}
    doubleList:list<double> <- {1.5, 2.5, 3.5}
    boolList:list<bool> <- {true, false, true}
    return intList.Count() + stringList.Count()
}

Assert.True(test() == 6)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试空泛型列表
    /// </summary>
    [Fact]
    public void Compile_EmptyGenericList_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    empty:list<int> <- {}
    return empty.Count()
}

Assert.True(test() == 0)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 基本泛型数组编译测试

    /// <summary>
    /// 测试基本泛型数组 - array&lt;int&gt;
    /// </summary>
    [Fact]
    public void Compile_BasicGenericArray_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    arr:array<int> <- [1, 2, 3, 4, 5]
    return arr[0]
}

Assert.True(test() == 1)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试数组 Length 属性（编译器模式）
    /// </summary>
    [Fact]
    public void Compile_ArrayLengthProperty_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    arr:array<int> <- [1, 2, 3]
    return arr.Length
}

Assert.True(test() == 3)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试多种类型的泛型数组
    /// </summary>
    [Fact]
    public void Compile_GenericArrayWithVariousTypes_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    intArray:array<int> <- [1, 2, 3]
    stringArray:array<string> <- [""Alice"", ""Bob""]
    return intArray[0] + stringArray.Length
}

Assert.True(test() == 3)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 基本泛型字典编译测试

    /// <summary>
    /// 测试基本泛型字典 - dict&lt;string, int&gt;
    /// </summary>
    [Fact]
    public void Compile_BasicGenericDictionary_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    ages:dict<string, int> <- {""Alice"": 30, ""Bob"": 25}
    return ages[""Alice""]
}

Assert.True(test() == 30)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试多种类型组合的泛型字典
    /// </summary>
    [Fact]
    public void Compile_GenericDictionaryWithVariousTypes_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    stringIntDict:dict<string, int> <- {""a"": 1, ""b"": 2}
    intStringDict:dict<int, string> <- {1: ""one"", 2: ""two""}
    return stringIntDict[""a""] + intStringDict.Count()
}

Assert.True(test() == 3)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 嵌套泛型类型编译测试

    /// <summary>
    /// 测试嵌套泛型列表 - list&lt;list&lt;int&gt;&gt;
    /// </summary>
    [Fact]
    public void Compile_NestedGenericList_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    matrix:list<list<int>> <- {{1, 2}, {3, 4}, {5, 6}}
    return matrix[0][0] + matrix[1][0]
}

Assert.True(test() == 4)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试字典值为列表 - dict&lt;string, list&lt;int&gt;&gt;
    /// </summary>
    [Fact]
    public void Compile_DictionaryWithListValue_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    groups:dict<string, list<int>> <- {
        ""even"": {2, 4, 6},
        ""odd"": {1, 3, 5}
    }
    return groups[""even""][0] + groups[""odd""][1]
}

Assert.True(test() == 5)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试列表元素为数组 - list&lt;array&lt;int&gt;&gt;
    /// </summary>
    [Fact]
    public void Compile_ListWithArrayElement_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    arrays:list<array<int>> <- {[1, 2], [3, 4], [5, 6]}
    return arrays[0][0] + arrays[1][1]
}

Assert.True(test() == 5)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型错误检测测试

    /// <summary>
    /// 测试列表类型不匹配错误
    /// </summary>
    [Fact]
    public void Compile_ListTypeMismatch_ThrowsCompilerException()
    {
        // Arrange
        var code = @"
func test() -> int {
    items:list<int> <- {1, ""hello"", 3}
    return items[0]
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("列表元素类型不匹配", exception.Message);
        Assert.Contains("期望类型 int", exception.Message);
        Assert.Contains("实际类型 string", exception.Message);
    }

    /// <summary>
    /// 测试数组类型不匹配错误
    /// </summary>
    [Fact]
    public void Compile_ArrayTypeMismatch_ThrowsCompilerException()
    {
        // Arrange
        var code = @"
func test() -> int {
    arr:array<string> <- [1, 2, 3]
    return arr.Length
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("数组元素类型不匹配", exception.Message);
        Assert.Contains("期望类型 string", exception.Message);
        Assert.Contains("实际类型 int", exception.Message);
    }

    /// <summary>
    /// 测试字典键类型不匹配错误
    /// </summary>
    [Fact]
    public void Compile_DictionaryKeyTypeMismatch_ThrowsCompilerException()
    {
        // Arrange
        var code = @"
func test() -> int {
    ages:dict<string, int> <- {123: 30, ""Bob"": 25}
    return ages[""Bob""]
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("字典键类型不匹配", exception.Message);
        Assert.Contains("期望类型 string", exception.Message);
        Assert.Contains("实际类型 int", exception.Message);
    }

    /// <summary>
    /// 测试字典值类型不匹配错误
    /// </summary>
    [Fact]
    public void Compile_DictionaryValueTypeMismatch_ThrowsCompilerException()
    {
        // Arrange
        var code = @"
func test() -> int {
    ages:dict<string, int> <- {""Alice"": 30, ""Bob"": ""twenty-five""}
    return ages[""Alice""]
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("字典值类型不匹配", exception.Message);
        Assert.Contains("期望类型 int", exception.Message);
        Assert.Contains("实际类型 string", exception.Message);
    }

    /// <summary>
    /// 测试嵌套泛型类型不匹配错误
    /// </summary>
    [Fact]
    public void Compile_NestedGenericTypeMismatch_ThrowsCompilerException()
    {
        // Arrange
        var code = @"
func test() -> int {
    matrix:list<list<int>> <- {{1, 2}, {""a"", ""b""}, {5, 6}}
    return matrix[0][0]
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("类型不匹配", exception.Message);
    }

    #endregion

    #region 函数中的泛型集合类型

    /// <summary>
    /// 测试函数参数中的泛型集合类型
    /// </summary>
    [Fact]
    public void Compile_GenericCollectionInFunctionParameter_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func process(items:list<int>) -> int {
    return items.Count()
}

func test() -> int {
    myList:list<int> <- {1, 2, 3}
    return process(myList)
}

Assert.True(test() == 3)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试函数返回值中的泛型集合类型
    /// </summary>
    [Fact]
    public void Compile_GenericCollectionInFunctionReturnType_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func getNumbers() -> list<int> {
    return {1, 2, 3}
}

func test() -> int {
    result <- getNumbers()
    return result.Count()
}

Assert.True(test() == 3)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 向后兼容性测试

    /// <summary>
    /// 测试不带类型注解的集合（向后兼容）
    /// </summary>
    [Fact]
    public void Compile_NonTypedCollections_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
func test() -> int {
    items <- {1, 2, 3}
    arr <- [10, 20, 30]
    return items[1] + arr[2]
}

Assert.True(test() == 32)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 自定义类型泛型测试

    /// <summary>
    /// 测试自定义类型的泛型列表
    /// </summary>
    [Fact]
    public void Compile_GenericListWithCustomType_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
class Person {
    public name:string
    public age:int
}

func test() -> int {
    alice <- Person()
    alice.name <- ""Alice""
    alice.age <- 30

    bob <- Person()
    bob.name <- ""Bob""
    bob.age <- 25

    people:list<Person> <- {alice, bob}
    return people.Count()
}

Assert.True(test() == 2)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试自定义类型的泛型数组
    /// </summary>
    [Fact]
    public void Compile_GenericArrayWithCustomType_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
class Product {
    public name:string
    public price:double
}

func test() -> int {
    apple <- Product()
    apple.name <- ""Apple""
    apple.price <- 1.5

    banana <- Product()
    banana.name <- ""Banana""
    banana.price <- 2.0

    products:array<Product> <- [apple, banana]
    return products.Length
}

Assert.True(test() == 2)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试嵌套的自定义类型泛型
    /// </summary>
    [Fact]
    public void Compile_NestedGenericWithCustomType_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
class TaskClass {
    public id:int
    public title:string
}

func test() -> int {
    task1 <- TaskClass()
    task1.id <- 1
    task1.title <- ""Task 1""

    task2 <- TaskClass()
    task2.id <- 2
    task2.title <- ""Task 2""

    groups:dict<string, list<TaskClass>> <- {
        ""todo"": {task1, task2}
    }

    todoTasks <- groups[""todo""]
    return todoTasks.Count()
}

Assert.True(test() == 2)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试函数参数中的自定义类型泛型
    /// </summary>
    [Fact]
    public void Compile_FunctionWithCustomTypeGeneric_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
class Student {
    public name:string
    public score:int
}

func countStudents(students:list<Student>) -> int {
    return students.Count()
}

func test() -> int {
    alice <- Student()
    alice.name <- ""Alice""
    alice.score <- 95

    bob <- Student()
    bob.name <- ""Bob""
    bob.score <- 88

    students:list<Student> <- {alice, bob}
    return countStudents(students)
}

Assert.True(test() == 2)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
