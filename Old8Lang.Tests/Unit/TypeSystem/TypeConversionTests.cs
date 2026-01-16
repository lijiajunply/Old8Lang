using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Unit.TypeSystem;

[Collection("Sequential")]
public class TypeConversionTests
{
    [Fact]
    public void LangValueType_ObjToValue_Null_ReturnsNullLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue(null);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NullLangValue>(result);
    }

    [Fact]
    public void LangValueType_ObjToValue_Int_ReturnsIntLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue(42);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void LangValueType_ObjToValue_String_ReturnsStringLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue("test");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("test", ((StringLangValue)result).Value);
    }

    [Fact]
    public void LangValueType_ObjToValue_Double_ReturnsDoubleLangValue()
    {
        // Act
        var result = LangValueType.ObjToValue(3.14);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void LangValueType_ObjToValue_Bool_ReturnsBoolLangValue()
    {
        // Act
        var result1 = LangValueType.ObjToValue(true);
        var result2 = LangValueType.ObjToValue(false);

        // Assert
        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.False(((BoolLangValue)result2).Value);
    }

    [Fact]
    public void IntLangValue_Plus_Int_ReturnsIntLangValue()
    {
        // Arrange
        var a = new IntLangValue(10);
        var b = new IntLangValue(20);

        // Act
        var result = a.Plus(b);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void IntLangValue_Minus_Int_ReturnsIntLangValue()
    {
        // Arrange
        var a = new IntLangValue(30);
        var b = new IntLangValue(10);

        // Act
        var result = a.Minus(b);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value);
    }

    [Fact]
    public void IntLangValue_Times_Int_ReturnsIntLangValue()
    {
        // Arrange
        var a = new IntLangValue(5);
        var b = new IntLangValue(6);

        // Act
        var result = a.Times(b);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void StringLangValue_Plus_String_ReturnsStringLangValue()
    {
        // Arrange
        var a = new StringLangValue("hello");
        var b = new StringLangValue(" world");

        // Act
        var result = a.Plus(b);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("hello world", ((StringLangValue)result).Value);
    }

    [Fact]
    public void BoolLangValue_Equal_Bool_ReturnsTrueForSameValues()
    {
        // Arrange
        var a = new BoolLangValue(true);
        var b = new BoolLangValue(true);

        // Act
        var result = a.Equal(b);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void BoolLangValue_Equal_Bool_ReturnsFalseForDifferentValues()
    {
        // Arrange
        var a = new BoolLangValue(true);
        var b = new BoolLangValue(false);

        // Act
        var result = a.Equal(b);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IntLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new IntLangValue(42);

        // Act
        var result = value.TypeToString();

        // Assert
        Assert.Equal("Int", result);
    }

    [Fact]
    public void StringLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new StringLangValue("test");

        // Act
        var result = value.TypeToString();

        // Assert
        Assert.Equal("String", result);
    }

    [Fact]
    public void DoubleLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new DoubleLangValue(3.14);

        // Act
        var result = value.TypeToString();

        // Assert
        Assert.Equal("Double", result);
    }

    [Fact]
    public void BoolLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new BoolLangValue(true);

        // Act
        var result = value.TypeToString();

        // Assert
        Assert.Equal("Bool", result);
    }

    [Fact]
    public void NullLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new NullLangValue();

        // Act
        var result = value.TypeToString();

        // Assert
        Assert.Equal("Null", result);
    }

    [Fact]
    public void TypeLangValue_TypeToString_ReturnsCorrectString()
    {
        // Arrange
        var value = new TypeLangValue("testType");

        // Act
        var result = value.TypeToString();

        // Assert
        Assert.Equal("Type", result);
    }

    #region ValueToObj 和 ObjToValue 往返转换测试

    [Fact]
    public void ValueToObj_IntLangValue_ReturnsInt()
    {
        // Arrange
        var langValue = new IntLangValue(42);

        // Act
        var result = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<int>(result);
        Assert.Equal(42, result);
    }

    [Fact]
    public void ValueToObj_StringLangValue_ReturnsString()
    {
        // Arrange
        var langValue = new StringLangValue("test");

        // Act
        var result = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<string>(result);
        Assert.Equal("test", result);
    }

    [Fact]
    public void ValueToObj_DoubleLangValue_ReturnsDouble()
    {
        // Arrange
        var langValue = new DoubleLangValue(3.14);

        // Act
        var result = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<double>(result);
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void ValueToObj_BoolLangValue_ReturnsBool()
    {
        // Arrange
        var trueLangValue = new BoolLangValue(true);
        var falseLangValue = new BoolLangValue(false);

        // Act
        var trueResult = LangValueType.ValueToObj(trueLangValue);
        var falseResult = LangValueType.ValueToObj(falseLangValue);

        // Assert
        Assert.NotNull(trueResult);
        Assert.IsType<bool>(trueResult);
        Assert.True((bool)trueResult);

        Assert.NotNull(falseResult);
        Assert.IsType<bool>(falseResult);
        Assert.False((bool)falseResult);
    }

    [Fact]
    public void ValueToObj_NullLangValue_ReturnsNull()
    {
        // Arrange
        var langValue = NullLangValue.Instance;

        // Act
        var result = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValueToObj_ListLangValue_ReturnsList()
    {
        // Arrange - 使用 List<LangExpression> 构造函数明确避免二义性
        var listVal = new ListLangValue(new List<LangExpression>());
        listVal.Values.Add(new IntLangValue(1));
        listVal.Values.Add(new IntLangValue(2));
        listVal.Values.Add(new IntLangValue(3));

        // Act
        var result = LangValueType.ValueToObj(listVal);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<object>>(result);
        var list = (List<object>)result;
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void ValueToObj_ArrayLangValue_ReturnsArray()
    {
        // Arrange
        var arrayVal = new ArrayLangValue([1, 2, 3]);
        // 模拟运行结果
        var field = typeof(ArrayLangValue).GetField("_runResult",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(arrayVal, new[]
        {
            new IntLangValue(1),
            new IntLangValue(2),
            new IntLangValue(3)
        });

        // Act
        var result = LangValueType.ValueToObj(arrayVal);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<object[]>(result);
        var array = (object[])result;
        Assert.Equal(3, array.Length);
        Assert.Equal(1, array[0]);
        Assert.Equal(2, array[1]);
        Assert.Equal(3, array[2]);
    }

    [Fact]
    public void ValueToObj_DictionaryLangValue_ReturnsDictionary()
    {
        // Arrange
        var dictVal = new DictionaryLangValue();
        dictVal.Value.Add((new StringLangValue("key1"), new IntLangValue(100)));
        dictVal.Value.Add((new StringLangValue("key2"), new IntLangValue(200)));

        // Act
        var result = LangValueType.ValueToObj(dictVal);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Dictionary<object, object>>(result);
        var dict = (Dictionary<object, object>)result;
        Assert.Equal(2, dict.Count);
        Assert.Equal(100, dict["key1"]);
        Assert.Equal(200, dict["key2"]);
    }

    [Fact]
    public void ValueToObj_TupleLangValue_ReturnsTuple()
    {
        // Arrange - 创建元组并通过 ObjToValue 方式来测试
        var originalTuple = new Tuple<object, object>(1, "test");
        var langTuple = LangValueType.ObjToValue(originalTuple) as TupleLangValue;

        Assert.NotNull(langTuple);

        // Act
        var result = LangValueType.ValueToObj(langTuple);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Tuple<object, object>>(result);
        var tuple = (Tuple<object, object>)result;
        Assert.Equal(1, tuple.Item1);
        Assert.Equal("test", tuple.Item2);
    }

    [Fact]
    public void RoundTrip_Int_MaintainsValue()
    {
        // Arrange
        var originalValue = 42;

        // Act
        var langValue = LangValueType.ObjToValue(originalValue);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<int>(roundTripValue);
        Assert.Equal(originalValue, roundTripValue);
    }

    [Fact]
    public void RoundTrip_String_MaintainsValue()
    {
        // Arrange
        var originalValue = "test string";

        // Act
        var langValue = LangValueType.ObjToValue(originalValue);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<string>(roundTripValue);
        Assert.Equal(originalValue, roundTripValue);
    }

    [Fact]
    public void RoundTrip_Double_MaintainsValue()
    {
        // Arrange
        var originalValue = 3.14159;

        // Act
        var langValue = LangValueType.ObjToValue(originalValue);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<double>(roundTripValue);
        Assert.Equal(originalValue, roundTripValue);
    }

    [Fact]
    public void RoundTrip_Bool_MaintainsValue()
    {
        // Arrange
        var originalValue = true;

        // Act
        var langValue = LangValueType.ObjToValue(originalValue);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<bool>(roundTripValue);
        Assert.Equal(originalValue, roundTripValue);
    }

    [Fact]
    public void RoundTrip_Null_MaintainsValue()
    {
        // Arrange
        object? originalValue = null;

        // Act
        var langValue = LangValueType.ObjToValue(originalValue);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.Null(roundTripValue);
    }

    [Fact]
    public void RoundTrip_List_MaintainsStructure()
    {
        // Arrange
        var originalList = new List<object> { 1, "test", 3.14 };

        // Act
        var langValue = LangValueType.ObjToValue(originalList);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<List<object>>(roundTripValue);
        var resultList = (List<object>)roundTripValue;
        Assert.Equal(3, resultList.Count);
        Assert.Equal(1, resultList[0]);
        Assert.Equal("test", resultList[1]);
        Assert.Equal(3.14, resultList[2]);
    }

    [Fact]
    public void RoundTrip_Dictionary_MaintainsStructure()
    {
        // Arrange
        var originalDict = new Dictionary<object, object>
        {
            { "key1", 100 },
            { "key2", "value2" },
            { 3, 3.14 }
        };

        // Act
        var langValue = LangValueType.ObjToValue(originalDict);

        // 需要运行字典以填充 Value 字段
        if (langValue is DictionaryLangValue dictVal)
        {
            foreach (var tuple in dictVal.Tuples)
            {
                var v0 = tuple.Get(0);
                var v1 = tuple.Get(1);
                var key = LangValueType.ObjToValue(v0);
                var value = LangValueType.ObjToValue(v1);
                dictVal.Value.Add((key, value));
            }
        }

        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<Dictionary<object, object>>(roundTripValue);
        var resultDict = (Dictionary<object, object>)roundTripValue;
        Assert.Equal(3, resultDict.Count);
        Assert.Equal(100, resultDict["key1"]);
        Assert.Equal("value2", resultDict["key2"]);
        Assert.Equal(3.14, resultDict[3]);
    }

    [Fact]
    public void RoundTrip_Tuple_MaintainsStructure()
    {
        // Arrange
        var originalTuple = new Tuple<object, object>(42, "test");

        // Act
        var langValue = LangValueType.ObjToValue(originalTuple);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<Tuple<object, object>>(roundTripValue);
        var resultTuple = (Tuple<object, object>)roundTripValue;
        Assert.Equal(42, resultTuple.Item1);
        Assert.Equal("test", resultTuple.Item2);
    }

    [Fact]
    public void RoundTrip_NestedList_MaintainsStructure()
    {
        // Arrange
        var originalList = new List<object>
        {
            1,
            new List<object> { 2, 3 },
            new List<object> { "a", "b" }
        };

        // Act
        var langValue = LangValueType.ObjToValue(originalList);
        var roundTripValue = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripValue);
        Assert.IsType<List<object>>(roundTripValue);
        var resultList = (List<object>)roundTripValue;
        Assert.Equal(3, resultList.Count);
        Assert.Equal(1, resultList[0]);

        Assert.IsType<List<object>>(resultList[1]);
        var nestedList1 = (List<object>)resultList[1];
        Assert.Equal(2, nestedList1.Count);
        Assert.Equal(2, nestedList1[0]);
        Assert.Equal(3, nestedList1[1]);

        Assert.IsType<List<object>>(resultList[2]);
        var nestedList2 = (List<object>)resultList[2];
        Assert.Equal(2, nestedList2.Count);
        Assert.Equal("a", nestedList2[0]);
        Assert.Equal("b", nestedList2[1]);
    }

    #endregion

    #region 自定义类型转换测试

    /// <summary>
    /// 测试用的自定义类
    /// </summary>
    public class TestCustomClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Value { get; set; }

        public string GetDescription()
        {
            return $"TestCustomClass(Id={Id}, Name={Name}, Value={Value})";
        }

        public int Add(int a, int b)
        {
            return a + b;
        }
    }

    [Fact]
    public void ObjToValue_CustomClass_ReturnsNativeAnyLangValue()
    {
        // Arrange
        var customObj = new TestCustomClass
        {
            Id = 1,
            Name = "Test",
            Value = 3.14
        };

        // Act
        var result = LangValueType.ObjToValue(customObj);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Old8Lang.AST.Expression.Intermediates.NativeAnyLangValue>(result);

        var nativeValue = (Old8Lang.AST.Expression.Intermediates.NativeAnyLangValue)result;
        Assert.Same(customObj, nativeValue.GetNativeObject());
        Assert.Equal(typeof(TestCustomClass), nativeValue.GetNativeType());
    }

    [Fact]
    public void RoundTrip_CustomClass_MaintainsReference()
    {
        // Arrange
        var originalObj = new TestCustomClass
        {
            Id = 42,
            Name = "Original",
            Value = 2.718
        };

        // Act
        var langValue = LangValueType.ObjToValue(originalObj);
        var roundTripObj = LangValueType.ValueToObj(langValue);

        // Assert
        Assert.NotNull(roundTripObj);
        Assert.IsType<TestCustomClass>(roundTripObj);
        var result = (TestCustomClass)roundTripObj;

        // 验证是同一个引用
        Assert.Same(originalObj, result);

        // 验证属性值
        Assert.Equal(42, result.Id);
        Assert.Equal("Original", result.Name);
        Assert.Equal(2.718, result.Value);
    }

    [Fact]
    public void NativeAnyLangValue_AccessProperty_ReturnsCorrectValue()
    {
        // Arrange
        var customObj = new TestCustomClass
        {
            Id = 100,
            Name = "PropertyTest",
            Value = 1.23
        };
        var langValue = LangValueType.ObjToValue(customObj) as Old8Lang.AST.Expression.Intermediates.NativeAnyLangValue;

        Assert.NotNull(langValue);

        // Act - 访问属性
        var idValue = langValue.Dot(new LangId("Id"), new Old8Lang.Interpreter.VariateManager());
        var nameValue = langValue.Dot(new LangId("Name"), new Old8Lang.Interpreter.VariateManager());
        var valueValue = langValue.Dot(new LangId("Value"), new Old8Lang.Interpreter.VariateManager());

        // Assert
        Assert.IsType<IntLangValue>(idValue);
        Assert.Equal(100, ((IntLangValue)idValue).Value);

        Assert.IsType<StringLangValue>(nameValue);
        Assert.Equal("PropertyTest", ((StringLangValue)nameValue).Value);

        Assert.IsType<DoubleLangValue>(valueValue);
        Assert.Equal(1.23, ((DoubleLangValue)valueValue).Value);
    }

    [Fact]
    public void NativeAnyLangValue_CallMethod_ReturnsCorrectResult()
    {
        // Arrange
        var customObj = new TestCustomClass { Id = 1, Name = "Test", Value = 1.0 };
        var langValue = LangValueType.ObjToValue(customObj) as Old8Lang.AST.Expression.Intermediates.NativeAnyLangValue;

        Assert.NotNull(langValue);

        var manager = new Old8Lang.Interpreter.VariateManager();

        // Act - 调用无参数方法
        var descInstance = new Instance(new LangId("GetDescription"), new List<LangExpression>());
        var descResult = langValue.Dot(descInstance, manager);

        // Assert
        Assert.IsType<StringLangValue>(descResult);
        var descStr = ((StringLangValue)descResult).Value;
        Assert.Contains("TestCustomClass", descStr);
        Assert.Contains("Id=1", descStr);
        Assert.Contains("Name=Test", descStr);

        // Act - 调用带参数方法
        var addInstance = new Instance(
            new LangId("Add"),
            new List<LangExpression> { new IntLangValue(10), new IntLangValue(20) }
        );
        var addResult = langValue.Dot(addInstance, manager);

        // Assert
        Assert.IsType<IntLangValue>(addResult);
        Assert.Equal(30, ((IntLangValue)addResult).Value);
    }

    [Fact]
    public void ObjToValue_ListOfCustomClasses_ConvertsCorrectly()
    {
        // Arrange
        var customList = new List<object>
        {
            new TestCustomClass { Id = 1, Name = "First", Value = 1.0 },
            new TestCustomClass { Id = 2, Name = "Second", Value = 2.0 }
        };

        // Act
        var langValue = LangValueType.ObjToValue(customList);

        // Assert
        Assert.NotNull(langValue);
        Assert.IsType<ListLangValue>(langValue);

        var listVal = (ListLangValue)langValue;
        Assert.Equal(2, listVal.Values.Count);

        Assert.IsType<Old8Lang.AST.Expression.Intermediates.NativeAnyLangValue>(listVal.Values[0]);
        Assert.IsType<Old8Lang.AST.Expression.Intermediates.NativeAnyLangValue>(listVal.Values[1]);
    }

    #endregion
}