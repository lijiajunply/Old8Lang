using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Generics;

/// <summary>
/// 泛型语法解析测试
/// </summary>
[Collection("Sequential")]
public class GenericsParserTests
{
    #region 基本泛型类解析

    /// <summary>
    /// 测试基本泛型类语法
    /// </summary>
    [Fact]
    public void ParseProgram_BasicGenericClass_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Box<T> {
    private value:T

    func set(v:T) -> void {
        this.value <- v
    }

    func get() -> T {
        return this.value
    }
}

interface IComparable {
    func compareTo(other) {

    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试泛型类字段声明
    /// </summary>
    [Fact]
    public void ParseProgram_GenericClassFields_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Container<T> {
    private item:T
    public items:List<T>
    protected data:Dictionary<string, T>
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试带修饰符的泛型字段
    /// </summary>
    [Fact]
    public void ParseProgram_GenericFieldsWithModifiers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Test {
    private items:List<int>
    public map:Dictionary<string, int>
    protected nested:List<List<string>>
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试不带修饰符的泛型字段
    /// </summary>
    [Fact]
    public void ParseProgram_GenericFieldsWithoutModifiers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
items:List<int>
map:Dictionary<string, List<int>>
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 基本泛型函数解析

    /// <summary>
    /// 测试基本泛型函数语法
    /// </summary>
    [Fact]
    public void ParseProgram_BasicGenericFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func identity<T>(value:T) -> T {
    return value
}

func getFirst<T>(list:List<T>) -> T {
    return list[0]
}

func maximum<T>(a:T, b:T) -> T {
    return a
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试泛型函数与泛型类型参数
    /// </summary>
    [Fact]
    public void ParseProgram_GenericFunctionWithGenericTypeParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func map<T, U>(source:List<T>, mapper:Func<T, U>) -> List<U> {
    result <- List<U>()
    for item in source {
        result.Add(mapper(item))
    }
    return result
}

func filter<T>(source:List<T>, predicate:Func<T, bool>) -> List<T> {
    result <- List<T>()
    for item in source {
        if predicate(item) {
            result.Add(item)
        }
    }
    return result
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 泛型约束解析

    /// <summary>
    /// 测试单个泛型约束
    /// </summary>
    [Fact]
    public void ParseProgram_SingleGenericConstraint_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class SortedList<T: IComparable> {
    private items:List<T>

    func add(item:T) -> void {
        // implementation
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多个泛型约束
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleGenericConstraints_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Container<T: IComparable | ICloneable> {
    private value:T

    func compare(other:T) -> int {
        return this.value.CompareTo(other)
    }

    func clone() -> T {
        return this.value.Clone()
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试泛型接口约束
    /// </summary>
    [Fact]
    public void ParseProgram_GenericInterfaceWithConstraints_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IRepository<T: IEntity> {
    func save(entity:T)
    func findById(id:int) -> T
    func findAll() -> List<T>
}

interface IEntity {
    func getId() -> int
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 嵌套泛型解析

    /// <summary>
    /// 测试二层嵌套泛型
    /// </summary>
    [Fact]
    public void ParseProgram_TwoLevelNestedGenerics_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Matrix<T> {
    private data:List<List<T>>

    func getRow(index:int) -> List<T> {
        return this.data[index]
    }

    func getCell(row:int, col:int) -> T {
        return this.data[row][col]
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂嵌套泛型
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexNestedGenerics_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class ComplexContainer<T> {
    private map:Dictionary<string, List<T>>
    private matrix:List<List<List<T>>>

    func get(key:string) -> List<T> {
        return this.map[key]
    }

    func getCube(x:int, y:int, z:int) -> T {
        return this.matrix[x][y][z]
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 多类型参数解析

    /// <summary>
    /// 测试双类型参数泛型类
    /// </summary>
    [Fact]
    public void ParseProgram_TwoTypeParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Pair<K, V> {
    private key:K
    private value:V

    func getKey() -> K {
        return this.key
    }

    func getValue() -> V {
        return this.value
    }

    func setKey(k:K) -> void {
        this.key <- k
    }

    func setValue(v:V) -> void {
        this.value <- v
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试三类型参数泛型类
    /// </summary>
    [Fact]
    public void ParseProgram_ThreeTypeParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Triple<T1, T2, T3> {
    private first:T1
    private second:T2
    private third:T3

    func getFirst() -> T1 {
        return this.first
    }

    func getSecond() -> T2 {
        return this.second
    }

    func getThird() -> T3 {
        return this.third
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多类型参数泛型函数
    /// </summary>
    [Fact]
    public void ParseProgram_GenericFunctionWithMultipleTypeParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func createPair<T1, T2>(first:T1, second:T2) -> Pair<T1, T2> {
    return null
}

func combine<T1, T2, T3>(a:T1, b:T2, c:T3) -> Triple<T1, T2, T3> {
    return null
}

func transform<TInput, TOutput>(input:TInput, transformer:Func<TInput, TOutput>) -> TOutput {
    return transformer(input)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 泛型实例化解析

    /// <summary>
    /// 测试基本泛型实例化
    /// </summary>
    [Fact]
    public void ParseProgram_BasicGenericInstantiation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
intBox <- Box<int>()
stringBox <- Box<string>()
doubleBox <- Box<double>()

result1 <- identity<int>(42)
result2 <- identity<string>(""hello"")
result3 <- identity<double>(3.14)
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套泛型实例化
    /// </summary>
    [Fact]
    public void ParseProgram_NestedGenericInstantiation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
listBox <- Box<List<int>>()
matrixBox <- Box<List<List<string>>>()
mapBox <- Box<Dictionary<string, List<int>>>()

matrix <- Matrix<int>()
nested <- Matrix<List<string>>()
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多类型参数实例化
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleTypeParametersInstantiation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
pair <- Pair<int, string>()
intStringPair <- Pair<int, string>(123, ""hello"")

triple <- Triple<int, string, double>()
typedTriple <- Triple<int, string, double>(1, ""test"", 3.14)

result <- zip<int, string>({1, 2, 3}, {""a"", ""b"", ""c""})
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 综合泛型场景

    /// <summary>
    /// 测试泛型类与泛型函数组合
    /// </summary>
    [Fact]
    public void ParseProgram_GenericClassAndFunctionCombination_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Repository<T: IEntity> {
    private items:List<T>

    func constructor() {
        this.items <- List<T>()
    }

    func add(item:T) -> void {
        this.items.Add(item)
    }

    func findAll() -> List<T> {
        return this.items
    }

    func findById(id:int) -> T {
        for item in this.items {
            if item.getId() == id {
                return item
            }
        }
        return null
    }
}

func createRepository<T: IEntity>() -> Repository<T> {
    return Repository<T>()
}

func processItems<T: IEntity>(repo:Repository<T>, processor:Func<T, void>) -> void {
    items <- repo.findAll()
    for item in items {
        processor(item)
    }
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂泛型场景
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexGenericScenario_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Cache<K: IComparable, V> {
    private data:Dictionary<K, V>
    private keys:List<K>

    func constructor() {
        this.data <- Dictionary<K, V>()
        this.keys <- List<K>()
    }

    func put(key:K, value:V) -> void {
        if not this.data.ContainsKey(key) {
            this.keys.Add(key)
        }
        this.data[key] <- value
    }

    func get(key:K) -> V {
        if this.data.ContainsKey(key) {
            return this.data[key]
        }
        return null
    }

    func getAllKeys() -> List<K> {
        return this.keys
    }

    func getAllValues() -> List<V> {
        values <- List<V>()
        for key in this.keys {
            values.Add(this.data[key])
        }
        return values
    }

    func transform<U>(mapper:Func<V, U>) -> Cache<K, U> {
        newCache <- Cache<K, U>()
        for key in this.keys {
            newCache.put(key, mapper(this.data[key]))
        }
        return newCache
    }
}

stringCache <- Cache<int, string>()
stringCache.put(1, ""hello"")
stringCache.put(2, ""world"")

intCache <- stringCache.transform<int>((s) -> s.Length)
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 泛型接口解析

    /// <summary>
    /// 测试泛型接口基本语法
    /// </summary>
    [Fact]
    public void ParseProgram_GenericInterface_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IContainer<T> {
    func add(item:T) -> void
    func remove(item:T) -> bool
    func contains(item:T) -> bool
    func getAll() -> List<T>
}

interface IMapper<TSource, TTarget> {
    func map(source:TSource) -> TTarget
    func mapList(sources:List<TSource>) -> List<TTarget>
}
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 自定义小写类型名称

    /// <summary>
    /// 测试自定义小写类型名称作为泛型参数
    /// </summary>
    [Fact]
    public void ParseProgram_CustomLowercaseTypeName_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 自定义小写类型名称
class a {
    private value:int
}

// 使用自定义类型作为泛型参数
box <- List<a>()
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套泛型中使用小写自定义类型名称
    /// </summary>
    [Fact]
    public void ParseProgram_NestedGenericWithLowercaseTypeName_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class item {
    private data:string
}

// 嵌套泛型使用小写类型名称
matrix <- List<List<item>>()
boxList <- Box<List<item>>()
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的泛型语法

    /// <summary>
    /// 测试泛型括号不匹配 - 缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingClosingBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class Box<T {
    private value:T
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试泛型括号不匹配
    /// </summary>
    [Fact]
    public void ParseProgram_UnmatchedGenericBrackets_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class Box<T {
    private value:T
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试约束语法错误
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidConstraintSyntax_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class Container<T: > {
    private value:T
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
