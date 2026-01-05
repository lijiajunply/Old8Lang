using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Types;

/// <summary>
/// 联合类型解释器执行测试
/// </summary>
[Collection("Sequential")]
public class UnionTypesInterpreterTests
{
    #region 基本联合类型赋值

    /// <summary>
    /// 测试简单联合类型赋值和重新赋值
    /// </summary>
    [Fact]
    public void Run_SimpleUnionTypeAssignment_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
a: int | string <- 123
b <- a

a <- ""hello""
c <- a";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var b = interpreter.Manager.GetValue(new LangId("b"));
        var c = interpreter.Manager.GetValue(new LangId("c"));

        Assert.IsType<IntLangValue>(b);
        Assert.Equal(123, ((IntLangValue)b).Value);
        Assert.IsType<StringLangValue>(c);
        Assert.Equal("hello", ((StringLangValue)c).Value);
    }

    /// <summary>
    /// 测试多类型联合赋值
    /// </summary>
    [Fact]
    public void Run_MultipleTypesUnionAssignment_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
value: int | string | bool <- true
v1 <- value

value <- 456
v2 <- value

value <- ""test""
v3 <- value";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var v1 = interpreter.Manager.GetValue(new LangId("v1"));
        var v2 = interpreter.Manager.GetValue(new LangId("v2"));
        var v3 = interpreter.Manager.GetValue(new LangId("v3"));

        Assert.IsType<BoolLangValue>(v1);
        Assert.True(((BoolLangValue)v1).Value);
        Assert.Equal(456, ((IntLangValue)v2).Value);
        Assert.IsType<StringLangValue>(v3);
        Assert.Equal("test", ((StringLangValue)v3).Value);
    }

    /// <summary>
    /// 测试可空联合类型 null 赋值
    /// </summary>
    [Fact]
    public void Run_NullableUnionTypeNullAssignment_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
a: int? | string? <- null
result <- a.ToStr()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Null", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试可空联合类型非 null 赋值
    /// </summary>
    [Fact]
    public void Run_NullableUnionTypeNonNullAssignment_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
a: int? | string? <- 789
b: int? | string? <- ""world""

result1 <- a
result2 <- b";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.Equal(789, ((IntLangValue)result1).Value);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("world", ((StringLangValue)result2).Value);
    }

    #endregion

    #region 联合类型函数参数

    /// <summary>
    /// 测试联合类型函数参数 - int 类型
    /// </summary>
    [Fact]
    public void Run_UnionTypeFunctionParameterWithInt_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func process(x: int | string) -> string {
    return ""Value: "" + x.ToStr()
}

result <- process(123)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Value: 123", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试联合类型函数参数 - string 类型
    /// </summary>
    [Fact]
    public void Run_UnionTypeFunctionParameterWithString_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func process(x: int | string) -> string {
    return ""Value: "" + x.ToStr()
}

result <- process(""hello"")";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Value: hello", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个联合类型参数
    /// </summary>
    [Fact]
    public void Run_MultipleUnionTypeParameters_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func combine(a: int | string, b: bool | double) -> string {
    return a.ToStr() + ""-"" + b.ToStr()
}

result1 <- combine(123, true)
result2 <- combine(""hello"", 3.14)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("123-true", ((StringLangValue)result1).Value);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("hello-3.14", ((StringLangValue)result2).Value);
    }

    #endregion

    #region 联合类型函数返回值

    /// <summary>
    /// 测试联合类型函数返回 int
    /// </summary>
    [Fact]
    public void Run_UnionTypeFunctionReturnInt_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func getValue(flag: bool) -> int | string {
    if flag {
        return 456
    } else {
        return ""text""
    }
}

result <- getValue(true)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.Equal(456, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试联合类型函数返回 string
    /// </summary>
    [Fact]
    public void Run_UnionTypeFunctionReturnString_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func getValue(flag: bool) -> int | string {
    if flag {
        return 456
    } else {
        return ""text""
    }
}

result <- getValue(false)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("text", ((StringLangValue)result).Value);
    }

    #endregion

    #region 联合类型类字段

    /// <summary>
    /// 测试类字段联合类型赋值
    /// </summary>
    [Fact]
    public void Run_UnionTypeClassFieldAssignment_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
class Container {
    public data: int | string | bool

    func init(value: int | string | bool) {
        this.data <- value
    }

    func getData() -> int | string | bool {
        return this.data
    }
}

c1 <- Container(123)
c2 <- Container(""hello"")
c3 <- Container(true)

result1 <- c1.getData()
result2 <- c2.getData()
result3 <- c3.getData()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.Equal(123, ((IntLangValue)result1).Value);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("hello", ((StringLangValue)result2).Value);
        Assert.True(((BoolLangValue)result3).Value);
    }

    /// <summary>
    /// 测试类字段联合类型重新赋值
    /// </summary>
    [Fact]
    public void Run_UnionTypeClassFieldReassignment_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
class Container {
    public data: int | string

    func init(value: int | string) {
        this.data <- value
    }

    func setData(value: int | string) -> void {
        this.data <- value
    }
}

c <- Container(100)
v1 <- c.data

c.setData(""updated"")
v2 <- c.data";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var v1 = interpreter.Manager.GetValue(new LangId("v1"));
        var v2 = interpreter.Manager.GetValue(new LangId("v2"));

        Assert.Equal(100, ((IntLangValue)v1).Value);
        Assert.IsType<StringLangValue>(v2);
        Assert.Equal("updated", ((StringLangValue)v2).Value);
    }

    #endregion

    #region 类型兼容性测试

    /// <summary>
    /// 测试联合类型兼容性 - 赋值给其中一个成员类型
    /// </summary>
    [Fact]
    public void Run_UnionTypeCompatibilityToMember_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
union_var: int | string <- 999
int_var: int <- union_var
result <- int_var";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.Equal(999, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试成员类型赋值给联合类型
    /// </summary>
    [Fact]
    public void Run_MemberTypeToUnionTypeCompatibility_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
int_var: int <- 555
string_var: string <- ""text""

union1: int | string <- int_var
union2: int | string <- string_var

result1 <- union1
result2 <- union2";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.Equal(555, ((IntLangValue)result1).Value);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("text", ((StringLangValue)result2).Value);
    }

    #endregion
}
