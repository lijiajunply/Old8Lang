using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Reflection;

/// <summary>
/// TypeLangValue 反射系统测试（解释器模式）
/// 测试 TypeLangValue 与反射系统的集成
/// </summary>
public class TypeLangValueReflectionTests
{
    #region GetType 测试

    [Fact]
    public void GetType_ReturnsTypeLangValue()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
            }
            personType <- GetType(""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("personType"));
        Assert.NotNull(result);
        Assert.IsType<TypeLangValue>(result);
        Assert.Equal("Person", ((TypeLangValue)result).Value);
    }

    [Fact]
    public void GetType_WithNonExistentType_ThrowsError()
    {
        // Arrange
        var code = @"
            personType <- GetType(""NonExistentClass"")
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<Old8Lang.Error.InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    #endregion

    #region GetAllTypes 测试

    [Fact]
    public void GetAllTypes_ReturnsListOfTypes()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
            }
            class Animal {
                public species <- ""dog""
            }
            allTypes <- GetAllTypes()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("allTypes"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.True(list.Values.Count >= 2); // 至少包含 Person 和 Animal
    }

    #endregion

    #region TypeOf 测试

    [Fact]
    public void TypeOf_ReturnsCorrectType()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
            }
            person <- Person()
            personType <- TypeOf(person)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("personType"));
        Assert.NotNull(result);
        Assert.IsType<TypeLangValue>(result);
        Assert.Equal("Person", ((TypeLangValue)result).Value);
    }

    #endregion

    #region TypeLangValue.GetMethodNames 测试

    [Fact]
    public void TypeLangValue_GetMethodNames_ReturnsAllMethods()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a, b) {
                    return a + b
                }
                public func subtract(a, b) {
                    return a - b
                }
            }
            calcType <- GetType(""Calculator"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var typeValue = interpreter.Manager.GetValue(new LangId("calcType")) as TypeLangValue;
        Assert.NotNull(typeValue);

        var methods = typeValue.GetMethodNames(interpreter.Manager);

        // Assert
        Assert.NotNull(methods);
        Assert.Contains("add", methods);
        Assert.Contains("subtract", methods);
    }

    #endregion

    #region TypeLangValue.GetFieldNames 测试

    [Fact]
    public void TypeLangValue_GetFieldNames_ReturnsAllFields()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
                public age <- 0
                private id <- 123
            }
            personType <- GetType(""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var typeValue = interpreter.Manager.GetValue(new LangId("personType")) as TypeLangValue;
        Assert.NotNull(typeValue);

        var fields = typeValue.GetFieldNames(interpreter.Manager);

        // Assert
        Assert.NotNull(fields);
        Assert.Contains("name", fields);
        Assert.Contains("age", fields);
        Assert.Contains("id", fields);
    }

    #endregion

    #region TypeLangValue.CreateInstance 测试

    [Fact]
    public void TypeLangValue_CreateInstance_CreatesValidInstance()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""default""
                public func init(n) {
                    name <- n
                }
            }
            personType <- GetType(""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var typeValue = interpreter.Manager.GetValue(new LangId("personType")) as TypeLangValue;
        Assert.NotNull(typeValue);

        var nameArg = new Old8Lang.AST.Expression.Value.StringLangValue("Alice");
        var args = new List<LangExpression> { nameArg };
        var instance = typeValue.CreateInstance(interpreter.Manager, args);

        // Assert
        Assert.NotNull(instance);
        Assert.Equal("Person", instance.ClassId.IdName);
    }

    #endregion

    #region TypeLangValue.IsAssignableFrom 测试

    [Fact]
    public void TypeLangValue_IsAssignableFrom_WithSameType_ReturnsTrue()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
            }
            personType1 <- GetType(""Person"")
            personType2 <- GetType(""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var type1 = interpreter.Manager.GetValue(new LangId("personType1")) as TypeLangValue;
        var type2 = interpreter.Manager.GetValue(new LangId("personType2")) as TypeLangValue;
        Assert.NotNull(type1);
        Assert.NotNull(type2);

        var result = type1.IsAssignableFrom(type2, interpreter.Manager);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TypeLangValue_IsAssignableFrom_WithInheritance_ReturnsTrue()
    {
        // Arrange
        var code = @"
            class Animal {
                public species <- ""unknown""
            }
            class Dog extends Animal {
                public breed <- ""unknown""
            }
            animalType <- GetType(""Animal"")
            dogType <- GetType(""Dog"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var animalType = interpreter.Manager.GetValue(new LangId("animalType")) as TypeLangValue;
        var dogType = interpreter.Manager.GetValue(new LangId("dogType")) as TypeLangValue;
        Assert.NotNull(animalType);
        Assert.NotNull(dogType);

        var result = animalType.IsAssignableFrom(dogType, interpreter.Manager);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region TypeLangValue.GetBaseType 测试

    [Fact]
    public void TypeLangValue_GetBaseType_ReturnsParentType()
    {
        // Arrange
        var code = @"
            class Animal {
                public species <- ""unknown""
            }
            class Dog extends Animal {
                public breed <- ""unknown""
            }
            dogType <- GetType(""Dog"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var dogType = interpreter.Manager.GetValue(new LangId("dogType")) as TypeLangValue;
        Assert.NotNull(dogType);

        var baseType = dogType.GetBaseType(interpreter.Manager);

        // Assert
        Assert.NotNull(baseType);
        Assert.Equal("Animal", baseType.Value);
    }

    [Fact]
    public void TypeLangValue_GetBaseType_WithNoParent_ReturnsNull()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
            }
            personType <- GetType(""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var personType = interpreter.Manager.GetValue(new LangId("personType")) as TypeLangValue;
        Assert.NotNull(personType);

        var baseType = personType.GetBaseType(interpreter.Manager);

        // Assert
        Assert.Null(baseType);
    }

    #endregion

    #region TypeLangValue.GetInterfaces 测试

    [Fact]
    public void TypeLangValue_GetInterfaces_ReturnsImplementedInterfaces()
    {
        // Arrange
        var code = @"
            interface IDrawable {
                public func draw()
            }
            interface IMovable {
                public func move()
            }
            class Shape implements IDrawable, IMovable {
                public func draw() {}
                public func move() {}
            }
            shapeType <- GetType(""Shape"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var shapeType = interpreter.Manager.GetValue(new LangId("shapeType")) as TypeLangValue;
        Assert.NotNull(shapeType);

        var interfaces = shapeType.GetInterfaces(interpreter.Manager);

        // Assert
        Assert.NotNull(interfaces);
        Assert.Equal(2, interfaces.Count);
        Assert.Contains(interfaces, i => i.Value == "IDrawable");
        Assert.Contains(interfaces, i => i.Value == "IMovable");
    }

    #endregion

    #region GetTypeInfo 测试

    [Fact]
    public void GetTypeInfo_ReturnsCompleteTypeInformation()
    {
        // Arrange
        var code = @"
            class Animal {
                public species <- ""unknown""
            }
            interface IDrawable {
                public func draw()
            }
            class Dog extends Animal implements IDrawable {
                public breed <- ""unknown""
                public func bark() {}
                public func draw() {}
            }
            dogInfo <- GetTypeInfo(""Dog"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("dogInfo"));
        Assert.NotNull(result);
        Assert.IsType<DictionaryLangValue>(result);

        var dict = (DictionaryLangValue)result;

        // 验证字典不为空
        Assert.NotEmpty(dict.Tuples);
    }

    #endregion

    #region 类型属性测试

    [Fact]
    public void TypeLangValue_IsClass_ReturnsTrue()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
            }
            personType <- GetType(""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var typeValue = interpreter.Manager.GetValue(new LangId("personType")) as TypeLangValue;
        Assert.NotNull(typeValue);

        var isClass = typeValue.IsClass(interpreter.Manager);

        // Assert
        Assert.True(isClass);
    }

    [Fact]
    public void TypeLangValue_IsInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
            interface IDrawable {
                public func draw()
            }
            drawableType <- GetType(""IDrawable"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var typeValue = interpreter.Manager.GetValue(new LangId("drawableType")) as TypeLangValue;
        Assert.NotNull(typeValue);

        var isInterface = typeValue.IsInterface(interpreter.Manager);

        // Assert
        Assert.True(isInterface);
    }

    [Fact]
    public void TypeLangValue_IsPrimitive_WithIntType_ReturnsTrue()
    {
        // Arrange
        var intType = new TypeLangValue("Int");

        // Act
        var isPrimitive = intType.IsPrimitive();

        // Assert
        Assert.True(isPrimitive);
    }

    [Fact]
    public void TypeLangValue_IsPrimitive_WithCustomType_ReturnsFalse()
    {
        // Arrange
        var customType = new TypeLangValue("Person");

        // Act
        var isPrimitive = customType.IsPrimitive();

        // Assert
        Assert.False(isPrimitive);
    }

    #endregion
}
