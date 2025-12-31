using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Types;

/// <summary>
/// 可空泛型类型解析测试
/// </summary>
[Collection("Sequential")]
public class NullableGenericsParserTests
{
    #region 基本可空泛型类解析

    /// <summary>
    /// 测试单个可空类型参数的泛型类
    /// </summary>
    [Fact]
    public void ParseProgram_SingleNullableTypeParameter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Box<T?> {
    value: T?

    func init(v: T?) {
        this.value <- v
    }

    func getValue() -> T? {
        return this.value
    }

    func hasValue() -> bool {
        return this.value != null
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多个可空类型参数的泛型类
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleNullableTypeParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Pair<K?, V?> {
    key: K?
    value: V?

    func init(k: K?, v: V?) {
        this.key <- k
        this.value <- v
    }

    func getKey() -> K? {
        return this.key
    }

    func getValue() -> V? {
        return this.value
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 混合可空和非可空类型参数

    /// <summary>
    /// 测试混合可空和非可空类型参数
    /// </summary>
    [Fact]
    public void ParseProgram_MixedNullableAndNonNullableParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Container<T, U?> {
    required: T
    optional: U?

    func init(r: T, o: U?) {
        this.required <- r
        this.optional <- o
    }

    func getRequired() -> T {
        return this.required
    }

    func getOptional() -> U? {
        return this.optional
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多个类型参数的混合可空性
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleParametersMixedNullability_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Triple<T, U?, V> {
    first: T
    second: U?
    third: V

    func init(f: T, s: U?, t: V) {
        this.first <- f
        this.second <- s
        this.third <- t
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 可空泛型函数解析

    /// <summary>
    /// 测试可空类型参数的泛型函数
    /// </summary>
    [Fact]
    public void ParseProgram_NullableGenericFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func identity<T?>(value: T?) -> T? {
    return value
}

func getOrDefault<T?>(value: T?, defaultVal: T?) -> T? {
    if value != null {
        return value
    }
    return defaultVal
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试混合可空和非可空参数的泛型函数
    /// </summary>
    [Fact]
    public void ParseProgram_MixedNullabilityGenericFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func wrap<T, U?>(required: T, optional: U?) -> Container<T, U?> {
    return null
}

func process<T, U?>(value: T, optionalValue: U?) -> T {
    return value
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 可空类型参数带约束

    /// <summary>
    /// 测试可空类型参数带单个约束
    /// </summary>
    [Fact]
    public void ParseProgram_NullableTypeParameterWithSingleConstraint_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IValue {
    func getValue() -> any
}

class OptionalValue<T?: IValue> {
    data: T?

    func init(d: T?) {
        this.data <- d
    }

    func hasValue() -> bool {
        return this.data != null
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试可空类型参数带多个约束
    /// </summary>
    [Fact]
    public void ParseProgram_NullableTypeParameterWithMultipleConstraints_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IComparable {
    func compareTo(other) -> int
}

interface ICloneable {
    func clone() -> any
}

class OptionalContainer<T?: IComparable & ICloneable> {
    value: T?

    func init(v: T?) {
        this.value <- v
    }

    func compare(other: T?) -> int {
        if this.value != null {
            return this.value.compareTo(other)
        }
        return 0
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 可空类型参数与 where 子句

    /// <summary>
    /// 测试可空类型参数与 where 子句
    /// </summary>
    [Fact]
    public void ParseProgram_NullableTypeParameterWithWhereClause_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IValue {
    func getValue() -> any
}

func process<T?>(value: T?) -> T? where T: IValue {
    return value
}

func transform<T?>(input: T?) -> string where T: IValue {
    if input != null {
        return input.getValue().ToStr()
    }
    return ""null""
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多个可空类型参数与 where 子句
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleNullableParametersWithWhereClause_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IValue {
    func getValue() -> any
}

interface IComparable {
    func compareTo(other) -> int
}

func combine<K?, V?>(key: K?, value: V?) -> any where K: IValue, V: IComparable {
    return null
}

func merge<T?, U?>(first: T?, second: U?) -> string where T: IValue, U: IValue {
    return """"
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试混合可空性的类型参数与 where 子句
    /// </summary>
    [Fact]
    public void ParseProgram_MixedNullabilityWithWhereClause_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IValue {
    func getValue() -> any
}

interface IComparable {
    func compareTo(other) -> int
}

func process<T, U?>(required: T, optional: U?) -> T where T: IComparable, U: IValue {
    return required
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 可空泛型接口解析

    /// <summary>
    /// 测试可空类型参数的泛型接口
    /// </summary>
    [Fact]
    public void ParseProgram_NullableGenericInterface_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IOptional<T?> {
    func getValue() -> T?
    func hasValue() -> bool
    func getOrDefault(defaultValue: T?) -> T?
}

interface IContainer<T?, U> {
    func getOptional() -> T?
    func getRequired() -> U
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region Optional 模式实现

    /// <summary>
    /// 测试完整的 Optional 类型模式实现
    /// </summary>
    [Fact]
    public void ParseProgram_OptionalPattern_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Optional<T?> {
    private value: T?
    private hasVal: bool

    func init(v: T?) {
        this.value <- v
        this.hasVal <- v != null
    }

    func hasValue() -> bool {
        return this.hasVal
    }

    func getValue() -> T? {
        return this.value
    }

    func getOrDefault(defaultValue: T?) -> T? {
        if this.hasVal {
            return this.value
        }
        return defaultValue
    }

    func map<U?>(mapper: Func<T?, U?>) -> Optional<U?> {
        if this.hasVal {
            return Optional<U?>(mapper(this.value))
        }
        return Optional<U?>(null)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 嵌套可空泛型

    /// <summary>
    /// 测试嵌套的可空泛型类型
    /// </summary>
    [Fact]
    public void ParseProgram_NestedNullableGenerics_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class NestedOptional<T?> {
    private data: List<T?>

    func init() {
        this.data <- {}
    }

    func add(item: T?) -> void {
        this.data.Add(item)
    }

    func getAll() -> List<T?> {
        return this.data
    }
}

class ComplexContainer<T?, U?> {
    private pairs: List<Pair<T?, U?>>

    func init() {
        this.pairs <- {}
    }

    func addPair(key: T?, value: U?) -> void {
        pair <- Pair<T?, U?>(key, value)
        this.pairs.Add(pair)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 综合场景

    /// <summary>
    /// 测试可空泛型的综合应用场景
    /// </summary>
    [Fact]
    public void ParseProgram_ComprehensiveNullableGenericsScenario_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IValue {
    func getValue() -> any
}

class Result<T?, E?> {
    private success: bool
    private value: T?
    private error: E?

    func init(isSuccess: bool, val: T?, err: E?) {
        this.success <- isSuccess
        this.value <- val
        this.error <- err
    }

    func isSuccess() -> bool {
        return this.success
    }

    func getValue() -> T? {
        return this.value
    }

    func getError() -> E? {
        return this.error
    }

    func map<U?>(mapper: Func<T?, U?>) -> Result<U?, E?> {
        if this.success {
            return Result<U?, E?>(true, mapper(this.value), null)
        }
        return Result<U?, E?>(false, null, this.error)
    }
}

func tryParse<T?: IValue>(input: string) -> Result<T?, string> {
    return Result<T?, string>(true, null, null)
}

func getOrElse<T?>(optional: T?, defaultValue: T?) -> T? {
    if optional != null {
        return optional
    }
    return defaultValue
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
    /// 测试可空标记位置错误 - 在约束之后
    /// </summary>
    [Fact]
    public void ParseProgram_NullableMarkerAfterConstraint_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
interface IValue {
    func getValue() -> any
}

class Box<T: IValue?> {
    value: T
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
