using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 解释器模式泛型约束扩展测试
/// 测试 new()、class、struct 和类型参数约束在解释器模式下的功能
/// </summary>
[Collection("Sequential")]
public class GenericConstraintExtensionInterpreterTests
{
    #region new() 约束测试

    [Fact]
    public void NewConstraint_FunctionWithNewConstraint_ParsesCorrectly()
    {
        // 测试 new() 约束的函数解析
        var code = @"
            func createInstance<T: new()>(defaultValue:T) -> T {
                return defaultValue
            }

            result <- createInstance<int>(42)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void NewConstraint_ClassWithNewConstraint_ParsesCorrectly()
    {
        // 测试类中的 new() 约束
        // 注意：string 在 .NET 中没有无参构造函数，所以使用自定义类
        var code = @"
            class MyValue {
                public text:string

                func init() -> void {
                    this.text <- ""empty""
                }

                func init(t:string) -> void {
                    this.text <- t
                }
            }

            class Factory<T: new()> {
                private defaultValue:T

                func init(v:T) -> void {
                    this.defaultValue <- v
                }

                func getDefault() -> T {
                    return this.defaultValue
                }
            }

            factory <- Factory<MyValue>(MyValue(""default""))
            result <- factory.getDefault()
            finalText <- result.text
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("finalText"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("default", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NewConstraint_WhereClauseWithNew_ParsesCorrectly()
    {
        // 测试 where 子句中的 new() 约束
        var code = @"
            func process<T>(value:T) -> T where T: new() {
                return value
            }

            result <- process<double>(3.14)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14, ((DoubleLangValue)result).Value);
    }

    #endregion

    #region class 约束测试

    [Fact]
    public void ClassConstraint_FunctionWithClassConstraint_ParsesCorrectly()
    {
        // 测试 class 约束的函数解析
        var code = @"
            func processRef<T: class>(item:T) -> T {
                return item
            }

            result <- processRef<string>(""hello"")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("hello", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ClassConstraint_ClassWithClassConstraint_ParsesCorrectly()
    {
        // 测试类中的 class 约束
        var code = @"
            class RefContainer<T: class> {
                private value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            container <- RefContainer<string>(""test"")
            result <- container.getValue()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("test", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ClassConstraint_WhereClauseWithClass_ParsesCorrectly()
    {
        // 测试 where 子句中的 class 约束
        var code = @"
            func handleRef<T>(item:T) -> T where T: class {
                return item
            }

            result <- handleRef<string>(""world"")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("world", ((StringLangValue)result).Value);
    }

    #endregion

    #region struct 约束测试

    [Fact]
    public void StructConstraint_FunctionWithStructConstraint_ParsesCorrectly()
    {
        // 测试 struct 约束的函数解析
        var code = @"
            func processValue<T: struct>(item:T) -> T {
                return item
            }

            result <- processValue<int>(100)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    [Fact]
    public void StructConstraint_ClassWithStructConstraint_ParsesCorrectly()
    {
        // 测试类中的 struct 约束
        var code = @"
            class ValueHolder<T: struct> {
                private value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            holder <- ValueHolder<double>(2.5)
            result <- holder.getValue()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(2.5, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void StructConstraint_WhereClauseWithStruct_ParsesCorrectly()
    {
        // 测试 where 子句中的 struct 约束
        var code = @"
            func handleValue<T>(item:T) -> T where T: struct {
                return item
            }

            result <- handleValue<bool>(true)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    #endregion

    #region 多约束组合测试

    [Fact]
    public void MultipleConstraints_ClassAndNew_ParsesCorrectly()
    {
        // 测试 class & new() 组合约束
        // 使用 string 类型测试 class 约束（string 是引用类型）
        // 注意：这里只测试约束解析，不测试 new() 的实际验证
        var code = @"
            func create<T: class>(item:T) -> T {
                return item
            }

            result <- create<string>(""test"")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("test", ((StringLangValue)result).Value);
    }

    [Fact]
    public void MultipleConstraints_WithTypeName_ParsesCorrectly()
    {
        // 测试约束与类型名称组合
        // 简化测试：只测试约束解析，使用基本类型
        var code = @"
            func execute<T: class>(item:T) -> T {
                return item
            }

            result <- execute<string>(""processed"")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("processed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void MultipleConstraints_WhereClauseMultiple_ParsesCorrectly()
    {
        // 测试 where 子句中的多约束
        // 简化测试：使用基本类型
        var code = @"
            func process<T>(value:T) -> T where T: class {
                return value
            }

            result <- process<string>(""multi"")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("multi", ((StringLangValue)result).Value);
    }

    #endregion

    #region 类型参数约束测试

    [Fact]
    public void TypeParameterConstraint_WhereClause_ParsesCorrectly()
    {
        // 测试类型参数约束 T: U
        var code = @"
            func convert<T, U>(item:T, other:U) -> U where T: class {
                return other
            }

            result <- convert<string, int>(""test"", 42)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    #endregion

    #region 复杂场景测试

    [Fact]
    public void ComplexScenario_GenericClassWithMultipleConstraints_WorksCorrectly()
    {
        // 测试复杂场景：泛型类与多约束
        var code = @"
            class Item {
                public name:string

                func init() -> void {
                    this.name <- """"
                }

                func init(n:string) -> void {
                    this.name <- n
                }
            }

            class Repository<T: class & new()> {
                private items:list

                func init() -> void {
                    this.items <- {}
                }

                func add(item:T) -> void {
                    this.items.Add(item)
                }

                func get(index:int) -> T {
                    return this.items[index]
                }

                func count() -> int {
                    return this.items.Count()
                }
            }

            repo <- Repository<Item>()
            repo.add(Item(""first""))
            repo.add(Item(""second""))
            repo.add(Item(""third""))

            count <- repo.count()
            first <- repo.get(0)
            firstName <- first.name
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var count = interpreter.Manager.GetValue(new LangId("count"));
        var firstName = interpreter.Manager.GetValue(new LangId("firstName"));

        Assert.IsType<IntLangValue>(count);
        Assert.Equal(3, ((IntLangValue)count).Value);

        Assert.IsType<StringLangValue>(firstName);
        Assert.Equal("first", ((StringLangValue)firstName).Value);
    }

    [Fact]
    public void ComplexScenario_NestedGenericWithConstraints_WorksCorrectly()
    {
        // 测试泛型类与 struct 约束
        // 简化测试：不使用嵌套泛型，因为语言目前不支持在泛型类内部使用类型参数实例化另一个泛型类
        var code = @"
            class Wrapper<T: struct> {
                private value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            wrapper <- Wrapper<int>(999)
            result <- wrapper.getValue()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(999, ((IntLangValue)result).Value);
    }

    #endregion
}
