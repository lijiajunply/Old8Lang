using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.EdgeCases;

/// <summary>
/// 简化的边界条件测试
/// </summary>
[Collection("Sequential")]
public class SimpleBoundaryTests
{
    #region 数值边界测试

    /// <summary>
    /// 测试基本数值边界
    /// </summary>
    [Fact]
    public void ParseProgram_BasicNumericBoundary_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 基本数值边界
maxInt <- 2147483647
minInt <- -2147483648
zero <- 0

// 边界运算
sum1 <- maxInt + 1
sum2 <- minInt - 1

// 比较边界
isMaxPositive <- maxInt > 0
isMinNegative <- minInt < 0
isZeroEqual <- zero == 0

// 浮点数边界
largeFloat <- 1.0e+20
smallFloat <- 1.0e-20
zeroFloat <- 0.0

// 浮点数运算
floatResult1 <- largeFloat * 2.0
floatResult2 <- smallFloat / 2.0";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 空值处理测试

    /// <summary>
    /// 测试基础空值操作
    /// </summary>
    [Fact]
    public void ParseProgram_BasicNullHandling_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 空值定义
nullValue <- null
explicitNull <- null
notNullValue <- 42

// 空值比较
isNull1 <- nullValue == null
isNull2 <- notNullValue != null

// 空值运算
nullPlusNumber <- null + 5
stringPlusNull <- ""test"" + null

// 空值逻辑
nullAndTrue <- null and true
nullOrTrue <- null or true

// 空值三元操作
ternaryResult1 <- null != null ? ""not null"" : ""is null""
ternaryResult2 <- notNullValue != null ? ""not null"" : ""is null""";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 空集合测试

    /// <summary>
    /// 测试空集合操作
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyCollectionOperations_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 空集合定义
emptyList <- {}
emptyArray <- []
emptyDict <- {}

// 空集合长度
emptyListLength <- emptyList.Count()
emptyArrayLength <- emptyArray.Count()
emptyDictLength <- emptyDict.Count()

// 空集合检查
isListEmpty <- emptyList.Count() == 0
isArrayEmpty <- emptyArray.Count() == 0
isDictEmpty <- emptyDict.Count() == 0

// 空集合添加元素
listAfterAdd <- emptyList.Push(42)
arrayAfterAdd <- emptyArray.Push(24)
dictAfterAdd <- emptyDict";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 字符串边界测试

    /// <summary>
    /// 测试字符串边界
    /// </summary>
    [Fact]
    public void ParseProgram_StringBoundary_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 字符串长度边界
emptyString <- """"
singleChar <- ""a""
normalString <- ""Hello""

// 字符串长度
emptyLength <- emptyString.Length()
singleLength <- singleChar.Length()
normalLength <- normalString.Length()

// 字符串检查
isEmpty1 <- emptyString == """"

// 字符串操作
combinedString <- emptyString + normalString
stringWithNull <- normalString + null";

        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 集合索引边界测试

    /// <summary>
    /// 测试集合索引边界
    /// </summary>
    [Fact]
    public void ParseProgram_CollectionIndexBoundary_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 创建测试集合
testList <- {1, 2, 3, 4, 5}
testArray <- [10, 20, 30, 40, 50]

// 正常索引访问
index0 <- testList[0]
index4 <- testList[4]
firstArray <- testArray[0]
lastArray <- testArray[4]

// 集合大小
listSize <- testList.Count()
arraySize <- testArray.Count()

// 边界检查
isIndexValid1 <- 0 < listSize
isIndexValid2 <- (listSize - 1) < listSize

// 边界值运算
boundarySum1 <- testList[0] + testList[listSize - 1]
boundarySum2 <- testArray[0] + testArray[arraySize - 1]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 循环边界测试

    /// <summary>
    /// 测试循环边界条件
    /// </summary>
    [Fact]
    public void ParseProgram_LoopBoundaryConditions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 边界循环测试
counter <- 0

// 零次循环
for i <- 0, i < 0, i <- i + 1 {
    counter <- counter + 1
}

// 单次循环
for i <- 0, i < 1, i <- i + 1 {
    counter <- counter + 1
}

// 边界条件循环
boundaryCount <- 0
for i <- 10, i > 0, i <- i - 1 {
    boundaryCount <- boundaryCount + 1
}

// 循环变量边界测试
loopResult <- 0
for i <- -2, i <= 2, i <- i + 1 {
    loopResult <- loopResult + i
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 类型转换边界测试

    /// <summary>
    /// 测试基础类型转换
    /// </summary>
    [Fact]
    public void ParseProgram_BasicTypeConversion_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 基础类型转换测试
intVal <- 42
floatVal <- 3.14
stringVal <- ""test""
boolVal <- true

// 数值运算
intSum <- intVal + 8
floatSum <- floatVal + 1.86

// 字符串连接
combinedStr <- stringVal + "" combined""
strWithInt <- ""Value: "" + intVal

// 布尔逻辑
notVal <- not boolVal
andResult <- boolVal and true
orResult <- boolVal or false

// 比较运算
isEqual <- intVal == 42
isGreater <- floatVal > 3.0
isLess <- intVal < 100";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误场景测试

    /// <summary>
    /// 测试基础错误场景
    /// </summary>
    [Fact]
    public void ParseProgram_BasicErrorScenarios_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 零除法错误
try {
    result <- 10 / 0
} catch (error) {
    divisionError <- error.ToStr()
}

// 空值方法调用错误
try {
    nullValue <- null
    result <- nullValue.ToStr()
} catch (error) {
    nullMethodError <- error.ToStr()
}

// 空集合索引错误
try {
    emptyList <- {}
    element <- emptyList[0]
} catch (error) {
    emptyListError <- error.ToStr()
}

// 无效数值转换错误
try {
    invalidInt <- ""abc"".ToInt()
} catch (error) {
    invalidIntError <- error.ToStr()
}

// 未终止的字符串（语法错误测试）
// unterminatedString <- ""This string is not terminated";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试未终止字符串语法错误
    /// </summary>
    [Fact]
    public void ParseProgram_UnterminatedString_ThrowsSyntaxError()
    {
        // Arrange
        var code = """
                   unterminatedString <- This string is not terminated
                   another <- 123 
                   """;
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}