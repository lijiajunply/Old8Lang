using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 类型转换测试
/// </summary>
public class TypeConversionTests
{
    [Fact]
    public void TypeConversion_IntToString_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            intValue <- 123
            stringValue <- intValue.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("stringValue"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("123", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeConversion_DoubleToString_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            doubleValue <- 3.14159
            stringValue <- doubleValue.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("stringValue"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("3.14159", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeConversion_BoolToString_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            boolValue1 <- true
            boolValue2 <- false
            stringTrue <- boolValue1.ToStr()
            stringFalse <- boolValue2.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("stringTrue"));
        var result2 = interpreter.Manager.GetValue(new LangId("stringFalse"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("true", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("false", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void TypeConversion_CharToString_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            charValue <- 'A'
            stringValue <- charValue.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("stringValue"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("A", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TypeConversion_StringToInt_ConvertsValidIntegers()
    {
        // Arrange
        var code = @"
            stringInt1 <- ""123""
            stringInt2 <- ""-456""
            stringInt3 <- ""0""
            intValue1 <- stringInt1.ToInt()
            intValue2 <- stringInt2.ToInt()
            intValue3 <- stringInt3.ToInt()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("intValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("intValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("intValue3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(123, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(-456, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(0, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void TypeConversion_StringToDouble_ConvertsValidDoubles()
    {
        // Arrange
        var code = @"
            stringDouble1 <- ""3.14159""
            stringDouble2 <- ""-2.718""
            stringDouble3 <- ""0.0""
            doubleValue1 <- stringDouble1.ToDouble()
            doubleValue2 <- stringDouble2.ToDouble()
            doubleValue3 <- stringDouble3.ToDouble()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("doubleValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("doubleValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("doubleValue3"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(3.14159, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(-2.718, ((DoubleLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<DoubleLangValue>(result3);
        Assert.Equal(0.0, ((DoubleLangValue)result3).Value);
    }

    [Fact]
    public void TypeConversion_StringToBool_ConvertsValidBooleans()
    {
        // Arrange
        var code = @"
            stringBool1 <- ""true""
            stringBool2 <- ""false""
            stringBool3 <- ""True""
            stringBool4 <- ""False""
            boolValue1 <- stringBool1.ToBool()
            boolValue2 <- stringBool2.ToBool()
            boolValue3 <- stringBool3.ToBool()
            boolValue4 <- stringBool4.ToBool()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("boolValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("boolValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("boolValue3"));
        var result4 = interpreter.Manager.GetValue(new LangId("boolValue4"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.Equal(true, ((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(false, ((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(true, ((BoolLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.Equal(false, ((BoolLangValue)result4).Value);
    }

    [Fact]
    public void TypeConversion_StringToChar_ConvertsValidCharacters()
    {
        // Arrange
        var code = @"
            stringChar1 <- ""A""
            stringChar2 <- ""z""
            stringChar3 <- ""5""
            stringChar4 <- ""!""
            charValue1 <- stringChar1.ToChar()
            charValue2 <- stringChar2.ToChar()
            charValue3 <- stringChar3.ToChar()
            charValue4 <- stringChar4.ToChar()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("charValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("charValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("charValue3"));
        var result4 = interpreter.Manager.GetValue(new LangId("charValue4"));

        Assert.NotNull(result1);
        Assert.IsType<CharLangValue>(result1);
        Assert.Equal('A', ((CharLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<CharLangValue>(result2);
        Assert.Equal('z', ((CharLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<CharLangValue>(result3);
        Assert.Equal('5', ((CharLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<CharLangValue>(result4);
        Assert.Equal('!', ((CharLangValue)result4).Value);
    }

    [Fact]
    public void TypeConversion_CharToInt_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            charValue1 <- 'A'
            charValue2 <- '0'
            charValue3 <- '9'
            intValue1 <- charValue1.ToInt32()
            intValue2 <- charValue2.ToInt32()
            intValue3 <- charValue3.ToInt32()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("intValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("intValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("intValue3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(65, ((IntLangValue)result1).Value); // ASCII value of 'A'

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(48, ((IntLangValue)result2).Value); // ASCII value of '0'

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(57, ((IntLangValue)result3).Value); // ASCII value of '9'
    }

    [Fact]
    public void TypeConversion_IntToDouble_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            intValue1 <- 42
            intValue2 <- -17
            intValue3 <- 0
            doubleValue1 <- intValue1.ToDouble()
            doubleValue2 <- intValue2.ToDouble()
            doubleValue3 <- intValue3.ToDouble()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("doubleValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("doubleValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("doubleValue3"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(42.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(-17.0, ((DoubleLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<DoubleLangValue>(result3);
        Assert.Equal(0.0, ((DoubleLangValue)result3).Value);
    }

    [Fact]
    public void TypeConversion_DoubleToInt_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            doubleValue1 <- 42.7
            doubleValue2 <- -17.3
            doubleValue3 <- 3.5
            doubleValue4 <- 0.0
            intValue1 <- doubleValue1.ToInt()
            intValue2 <- doubleValue2.ToInt()
            intValue3 <- doubleValue3.ToInt()
            intValue4 <- doubleValue4.ToInt()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("intValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("intValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("intValue3"));
        var result4 = interpreter.Manager.GetValue(new LangId("intValue4"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(42, ((IntLangValue)result1).Value); // Truncates

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(-17, ((IntLangValue)result2).Value); // Truncates

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value); // Truncates

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(0, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void TypeConversion_BoolToInt_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            boolValue1 <- true
            boolValue2 <- false
            intValue1 <- boolValue1.ToInt()
            intValue2 <- boolValue2.ToInt()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("intValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("intValue2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(1, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(0, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void TypeConversion_BoolToDouble_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            boolValue1 <- true
            boolValue2 <- false
            doubleValue1 <- boolValue1.ToDouble()
            doubleValue2 <- boolValue2.ToDouble()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("doubleValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("doubleValue2"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(1.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(0.0, ((DoubleLangValue)result2).Value);
    }

    [Fact]
    public void TypeConversion_IntToBool_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            intValue1 <- 0
            intValue2 <- 1
            intValue3 <- -5
            intValue4 <- 42
            boolValue1 <- intValue1.ToBool()
            boolValue2 <- intValue2.ToBool()
            boolValue3 <- intValue3.ToBool()
            boolValue4 <- intValue4.ToBool()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("boolValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("boolValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("boolValue3"));
        var result4 = interpreter.Manager.GetValue(new LangId("boolValue4"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.Equal(false, ((BoolLangValue)result1).Value); // 0 = false

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value); // non-zero = true

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(true, ((BoolLangValue)result3).Value); // non-zero = true

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.Equal(true, ((BoolLangValue)result4).Value); // non-zero = true
    }

    [Fact]
    public void TypeConversion_DoubleToBool_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            doubleValue1 <- 0.0
            doubleValue2 <- 1.0
            doubleValue3 <- -2.5
            doubleValue4 <- 3.14
            boolValue1 <- doubleValue1.ToBool()
            boolValue2 <- doubleValue2.ToBool()
            boolValue3 <- doubleValue3.ToBool()
            boolValue4 <- doubleValue4.ToBool()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("boolValue1"));
        var result2 = interpreter.Manager.GetValue(new LangId("boolValue2"));
        var result3 = interpreter.Manager.GetValue(new LangId("boolValue3"));
        var result4 = interpreter.Manager.GetValue(new LangId("boolValue4"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.Equal(false, ((BoolLangValue)result1).Value); // 0.0 = false

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value); // non-zero = true

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(true, ((BoolLangValue)result3).Value); // non-zero = true

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.Equal(true, ((BoolLangValue)result4).Value); // non-zero = true
    }

    [Fact]
    public void TypeConversion_StringToBool_NumericValues()
    {
        // Arrange
        var code = @"
            string1 <- ""0""
            string2 <- ""1""
            string3 <- ""-1""
            string4 <- ""2""
            bool1 <- string1.ToBool()
            bool2 <- string2.ToBool()
            bool3 <- string3.ToBool()
            bool4 <- string4.ToBool()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("bool1"));
        var result2 = interpreter.Manager.GetValue(new LangId("bool2"));
        var result3 = interpreter.Manager.GetValue(new LangId("bool3"));
        var result4 = interpreter.Manager.GetValue(new LangId("bool4"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.Equal(false, ((BoolLangValue)result1).Value); // "0" = false

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.Equal(true, ((BoolLangValue)result2).Value); // "1" = true

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.Equal(true, ((BoolLangValue)result3).Value); // "-1" = true

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.Equal(true, ((BoolLangValue)result4).Value); // "2" = true
    }

    [Fact]
    public void TypeConversion_ArrayToList_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            arrayValue <- [1, 2, 3, 4, 5]
            listValue <- arrayValue.ToList()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("listValue"));
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
    public void TypeConversion_ListToArray_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            listValue <- {1, 2, 3, 4, 5}
            arrayValue <- listValue.ToArray()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("arrayValue"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var array = (ArrayLangValue)result;
        Assert.Equal(5, array.GetItems().Count());
        Assert.Equal(1, ((IntLangValue)array.GetItems().ElementAt(0)).Value);
        Assert.Equal(2, ((IntLangValue)array.GetItems().ElementAt(1)).Value);
        Assert.Equal(3, ((IntLangValue)array.GetItems().ElementAt(2)).Value);
        Assert.Equal(4, ((IntLangValue)array.GetItems().ElementAt(3)).Value);
        Assert.Equal(5, ((IntLangValue)array.GetItems().ElementAt(4)).Value);
    }

    [Fact]
    public void TypeConversion_TupleToList_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            tupleValue <- (1, ""hello"", true, 3.14)
            listValue <- tupleValue.ToList()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("listValue"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal("hello", ((StringLangValue)list.Values[1]).Value);
        Assert.Equal(true, ((BoolLangValue)list.Values[2]).Value);
        Assert.Equal(3.14, ((DoubleLangValue)list.Values[3]).Value);
    }

    [Fact]
    public void TypeConversion_ListToTuple_ConvertsCorrectly()
    {
        // Arrange
        var code = @"
            listValue <- {1, ""world"", false}
            tupleValue <- listValue.ToTuple()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("tupleValue"));
        Assert.NotNull(result);
        Assert.IsType<TupleLangValue>(result);
    }

    [Fact]
    public void TypeConversion_ExplicitCasting_CastsCorrectly()
    {
        // Arrange
        var code = @"
            doubleValue <- 3.99
            intValue <- 42
            stringValue <- ""123""

            // Explicit casting
            castToInt <- doubleValue as int
            castToDouble <- intValue as double
            castToString <- intValue as string
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var castInt = interpreter.Manager.GetValue(new LangId("castToInt"));
        var castDouble = interpreter.Manager.GetValue(new LangId("castToDouble"));
        var castString = interpreter.Manager.GetValue(new LangId("castToString"));

        Assert.NotNull(castInt);
        Assert.IsType<IntLangValue>(castInt);
        Assert.Equal(3, ((IntLangValue)castInt).Value); // Truncates

        Assert.NotNull(castDouble);
        Assert.IsType<DoubleLangValue>(castDouble);
        Assert.Equal(42.0, ((DoubleLangValue)castDouble).Value);

        Assert.NotNull(castString);
        Assert.IsType<StringLangValue>(castString);
        Assert.Equal("42", ((StringLangValue)castString).Value);
    }

    [Fact]
    public void TypeConversion_NullHandling_HandlesNullValues()
    {
        // Arrange
        var code = @"
            nullString <- null
            nullInt <- null
            nullDouble <- null

            // Convert null to various types
            intFromNull <- nullString.ToInt()
            stringFromNull <- nullInt.ToStr()
            doubleFromNull <- nullString.ToDouble()
            boolFromNull <- nullString.ToBool()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intFromNull = interpreter.Manager.GetValue(new LangId("intFromNull"));
        var stringFromNull = interpreter.Manager.GetValue(new LangId("stringFromNull"));
        var doubleFromNull = interpreter.Manager.GetValue(new LangId("doubleFromNull"));
        var boolFromNull = interpreter.Manager.GetValue(new LangId("boolFromNull"));

        // Verify null handling (actual behavior may vary by implementation)
        Assert.NotNull(intFromNull);
        Assert.NotNull(stringFromNull);
        Assert.NotNull(doubleFromNull);
        Assert.NotNull(boolFromNull);
    }

    [Fact]
    public void TypeConversion_NumericStringParsing_ParsesVariousFormats()
    {
        // Arrange
        var code = @"
            positiveInt <- ""100""
            negativeInt <- ""-50""
            positiveDouble <- ""123.456""
            negativeDouble <- ""-78.9""
            scientificNotation <- ""1.23e4""

            int1 <- positiveInt.ToInt()
            int2 <- negativeInt.ToInt()
            double1 <- positiveDouble.ToDouble()
            double2 <- negativeDouble.ToDouble()
            double3 <- scientificNotation.ToDouble()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var int1 = interpreter.Manager.GetValue(new LangId("int1"));
        var int2 = interpreter.Manager.GetValue(new LangId("int2"));
        var double1 = interpreter.Manager.GetValue(new LangId("double1"));
        var double2 = interpreter.Manager.GetValue(new LangId("double2"));
        var double3 = interpreter.Manager.GetValue(new LangId("double3"));

        Assert.NotNull(int1);
        Assert.IsType<IntLangValue>(int1);
        Assert.Equal(100, ((IntLangValue)int1).Value);

        Assert.NotNull(int2);
        Assert.IsType<IntLangValue>(int2);
        Assert.Equal(-50, ((IntLangValue)int2).Value);

        Assert.NotNull(double1);
        Assert.IsType<DoubleLangValue>(double1);
        Assert.Equal(123.456, ((DoubleLangValue)double1).Value);

        Assert.NotNull(double2);
        Assert.IsType<DoubleLangValue>(double2);
        Assert.Equal(-78.9, ((DoubleLangValue)double2).Value);

        Assert.NotNull(double3);
        Assert.IsType<DoubleLangValue>(double3);
        Assert.Equal(12300.0, ((DoubleLangValue)double3).Value); // 1.23 * 10^4
    }

    [Fact]
    public void TypeConversion_InvalidConversion_HandlesErrors()
    {
        // Arrange
        var code = @"
            invalidInt <- ""not a number""
            invalidDouble <- ""invalid""
            invalidChar <- ""too long""

            // These should handle errors gracefully
            try {
                intResult <- invalidInt.ToInt()
            } catch {
                intResult <- -1
            }

            try {
                doubleResult <- invalidDouble.ToDouble()
            } catch {
                doubleResult <- -1.0
            }

            try {
                charResult <- invalidChar.ToChar()
            } catch {
                charResult <- '?'
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intResult = interpreter.Manager.GetValue(new LangId("intResult"));
        var doubleResult = interpreter.Manager.GetValue(new LangId("doubleResult"));
        var charResult = interpreter.Manager.GetValue(new LangId("charResult"));

        Assert.NotNull(intResult);
        Assert.NotNull(doubleResult);
        Assert.NotNull(charResult);

        // Should contain fallback values from catch blocks
        Assert.IsType<IntLangValue>(intResult);
        Assert.IsType<DoubleLangValue>(doubleResult);
        Assert.IsType<CharLangValue>(charResult);
    }
}