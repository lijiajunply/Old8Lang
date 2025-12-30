using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.Tests.Unit.TypeSystem;

/// <summary>
/// 联合类型和交叉类型的类型系统测试
/// </summary>
[Collection("Sequential")]
public class UnionIntersectionTypeAnnotationTests
{
    #region 联合类型解析测试

    /// <summary>
    /// 测试简单联合类型解析
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_SimpleUnion_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "int | string";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.NotNull(parsed);
        Assert.True(parsed.IsUnion);
        Assert.Equal("union", parsed.BaseType);
        Assert.NotNull(parsed.GenericArguments);
        Assert.Equal(2, parsed.GenericArguments.Count);
        Assert.Equal("int", parsed.GenericArguments[0].BaseType);
        Assert.Equal("string", parsed.GenericArguments[1].BaseType);
        Assert.Equal("int | string", parsed.GetFullName());
    }

    /// <summary>
    /// 测试多类型联合解析
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_MultipleTypesUnion_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "int | string | bool | double";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.True(parsed.IsUnion);
        Assert.Equal(4, parsed.GenericArguments!.Count);
        Assert.Equal("int", parsed.GenericArguments[0].BaseType);
        Assert.Equal("string", parsed.GenericArguments[1].BaseType);
        Assert.Equal("bool", parsed.GenericArguments[2].BaseType);
        Assert.Equal("double", parsed.GenericArguments[3].BaseType);
        Assert.Equal("int | string | bool | double", parsed.GetFullName());
    }

    /// <summary>
    /// 测试可空联合类型解析
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_NullableUnion_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "int? | string?";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.True(parsed.IsUnion);
        Assert.Equal(2, parsed.GenericArguments!.Count);
        Assert.Equal("int", parsed.GenericArguments[0].BaseType);
        Assert.True(parsed.GenericArguments[0].IsNullable);
        Assert.Equal("string", parsed.GenericArguments[1].BaseType);
        Assert.True(parsed.GenericArguments[1].IsNullable);
        Assert.Equal("int? | string?", parsed.GetFullName());
    }

    /// <summary>
    /// 测试泛型中的联合类型解析
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_UnionInGeneric_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "List<int | string>";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.Equal("List", parsed.BaseType);
        Assert.True(parsed.IsGeneric);
        Assert.NotNull(parsed.GenericArguments);
        Assert.Single(parsed.GenericArguments);

        var innerType = parsed.GenericArguments[0];
        Assert.True(innerType.IsUnion);
        Assert.Equal(2, innerType.GenericArguments!.Count);
        Assert.Equal("int", innerType.GenericArguments[0].BaseType);
        Assert.Equal("string", innerType.GenericArguments[1].BaseType);
        Assert.Equal("List<int | string>", parsed.GetFullName());
    }

    /// <summary>
    /// 测试嵌套泛型联合类型解析
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_NestedGenericUnion_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "Map<string, int | string>";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.Equal("Map", parsed.BaseType);
        Assert.True(parsed.IsGeneric);
        Assert.Equal(2, parsed.GenericArguments!.Count);
        Assert.Equal("string", parsed.GenericArguments[0].BaseType);

        var valueType = parsed.GenericArguments[1];
        Assert.True(valueType.IsUnion);
        Assert.Equal("int", valueType.GenericArguments![0].BaseType);
        Assert.Equal("string", valueType.GenericArguments[1].BaseType);
        Assert.Equal("Map<string, int | string>", parsed.GetFullName());
    }

    #endregion

    #region 交叉类型解析测试

    /// <summary>
    /// 测试简单交叉类型解析
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_SimpleIntersection_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "IComparable & ICloneable";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.NotNull(parsed);
        Assert.True(parsed.IsIntersection);
        Assert.Equal("intersection", parsed.BaseType);
        Assert.NotNull(parsed.GenericArguments);
        Assert.Equal(2, parsed.GenericArguments.Count);
        Assert.Equal("IComparable", parsed.GenericArguments[0].BaseType);
        Assert.Equal("ICloneable", parsed.GenericArguments[1].BaseType);
        Assert.Equal("IComparable & ICloneable", parsed.GetFullName());
    }

    /// <summary>
    /// 测试多接口交叉类型解析
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_MultipleInterfacesIntersection_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "IA & IB & IC";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.True(parsed.IsIntersection);
        Assert.Equal(3, parsed.GenericArguments!.Count);
        Assert.Equal("IA", parsed.GenericArguments[0].BaseType);
        Assert.Equal("IB", parsed.GenericArguments[1].BaseType);
        Assert.Equal("IC", parsed.GenericArguments[2].BaseType);
        Assert.Equal("IA & IB & IC", parsed.GetFullName());
    }

    #endregion

    #region 联合类型兼容性测试

    /// <summary>
    /// 测试联合类型兼容于其成员类型
    /// </summary>
    [Fact]
    public void ValidateTypeCompatibility_UnionToMember_ReturnsTrue()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert - A|B 兼容于 A
        Assert.True(manager.ValidateTypeCompatibility("int", "int | string"));
        Assert.True(manager.ValidateTypeCompatibility("string", "int | string"));
        Assert.True(manager.ValidateTypeCompatibility("bool", "int | string | bool"));
    }

    /// <summary>
    /// 测试成员类型兼容于联合类型
    /// </summary>
    [Fact]
    public void ValidateTypeCompatibility_MemberToUnion_ReturnsTrue()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert - A 兼容于 A|B
        Assert.True(manager.ValidateTypeCompatibility("int | string", "int"));
        Assert.True(manager.ValidateTypeCompatibility("int | string", "string"));
        Assert.True(manager.ValidateTypeCompatibility("int | string | bool", "bool"));
    }

    /// <summary>
    /// 测试不兼容的联合类型
    /// </summary>
    [Fact]
    public void ValidateTypeCompatibility_IncompatibleUnion_ReturnsFalse()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert
        Assert.False(manager.ValidateTypeCompatibility("int | string", "bool"));
        Assert.False(manager.ValidateTypeCompatibility("double | char", "int | string"));
    }

    /// <summary>
    /// 测试 null 兼容于可空联合类型
    /// </summary>
    [Fact]
    public void ValidateTypeCompatibility_NullToNullableUnion_ReturnsTrue()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert
        Assert.True(manager.ValidateTypeCompatibility("int? | string?", "null"));
        Assert.True(manager.ValidateTypeCompatibility("double? | bool?", "null"));
    }

    /// <summary>
    /// 测试可空类型成员兼容于可空联合类型
    /// </summary>
    [Fact]
    public void ValidateTypeCompatibility_NullableMemberToNullableUnion_ReturnsTrue()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert
        Assert.True(manager.ValidateTypeCompatibility("int? | string?", "int"));
        Assert.True(manager.ValidateTypeCompatibility("int? | string?", "string"));
    }

    #endregion

    #region 交叉类型兼容性测试

    /// <summary>
    /// 测试交叉类型兼容于其所有成员
    /// </summary>
    [Fact]
    public void ValidateTypeCompatibility_IntersectionToMembers_ReturnsTrue()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert - A&B 兼容于 A 和 B
        Assert.True(manager.ValidateTypeCompatibility("IComparable", "IComparable & ICloneable"));
        Assert.True(manager.ValidateTypeCompatibility("ICloneable", "IComparable & ICloneable"));
    }

    /// <summary>
    /// 测试单个类型不兼容于交叉类型
    /// </summary>
    [Fact]
    public void ValidateTypeCompatibility_SingleTypeToIntersection_ReturnsFalse()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert - A 不兼容于 A&B
        Assert.False(manager.ValidateTypeCompatibility("IComparable & ICloneable", "IComparable"));
        Assert.False(manager.ValidateTypeCompatibility("IComparable & ICloneable", "ICloneable"));
    }

    #endregion

    #region 混合类型测试

    /// <summary>
    /// 测试联合类型和交叉类型的组合
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_UnionAndIntersectionCombination_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // 测试联合类型
        var union = manager.ParseTypeAnnotation("int | string");
        Assert.True(union.IsUnion);

        // 测试交叉类型
        var intersection = manager.ParseTypeAnnotation("IA & IB");
        Assert.True(intersection.IsIntersection);
    }

    /// <summary>
    /// 测试泛型约束中的交叉类型
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_IntersectionInGenericConstraint_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "IComparable & ISerializable";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.True(parsed.IsIntersection);
        Assert.Equal(2, parsed.GenericArguments!.Count);
        Assert.Equal("IComparable", parsed.GenericArguments[0].BaseType);
        Assert.Equal("ISerializable", parsed.GenericArguments[1].BaseType);
    }

    #endregion

    #region IsTypeCompatible 方法测试

    /// <summary>
    /// 测试 IsTypeCompatible 方法处理联合类型
    /// </summary>
    [Fact]
    public void IsTypeCompatible_WithUnionTypes_ReturnsCorrectResult()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert
        Assert.True(manager.IsTypeCompatible("int | string", "int"));
        Assert.True(manager.IsTypeCompatible("int", "int | string"));
        Assert.False(manager.IsTypeCompatible("bool", "int | string"));
    }

    /// <summary>
    /// 测试 IsTypeCompatible 方法处理交叉类型
    /// </summary>
    [Fact]
    public void IsTypeCompatible_WithIntersectionTypes_ReturnsCorrectResult()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act & Assert
        Assert.True(manager.IsTypeCompatible("IA & IB", "IA"));
        Assert.True(manager.IsTypeCompatible("IA & IB", "IB"));
        Assert.False(manager.IsTypeCompatible("IA", "IA & IB"));
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试空字符串类型注解
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_EmptyString_ReturnsAnyType()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act
        var parsed = manager.ParseTypeAnnotation("");

        // Assert
        Assert.Equal("any", parsed.BaseType);
    }

    /// <summary>
    /// 测试只有空格的类型注解
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_WhitespaceOnly_ReturnsAnyType()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();

        // Act
        var parsed = manager.ParseTypeAnnotation("   ");

        // Assert
        Assert.Equal("any", parsed.BaseType);
    }

    /// <summary>
    /// 测试带有额外空格的联合类型
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_UnionWithWhitespace_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "  int  |  string  ";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.True(parsed.IsUnion);
        Assert.Equal("int", parsed.GenericArguments![0].BaseType);
        Assert.Equal("string", parsed.GenericArguments[1].BaseType);
    }

    /// <summary>
    /// 测试带有额外空格的交叉类型
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_IntersectionWithWhitespace_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "  IA  &  IB  ";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.True(parsed.IsIntersection);
        Assert.Equal("IA", parsed.GenericArguments![0].BaseType);
        Assert.Equal("IB", parsed.GenericArguments[1].BaseType);
    }

    #endregion

    #region 复杂场景测试

    /// <summary>
    /// 测试深度嵌套的泛型联合类型
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_DeeplyNestedGenericUnion_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "List<Map<string, int | string>>";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.Equal("List", parsed.BaseType);
        Assert.True(parsed.IsGeneric);

        var mapType = parsed.GenericArguments![0];
        Assert.Equal("Map", mapType.BaseType);
        Assert.True(mapType.IsGeneric);

        var valueType = mapType.GenericArguments![1];
        Assert.True(valueType.IsUnion);
        Assert.Equal("int | string", valueType.GetFullName());
    }

    /// <summary>
    /// 测试可空泛型联合类型
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_NullableGenericWithUnion_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "List<int | string>?";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.Equal("List", parsed.BaseType);
        Assert.True(parsed.IsNullable);
        Assert.True(parsed.IsGeneric);

        var innerType = parsed.GenericArguments![0];
        Assert.True(innerType.IsUnion);
    }

    /// <summary>
    /// 测试多个泛型参数都是联合类型
    /// </summary>
    [Fact]
    public void ParseTypeAnnotation_MultipleGenericUnionParameters_ParsesCorrectly()
    {
        // Arrange
        var manager = CreateTypeAnnotationManager();
        var typeAnnotation = "Map<int | string, bool | double>";

        // Act
        var parsed = manager.ParseTypeAnnotation(typeAnnotation);

        // Assert
        Assert.Equal("Map", parsed.BaseType);
        Assert.Equal(2, parsed.GenericArguments!.Count);

        var keyType = parsed.GenericArguments[0];
        Assert.True(keyType.IsUnion);
        Assert.Equal("int | string", keyType.GetFullName());

        var valueType = parsed.GenericArguments[1];
        Assert.True(valueType.IsUnion);
        Assert.Equal("bool | double", valueType.GetFullName());
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建 TypeAnnotationManager 实例
    /// </summary>
    private TypeAnnotationManager CreateTypeAnnotationManager()
    {
        var globalManager = new VariateManager();
        return new TypeAnnotationManager(globalManager);
    }

    #endregion
}
