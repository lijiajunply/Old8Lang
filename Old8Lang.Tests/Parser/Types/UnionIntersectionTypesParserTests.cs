using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Types;

/// <summary>
/// 联合类型和交叉类型解析测试
/// </summary>
[Collection("Sequential")]
public class UnionIntersectionTypesParserTests
{
    #region 基本联合类型解析

    /// <summary>
    /// 测试简单联合类型变量声明
    /// </summary>
    [Fact]
    public void ParseProgram_SimpleUnionTypeVariable_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
a: int | string <- 123
b: double | bool <- 3.14";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多类型联合
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleTypesUnion_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
value: int | string | bool | double <- 123";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试可空联合类型
    /// </summary>
    [Fact]
    public void ParseProgram_NullableUnionType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
a: int? | string? <- null
b: double? | bool? <- 3.14";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 联合类型函数声明

    /// <summary>
    /// 测试联合类型函数参数
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeFunctionParameter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func process(x: int | string) -> void {
    PrintLine(x.ToStr())
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试联合类型函数返回值
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeFunctionReturn_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getValue(flag: bool) -> int | string {
    if flag {
        return 123
    } else {
        return ""hello""
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多个联合类型参数
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleUnionTypeParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func process(a: int | string, b: double | bool, c: int | string | bool) -> void {
    PrintLine(a.ToStr())
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试联合类型参数和返回值
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeParameterAndReturn_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func transform(input: int | string) -> string | bool {
    if input == 0 {
        return true
    }
    return ""result""
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 联合类型类声明

    /// <summary>
    /// 测试类字段联合类型
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeClassField_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Container {
    public data: int | string | bool
    private value: double | int

    func init(d: int | string | bool) {
        this.data <- d
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试类方法参数和返回值使用联合类型
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeClassMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Processor {
    func process(input: int | string) -> string | bool {
        if input == 0 {
            return true
        }
        return ""result""
    }

    func getValue() -> int | string | bool {
        return 123
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 泛型联合类型

    /// <summary>
    /// 测试泛型参数中的联合类型
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeInGenericParameter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
list: List<int | string> <- {1, ""hello"", 2, ""world""}
map: Map<string, int | string> <- {""age"": 25, ""name"": ""Alice""}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套泛型中的联合类型
    /// </summary>
    [Fact]
    public void ParseProgram_NestedGenericUnionType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
nested: List<List<int | string>> <- {{1, ""a""}, {2, ""b""}}
complex: Map<string, List<int | bool>> <- {""data"": {1, true, 2}}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 基本交叉类型解析

    /// <summary>
    /// 测试简单交叉类型变量声明
    /// </summary>
    [Fact]
    public void ParseProgram_SimpleIntersectionTypeVariable_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IA {
    func methodA() -> void
}

interface IB {
    func methodB() -> void
}

handler: IA & IB <- null";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多接口交叉类型
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleInterfacesIntersection_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IA { func a() -> void }
interface IB { func b() -> void }
interface IC { func c() -> void }

handler: IA & IB & IC <- null";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 交叉类型泛型约束

    /// <summary>
    /// 测试泛型约束中的交叉类型（冒号语法）
    /// </summary>
    [Fact]
    public void ParseProgram_IntersectionTypeGenericConstraint_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IComparable {
    func compareTo(other: any) -> int
}

interface ICloneable {
    func clone() -> any
}

class SortedList<T: IComparable & ICloneable> {
    items: list

    func add(item: T) -> void {
        this.items.Add(item)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试 where 子句中的交叉类型约束
    /// </summary>
    [Fact]
    public void ParseProgram_IntersectionTypeWhereClause_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IComparable {
    func compareTo(other: any) -> int
}

interface ICloneable {
    func clone() -> any
}

func sort<T>(items: list) -> list where T: IComparable & ICloneable {
    return items
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数参数中的交叉类型
    /// </summary>
    [Fact]
    public void ParseProgram_IntersectionTypeFunctionParameter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IReadable {
    func read() -> string
}

interface IWritable {
    func write(data: string) -> void
}

func process(handler: IReadable & IWritable) -> void {
    data <- handler.read()
    handler.write(data)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 混合联合和交叉类型

    /// <summary>
    /// 测试联合类型和交叉类型的组合（简单组合，不嵌套）
    /// </summary>
    [Fact]
    public void ParseProgram_UnionAndIntersectionTypeCombination_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IA { func a() -> void }
interface IB { func b() -> void }

// 变量可以是 int 或 string
value1: int | string <- 123

// 变量必须同时实现 IA 和 IB
value2: IA & IB <- null";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试泛型约束中同时使用联合和交叉类型
    /// </summary>
    [Fact]
    public void ParseProgram_UnionAndIntersectionInGenerics_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IComparable { func compareTo(other: any) -> int }
interface ICloneable { func clone() -> any }

// 泛型参数必须同时实现两个接口
class Container<T: IComparable & ICloneable> {
    // 字段可以是 T 或 null
    value: T | null

    func init(v: T) {
        this.value <- v
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试联合类型带可空标记
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeWithNullableMembers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 每个成员都是可空的
a: int? | string? <- null
b: double? | bool? | char? <- 3.14";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试泛型类型中嵌套联合类型
    /// </summary>
    [Fact]
    public void ParseProgram_GenericWithNestedUnionType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// List 的元素可以是 int、string 或 bool
list: List<int | string | bool> <- {1, ""hello"", true, 2, ""world"", false}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试空联合类型（语法合法但可能无实际意义）
    /// </summary>
    [Fact]
    public void ParseProgram_SingleTypeUnion_ParsesSuccessfully()
    {
        // Arrange - 虽然只有一个类型，但联合语法是合法的
        var code = @"
a: int <- 123";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 复杂场景测试

    /// <summary>
    /// 测试综合场景：类、函数、泛型、联合和交叉类型
    /// </summary>
    [Fact]
    public void ParseProgram_ComprehensiveUnionIntersectionScenario_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IComparable {
    func compareTo(other: any) -> int
}

interface ISerializable {
    func serialize() -> string
}

// 泛型类，T 必须同时实现两个接口
class DataStore<T: IComparable & ISerializable> {
    // 字段可以是 T 或 null
    private data: T | null
    // 字段可以是 int 或 string
    public id: int | string

    func init(value: T | null, identifier: int | string) {
        this.data <- value
        this.id <- identifier
    }

    func getData() -> T | null {
        return this.data
    }

    func setData(value: T | null) -> void {
        this.data <- value
    }

    // 参数可以是 int 或 string，返回值可以是 T 或 null
    func findById(id: int | string) -> T | null {
        if this.id == id {
            return this.data
        }
        return null
    }
}

// 函数参数和返回值都使用联合类型
func processValue(input: int | string | bool) -> string | null {
    if input == null {
        return null
    }
    return input.ToStr()
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误语法测试

    /// <summary>
    /// 测试联合类型中的语法错误 - 缺少类型标识符
    /// </summary>
    [Fact]
    public void ParseProgram_UnionTypeMissingIdentifier_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
a: int | <- 123";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试交叉类型中的语法错误 - 缺少类型标识符
    /// </summary>
    [Fact]
    public void ParseProgram_IntersectionTypeMissingIdentifier_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
interface IA { func a() -> void }

a: IA & <- null";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
