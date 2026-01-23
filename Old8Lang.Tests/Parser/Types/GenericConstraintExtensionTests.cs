using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Types;

/// <summary>
/// 泛型约束扩展测试
/// 测试 new()、class、struct 和类型参数约束的解析
/// </summary>
public class GenericConstraintExtensionTests
{
    [Fact]
    public void ParseNewConstraint_WithParentheses_Success()
    {
        // 测试 new() 约束语法
        var code = @"
func createInstance<T: new()>() -> T {
    return T()
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
        // 如果没有抛出异常，则解析成功
    }

    [Fact]
    public void ParseNewConstraint_WithoutParentheses_Success()
    {
        // 测试 new 约束语法（不带括号）
        var code = @"
func createInstance<T: new>() -> T {
    return T()
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseClassConstraint_Success()
    {
        // 测试 class 约束语法
        var code = @"
func processRef<T: class>(item:T) -> T {
    return item
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseStructConstraint_Success()
    {
        // 测试 struct 约束语法
        var code = @"
func processValue<T: struct>(item:T) -> T {
    return item
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseMultipleConstraints_WithAmpersand_Success()
    {
        // 测试多约束组合（使用 & 分隔符）
        var code = @"
func create<T: class & new()>() -> T {
    return T()
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseMultipleConstraints_WithPipe_Success()
    {
        // 测试多约束组合（使用 | 分隔符）
        var code = @"
func create<T: class | new()>() -> T {
    return T()
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseConstraintWithTypeName_Success()
    {
        // 测试约束与类型名称组合
        var code = @"
func process<T: class & IComparable>() -> T {
    return T()
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseWhereClause_WithNewConstraint_Success()
    {
        // 测试 where 子句中的 new() 约束
        var code = @"
func createInstance<T>() -> T where T: new() {
    return T()
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseWhereClause_WithClassConstraint_Success()
    {
        // 测试 where 子句中的 class 约束
        var code = @"
func processRef<T>(item:T) -> T where T: class {
    return item
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseWhereClause_WithStructConstraint_Success()
    {
        // 测试 where 子句中的 struct 约束
        var code = @"
func processValue<T>(item:T) -> T where T: struct {
    return item
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseWhereClause_WithMultipleConstraints_Success()
    {
        // 测试 where 子句中的多约束
        var code = @"
func create<T>() -> T where T: class & new() {
    return T()
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseClassWithNewConstraint_Success()
    {
        // 测试类中的 new() 约束
        var code = @"
class Factory<T: new()> {
    private value:T

    public func init(v:T) -> void {
        value <- v
    }

    public func create() -> T {
        return value
    }
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseClassWithClassConstraint_Success()
    {
        // 测试类中的 class 约束
        var code = @"
class Container<T: class> {
    private value:T

    public func init(v:T) -> void {
        value <- v
    }

    public func getValue() -> T {
        return value
    }
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void ParseClassWithStructConstraint_Success()
    {
        // 测试类中的 struct 约束
        var code = @"
class ValueHolder<T: struct> {
    private value:T

    public func init(v:T) -> void {
        value <- v
    }

    public func getValue() -> T {
        return value
    }
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code);
    }

    [Fact]
    public void GenericConstraint_CreateNew_ReturnsCorrectKind()
    {
        var constraint = GenericConstraint.CreateNew();
        Assert.Equal(GenericConstraintKind.New, constraint.Kind);
        Assert.Null(constraint.TypeName);
        Assert.True(constraint.IsSpecialConstraint);
        Assert.False(constraint.IsTypeConstraint);
    }

    [Fact]
    public void GenericConstraint_CreateClass_ReturnsCorrectKind()
    {
        var constraint = GenericConstraint.CreateClass();
        Assert.Equal(GenericConstraintKind.Class, constraint.Kind);
        Assert.Null(constraint.TypeName);
        Assert.True(constraint.IsSpecialConstraint);
        Assert.False(constraint.IsTypeConstraint);
    }

    [Fact]
    public void GenericConstraint_CreateStruct_ReturnsCorrectKind()
    {
        var constraint = GenericConstraint.CreateStruct();
        Assert.Equal(GenericConstraintKind.Struct, constraint.Kind);
        Assert.Null(constraint.TypeName);
        Assert.True(constraint.IsSpecialConstraint);
        Assert.False(constraint.IsTypeConstraint);
    }

    [Fact]
    public void GenericConstraint_CreateTypeName_ReturnsCorrectKind()
    {
        var constraint = GenericConstraint.CreateTypeName("IComparable");
        Assert.Equal(GenericConstraintKind.TypeName, constraint.Kind);
        Assert.Equal("IComparable", constraint.TypeName);
        Assert.False(constraint.IsSpecialConstraint);
        Assert.True(constraint.IsTypeConstraint);
    }

    [Fact]
    public void GenericConstraint_CreateTypeParameter_ReturnsCorrectKind()
    {
        var constraint = GenericConstraint.CreateTypeParameter("U");
        Assert.Equal(GenericConstraintKind.TypeParameter, constraint.Kind);
        Assert.Equal("U", constraint.TypeName);
        Assert.False(constraint.IsSpecialConstraint);
        Assert.True(constraint.IsTypeConstraint);
    }

    [Fact]
    public void GenericConstraint_ClassAndStruct_Conflict()
    {
        var classConstraint = GenericConstraint.CreateClass();
        var structConstraint = GenericConstraint.CreateStruct();

        Assert.True(classConstraint.ConflictsWith(structConstraint));
        Assert.True(structConstraint.ConflictsWith(classConstraint));
    }

    [Fact]
    public void GenericConstraint_StructAndNew_Conflict()
    {
        var structConstraint = GenericConstraint.CreateStruct();
        var newConstraint = GenericConstraint.CreateNew();

        Assert.True(structConstraint.ConflictsWith(newConstraint));
        Assert.True(newConstraint.ConflictsWith(structConstraint));
    }

    [Fact]
    public void GenericConstraint_ClassAndNew_NoConflict()
    {
        var classConstraint = GenericConstraint.CreateClass();
        var newConstraint = GenericConstraint.CreateNew();

        Assert.False(classConstraint.ConflictsWith(newConstraint));
        Assert.False(newConstraint.ConflictsWith(classConstraint));
    }

    [Fact]
    public void GenericConstraint_Parse_NewWithParentheses()
    {
        var constraint = GenericConstraint.Parse("new()");
        Assert.Equal(GenericConstraintKind.New, constraint.Kind);
    }

    [Fact]
    public void GenericConstraint_Parse_NewWithoutParentheses()
    {
        var constraint = GenericConstraint.Parse("new");
        Assert.Equal(GenericConstraintKind.New, constraint.Kind);
    }

    [Fact]
    public void GenericConstraint_Parse_Class()
    {
        var constraint = GenericConstraint.Parse("class");
        Assert.Equal(GenericConstraintKind.Class, constraint.Kind);
    }

    [Fact]
    public void GenericConstraint_Parse_Struct()
    {
        var constraint = GenericConstraint.Parse("struct");
        Assert.Equal(GenericConstraintKind.Struct, constraint.Kind);
    }

    [Fact]
    public void GenericConstraint_Parse_TypeName()
    {
        var constraint = GenericConstraint.Parse("IComparable");
        Assert.Equal(GenericConstraintKind.TypeName, constraint.Kind);
        Assert.Equal("IComparable", constraint.TypeName);
    }

    [Fact]
    public void GenericConstraint_Parse_TypeParameter()
    {
        var genericParamNames = new HashSet<string> { "T", "U" };
        var constraint = GenericConstraint.Parse("U", genericParamNames);
        Assert.Equal(GenericConstraintKind.TypeParameter, constraint.Kind);
        Assert.Equal("U", constraint.TypeName);
    }

    [Fact]
    public void GenericParameter_HasNewConstraint_ReturnsTrue()
    {
        var constraints = new List<GenericConstraint>
        {
            GenericConstraint.CreateNew(),
            GenericConstraint.CreateTypeName("IComparable")
        };
        var param = new GenericParameter("T", constraints, default, false);

        Assert.True(param.HasNewConstraint);
        Assert.False(param.HasClassConstraint);
        Assert.False(param.HasStructConstraint);
    }

    [Fact]
    public void GenericParameter_HasClassConstraint_ReturnsTrue()
    {
        var constraints = new List<GenericConstraint>
        {
            GenericConstraint.CreateClass()
        };
        var param = new GenericParameter("T", constraints, default, false);

        Assert.False(param.HasNewConstraint);
        Assert.True(param.HasClassConstraint);
        Assert.False(param.HasStructConstraint);
    }

    [Fact]
    public void GenericParameter_HasStructConstraint_ReturnsTrue()
    {
        var constraints = new List<GenericConstraint>
        {
            GenericConstraint.CreateStruct()
        };
        var param = new GenericParameter("T", constraints, default, false);

        Assert.False(param.HasNewConstraint);
        Assert.False(param.HasClassConstraint);
        Assert.True(param.HasStructConstraint);
    }

    [Fact]
    public void GenericParameter_ValidateConstraints_NoConflict_ReturnsNull()
    {
        var constraints = new List<GenericConstraint>
        {
            GenericConstraint.CreateClass(),
            GenericConstraint.CreateNew()
        };
        var param = new GenericParameter("T", constraints, default, false);

        var error = param.ValidateConstraints();
        Assert.Null(error);
    }

    [Fact]
    public void GenericParameter_ValidateConstraints_WithConflict_ReturnsError()
    {
        var constraints = new List<GenericConstraint>
        {
            GenericConstraint.CreateClass(),
            GenericConstraint.CreateStruct()
        };
        var param = new GenericParameter("T", constraints, default, false);

        var error = param.ValidateConstraints();
        Assert.NotNull(error);
        Assert.Contains("冲突", error);
    }

    [Fact]
    public void GenericParameter_TypeNameConstraints_ReturnsCorrectList()
    {
        var constraints = new List<GenericConstraint>
        {
            GenericConstraint.CreateClass(),
            GenericConstraint.CreateTypeName("IComparable"),
            GenericConstraint.CreateTypeName("ICloneable")
        };
        var param = new GenericParameter("T", constraints, default, false);

        var typeNameConstraints = param.TypeNameConstraints.ToList();
        Assert.Equal(2, typeNameConstraints.Count);
        Assert.Contains("IComparable", typeNameConstraints);
        Assert.Contains("ICloneable", typeNameConstraints);
    }

    [Fact]
    public void GenericParameter_TypeParameterConstraints_ReturnsCorrectList()
    {
        var constraints = new List<GenericConstraint>
        {
            GenericConstraint.CreateTypeParameter("U"),
            GenericConstraint.CreateTypeParameter("V")
        };
        var param = new GenericParameter("T", constraints, default, false);

        var typeParamConstraints = param.TypeParameterConstraints.ToList();
        Assert.Equal(2, typeParamConstraints.Count);
        Assert.Contains("U", typeParamConstraints);
        Assert.Contains("V", typeParamConstraints);
    }
}
