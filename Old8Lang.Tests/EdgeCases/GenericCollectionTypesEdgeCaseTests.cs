using Old8Lang.Interpreter;
using Old8Lang.Error;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.EdgeCases;

/// <summary>
/// 泛型集合类型边界和错误测试
/// 测试极限情况、边界条件和错误处理
/// </summary>
[Collection("Sequential")]
public class GenericCollectionTypesEdgeCaseTests
{
    #region 空集合测试

    /// <summary>
    /// 测试空泛型列表
    /// </summary>
    [Fact]
    public void Empty_GenericList_HandledCorrectly()
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

    /// <summary>
    /// 测试空泛型数组
    /// </summary>
    [Fact]
    public void Empty_GenericArray_HandledCorrectly()
    {
        // Arrange
        var code = @"
empty:array<string> <- []
result <- empty.Count()
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

    /// <summary>
    /// 测试空嵌套泛型列表
    /// </summary>
    [Fact]
    public void Empty_NestedGenericList_HandledCorrectly()
    {
        // Arrange
        var code = @"
emptyNested:list<list<int>> <- {}
result <- len(emptyNested)
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

    /// <summary>
    /// 测试包含空列表的嵌套泛型列表
    /// </summary>
    [Fact]
    public void NestedGenericList_WithEmptyLists_HandledCorrectly()
    {
        // Arrange
        var code = @"
matrix:list<list<int>> <- {{}, {1, 2}, {}}
result <- len(matrix)
empty1 <- len(matrix[0])
filled <- len(matrix[1])
empty2 <- len(matrix[2])
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var empty1 = interpreter.Manager.GetValue(new LangId("empty1"));
        var filled = interpreter.Manager.GetValue(new LangId("filled"));
        var empty2 = interpreter.Manager.GetValue(new LangId("empty2"));

        Assert.Equal(3, ((IntLangValue)result).Value);
        Assert.Equal(0, ((IntLangValue)empty1).Value);
        Assert.Equal(2, ((IntLangValue)filled).Value);
        Assert.Equal(0, ((IntLangValue)empty2).Value);
    }

    #endregion

    #region 单元素集合测试

    /// <summary>
    /// 测试单元素泛型列表
    /// </summary>
    [Fact]
    public void SingleElement_GenericList_HandledCorrectly()
    {
        // Arrange
        var code = @"
single:list<int> <- {42}
result <- single[0]
count <- len(single)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var count = interpreter.Manager.GetValue(new LangId("count"));

        Assert.Equal(42, ((IntLangValue)result).Value);
        Assert.Equal(1, ((IntLangValue)count).Value);
    }

    /// <summary>
    /// 测试单元素泛型数组
    /// </summary>
    [Fact]
    public void SingleElement_GenericArray_HandledCorrectly()
    {
        // Arrange
        var code = @"
single:array<string> <- [""hello""]
result <- single[0]
count <- single.Count()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var count = interpreter.Manager.GetValue(new LangId("count"));

        Assert.Equal("hello", ((StringLangValue)result).Value);
        Assert.Equal(1, ((IntLangValue)count).Value);
    }

    /// <summary>
    /// 测试单键值对字典
    /// </summary>
    [Fact]
    public void SinglePair_GenericDictionary_HandledCorrectly()
    {
        // Arrange
        var code = @"
single:dict<string, int> <- {""key"": 123}
result <- single[""key""]
count <- len(single)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var count = interpreter.Manager.GetValue(new LangId("count"));

        Assert.Equal(123, ((IntLangValue)result).Value);
        Assert.Equal(1, ((IntLangValue)count).Value);
    }

    #endregion

    #region 大型集合测试

    /// <summary>
    /// 测试大型泛型列表
    /// </summary>
    [Fact]
    public void Large_GenericList_HandledCorrectly()
    {
        // Arrange - 创建 100 个元素的列表
        var elements = string.Join(", ", Enumerable.Range(1, 100));
        var code = $@"
large:list<int> <- {{{elements}}}
result <- len(large)
first <- large[0]
last <- large[99]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var first = interpreter.Manager.GetValue(new LangId("first"));
        var last = interpreter.Manager.GetValue(new LangId("last"));

        Assert.Equal(100, ((IntLangValue)result).Value);
        Assert.Equal(1, ((IntLangValue)first).Value);
        Assert.Equal(100, ((IntLangValue)last).Value);
    }

    /// <summary>
    /// 测试深度嵌套的泛型列表（3层）
    /// </summary>
    [Fact]
    public void DeepNested_GenericList_HandledCorrectly()
    {
        // Arrange
        var code = @"
cube:list<list<list<int>>> <- {
    {
        {1, 2}, {3, 4}
    },
    {
        {5, 6}, {7, 8}
    }
}
result1 <- cube[0][0][0]
result2 <- cube[1][1][1]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.Equal(1, ((IntLangValue)result1).Value);
        Assert.Equal(8, ((IntLangValue)result2).Value);
    }

    #endregion

    #region 特殊字符和值测试

    /// <summary>
    /// 测试包含特殊字符的字符串列表
    /// </summary>
    [Fact]
    public void SpecialCharacters_StringList_HandledCorrectly()
    {
        // Arrange
        var code = @"
special:list<string> <- {""hello\nworld"", ""tab\there"", ""quote\""test""}
result <- len(special)
first <- special[0]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试浮点数精度
    /// </summary>
    [Fact]
    public void FloatingPoint_DoubleList_HandledCorrectly()
    {
        // Arrange
        var code = @"
doubles:list<double> <- {3.14159, 2.71828, 1.41421, 0.00001}
result <- len(doubles)
first <- doubles[0]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var first = interpreter.Manager.GetValue(new LangId("first"));

        Assert.Equal(4, ((IntLangValue)result).Value);
        Assert.IsType<DoubleLangValue>(first);
        Assert.Equal(3.14159, ((DoubleLangValue)first).Value, precision: 5);
    }

    /// <summary>
    /// 测试负数和零
    /// </summary>
    [Fact]
    public void NegativeAndZero_IntList_HandledCorrectly()
    {
        // Arrange
        var code = @"
numbers:list<int> <- {-100, -1, 0, 1, 100}
result <- len(numbers)
negative <- numbers[0]
zero <- numbers[2]
positive <- numbers[4]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var negative = interpreter.Manager.GetValue(new LangId("negative"));
        var zero = interpreter.Manager.GetValue(new LangId("zero"));
        var positive = interpreter.Manager.GetValue(new LangId("positive"));

        Assert.Equal(5, ((IntLangValue)result).Value);
        Assert.Equal(-100, ((IntLangValue)negative).Value);
        Assert.Equal(0, ((IntLangValue)zero).Value);
        Assert.Equal(100, ((IntLangValue)positive).Value);
    }

    #endregion

    #region 编译器模式类型错误边界测试

    /// <summary>
    /// 测试编译器模式下第一个元素类型错误
    /// </summary>
    [Fact]
    public void Compiler_FirstElementTypeMismatch_ThrowsException()
    {
        // Arrange
        var code = @"
func test() -> int {
    items:list<int> <- {""hello"", 2, 3}
    return items[0]
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("类型不匹配", exception.Message);
        Assert.Contains("第 0 个元素", exception.Message);
    }

    /// <summary>
    /// 测试编译器模式下最后一个元素类型错误
    /// </summary>
    [Fact]
    public void Compiler_LastElementTypeMismatch_ThrowsException()
    {
        // Arrange
        var code = @"
func test() -> int {
    items:list<int> <- {1, 2, ""hello""}
    return items[0]
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("类型不匹配", exception.Message);
        Assert.Contains("第 2 个元素", exception.Message);
    }

    /// <summary>
    /// 测试编译器模式下中间元素类型错误
    /// </summary>
    [Fact]
    public void Compiler_MiddleElementTypeMismatch_ThrowsException()
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

        Assert.Contains("类型不匹配", exception.Message);
        Assert.Contains("第 1 个元素", exception.Message);
    }

    /// <summary>
    /// 测试编译器模式下所有元素类型都错误
    /// </summary>
    [Fact]
    public void Compiler_AllElementsTypeMismatch_ThrowsException()
    {
        // Arrange
        var code = @"
func test() -> int {
    items:list<int> <- {""a"", ""b"", ""c""}
    return items.Count()
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)
        );

        Assert.Contains("类型不匹配", exception.Message);
        // 应该报告第一个错误
        Assert.Contains("第 0 个元素", exception.Message);
    }

    /// <summary>
    /// 测试编译器模式下嵌套类型的内部元素错误
    /// </summary>
    [Fact]
    public void Compiler_NestedTypeMismatch_ThrowsException()
    {
        // Arrange
        var code = @"
func test() -> int {
    matrix:list<list<int>> <- {{1, 2}, {""a"", ""b""}}
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

    #region 解释器模式灵活性测试

    /// <summary>
    /// 测试解释器模式允许混合类型（带类型注解）
    /// </summary>
    [Fact]
    public void Interpreter_MixedTypesWithAnnotation_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
// 解释器模式下，类型注解不强制
items:list<int> <- {1, ""hello"", 3.14, true}
result <- len(items)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.Equal(4, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试解释器模式允许混合类型（不带类型注解）
    /// </summary>
    [Fact]
    public void Interpreter_MixedTypesWithoutAnnotation_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
items <- {1, ""hello"", 3.14, true, 'A'}
result <- len(items)
first <- items[0]
second <- items[1]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var first = interpreter.Manager.GetValue(new LangId("first"));
        var second = interpreter.Manager.GetValue(new LangId("second"));

        Assert.Equal(5, ((IntLangValue)result).Value);
        Assert.Equal(1, ((IntLangValue)first).Value);
        Assert.Equal("hello", ((StringLangValue)second).Value);
    }

    #endregion

    #region 可空类型测试

    /// <summary>
    /// 测试可空泛型集合类型
    /// </summary>
    [Fact]
    public void Nullable_GenericCollectionType_HandledCorrectly()
    {
        // Arrange
        var code = @"
nullableList:list<int>? <- null
result1 <- nullableList == null

items:list<int>? <- {1, 2, 3}
result2 <- items != null
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.True(((BoolLangValue)result2).Value);
    }

    #endregion

    #region 泛型类型参数边界测试

    /// <summary>
    /// 测试复杂的嵌套泛型类型
    /// </summary>
    [Fact]
    public void Complex_NestedGenericType_HandledCorrectly()
    {
        // Arrange
        var code = @"
complex:dict<string, list<array<int>>> <- {
    ""group1"": {[1, 2], [3, 4]},
    ""group2"": {[5, 6], [7, 8]}
}
result <- complex[""group1""][0][0]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    #endregion

    #region 自定义类型泛型边界测试

    /// <summary>
    /// 测试空的自定义类型泛型列表
    /// </summary>
    [Fact]
    public void Empty_GenericListWithCustomType_HandledCorrectly()
    {
        // Arrange
        var code = @"
class Person {
    public name:string
    public age:int
}

people:list<Person> <- {}
result <- len(people)
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

    /// <summary>
    /// 测试单个自定义类型元素的列表
    /// </summary>
    [Fact]
    public void SingleElement_GenericListWithCustomType_HandledCorrectly()
    {
        // Arrange
        var code = @"
class User {
    public id:int
    public email:string
}

user <- User()
user.id <- 1
user.email <- ""test@example.com""

users:list<User> <- {user}
result <- len(users)
firstUser <- users[0]
userId <- firstUser.id
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var userId = interpreter.Manager.GetValue(new LangId("userId"));

        Assert.Equal(1, ((IntLangValue)result).Value);
        Assert.Equal(1, ((IntLangValue)userId).Value);
    }

    /// <summary>
    /// 测试大型自定义类型集合
    /// </summary>
    [Fact]
    public void Large_GenericListWithCustomType_HandledCorrectly()
    {
        // Arrange
        var code = @"
class Item {
    public id:int
    public value:string
}

items:list<Item> <- {}
for i in [1~50] {
    item <- Item()
    item.id <- i
    item.value <- $""Item {i}""
    items.Add(item)
}

result <- len(items)
firstItem <- items[0]
lastItem <- items[49]
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var firstItem = interpreter.Manager.GetValue(new LangId("firstItem"));
        var lastItem = interpreter.Manager.GetValue(new LangId("lastItem"));

        Assert.NotNull(result);
        Assert.Equal(50, ((IntLangValue)result).Value);
        Assert.NotNull(firstItem);
        Assert.NotNull(lastItem);
    }

    /// <summary>
    /// 测试嵌套自定义类型的边界情况
    /// </summary>
    [Fact]
    public void Nested_GenericListWithCustomType_EmptyInnerLists_HandledCorrectly()
    {
        // Arrange
        var code = @"
class TaskClass {
    public id:int
    public title:string
}

groups:dict<string, list<TaskClass>> <- {
    ""todo"": {},
    ""done"": {}
}

result1 <- len(groups[""todo""])
result2 <- len(groups[""done""])
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.Equal(0, ((IntLangValue)result1).Value);
        Assert.Equal(0, ((IntLangValue)result2).Value);
    }

    /// <summary>
    /// 测试自定义类型数组边界情况
    /// </summary>
    [Fact]
    public void CustomType_GenericArray_BoundaryConditions_HandledCorrectly()
    {
        // Arrange
        var code = @"
class Product {
    public name:string
    public price:double
}

// 空数组
emptyArray:array<Product> <- []
result1 <- len(emptyArray)

// 单元素数组
apple <- Product()
apple.name <- ""Apple""
apple.price <- 1.5

singleArray:array<Product> <- [apple]
result2 <- len(singleArray)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.Equal(0, ((IntLangValue)result1).Value);
        Assert.Equal(1, ((IntLangValue)result2).Value);
    }

    #endregion
}
