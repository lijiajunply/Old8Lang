using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Interfaces;

/// <summary>
/// 抽象类语法测试
/// </summary>
[Collection("Sequential")]
public class AbstractClassTests
{
    #region 抽象类基础语法

    /// <summary>
    /// 测试基本抽象类声明 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_BasicAbstractClass_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class Shape {
    protected Name
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试抽象类继承链 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassInheritanceChain_ThrowsSyntaxError()
    {
        // Arrange
        var code = """
                   abstract class Animal {
                       protected name
                   }
                   """;
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试抽象类实现接口 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassImplementingInterface_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class GameObject : IDrawable {
    protected x
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试抽象类的静态成员 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassStaticMembers_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class DatabaseConnection {
    public static connectionPoolSize <- 10
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 抽象方法重载

    /// <summary>
    /// 测试抽象方法重载 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractMethodOverloading_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class Processor {
    abstract func Process(data:int) -> string
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 抽象类的构造函数

    /// <summary>
    /// 测试抽象类的构造函数链 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassConstructorChain_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class Vehicle {
    protected make
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 错误语法测试

    /// <summary>
    /// 测试不完整的抽象类声明
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteAbstractClass_ThrowsSyntaxError()
    {
        // Arrange
        var code = """
                   abstract class TestAbstract {
                   """;
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试实例化抽象类 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassInstantiation_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class AbstractTest {
    abstract func DoSomething() -> void
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试抽象类中的非抽象方法缺少实现
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassNonAbstractMethodWithoutImplementation_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class TestAbstract {
    abstract func AbstractMethod() -> void

    // 非抽象方法应该有实现
    public func ConcreteMethod() -> void
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试子类未实现父类的所有抽象方法 - 应该抛出语法错误，因为Old8Lang不支持abstract关键字
    /// </summary>
    [Fact]
    public void ParseProgram_ClassNotImplementingAllAbstractMethods_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class AbstractBase {
    abstract func Method1() -> void
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}