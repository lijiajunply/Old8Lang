using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 泛型类测试
/// 测试泛型类的基本功能，包括实例化、方法调用、字段访问等
/// </summary>
[Collection("Sequential")]
public class GenericClassTests
{
    [Fact]
    public void GenericClass_SingleTypeParameter_CreatesInstanceCorrectly()
    {
        // Arrange
        var code = @"
            class Box<T> {
                private value:T

                func set(v:T) -> void {
                    this.value <- v
                }

                func get() -> T {
                    return this.value
                }
            }

            box <- Box<int>()
            box.set(42)
            result <- box.get()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void GenericClass_MultipleTypeParameters_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            class Pair<K, V> {
                private key:K
                private value:V

                func set(k:K, v:V) -> void {
                    this.key <- k
                    this.value <- v
                }

                func getKey() -> K {
                    return this.key
                }

                func getValue() -> V {
                    return this.value
                }
            }

            pair <- Pair<string, int>()
            pair.set(""age"", 25)
            k <- pair.getKey()
            v <- pair.getValue()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var key = interpreter.Manager.GetValue(new LangId("k"));
        var value = interpreter.Manager.GetValue(new LangId("v"));

        Assert.NotNull(key);
        Assert.IsType<StringLangValue>(key);
        Assert.Equal("age", ((StringLangValue)key).Value);

        Assert.NotNull(value);
        Assert.IsType<IntLangValue>(value);
        Assert.Equal(25, ((IntLangValue)value).Value);
    }

    [Fact]
    public void GenericClass_DifferentInstances_MaintainSeparateTypes()
    {
        // Arrange
        var code = @"
            class Container<T> {
                private data:T

                func store(d:T) -> void {
                    this.data <- d
                }

                func retrieve() -> T {
                    return this.data
                }
            }

            intContainer <- Container<int>()
            intContainer.store(100)

            stringContainer <- Container<string>()
            stringContainer.store(""hello"")

            intResult <- intContainer.retrieve()
            stringResult <- stringContainer.retrieve()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intResult = interpreter.Manager.GetValue(new LangId("intResult"));
        var stringResult = interpreter.Manager.GetValue(new LangId("stringResult"));

        Assert.IsType<IntLangValue>(intResult);
        Assert.Equal(100, ((IntLangValue)intResult).Value);

        Assert.IsType<StringLangValue>(stringResult);
        Assert.Equal("hello", ((StringLangValue)stringResult).Value);
    }

    [Fact]
    public void GenericClass_WithPrimitiveTypes_AllTypesWork()
    {
        // Arrange
        var code = @"
            class Holder<T> {
                private item:T

                func set(i:T) -> void {
                    this.item <- i
                }

                func get() -> T {
                    return this.item
                }
            }

            intHolder <- Holder<int>()
            intHolder.set(42)
            intVal <- intHolder.get()

            doubleHolder <- Holder<double>()
            doubleHolder.set(3.14)
            doubleVal <- doubleHolder.get()

            boolHolder <- Holder<bool>()
            boolHolder.set(true)
            boolVal <- boolHolder.get()

            stringHolder <- Holder<string>()
            stringHolder.set(""test"")
            stringVal <- stringHolder.get()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intVal = interpreter.Manager.GetValue(new LangId("intVal"));
        var doubleVal = interpreter.Manager.GetValue(new LangId("doubleVal"));
        var boolVal = interpreter.Manager.GetValue(new LangId("boolVal"));
        var stringVal = interpreter.Manager.GetValue(new LangId("stringVal"));

        Assert.IsType<IntLangValue>(intVal);
        Assert.Equal(42, ((IntLangValue)intVal).Value);

        Assert.IsType<DoubleLangValue>(doubleVal);
        Assert.Equal(3.14, ((DoubleLangValue)doubleVal).Value);

        Assert.IsType<BoolLangValue>(boolVal);
        Assert.True(((BoolLangValue)boolVal).Value);

        Assert.IsType<StringLangValue>(stringVal);
        Assert.Equal("test", ((StringLangValue)stringVal).Value);
    }

    [Fact]
    public void GenericClass_MethodReturnsTypeParameter_ReturnsCorrectType()
    {
        // Arrange
        var code = @"
            class Wrapper<T> {
                private wrapped:T

                func init(w:T) {
                    this.wrapped <- w
                }

                func unwrap() -> T {
                    return this.wrapped
                }
            }

            wrapper <- Wrapper<int>(123)
            result <- wrapper.unwrap()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(123, ((IntLangValue)result).Value);
    }

    [Fact]
    public void GenericClass_MultipleMethodsWithTypeParameter_AllWorkCorrectly()
    {
        // Arrange
        var code = @"
            class Stack<T> {
                private items:list

                func init() {
                    this.items <- {}
                }

                func push(item:T) -> void {
                    this.items.Add(item)
                }

                func pop() -> T {
                    lastIndex <- this.items.Count() - 1
                    item <- this.items[lastIndex]
                    this.items.RemoveAt(lastIndex)
                    return item
                }

                func peek() -> T {
                    return this.items[-1]
                }
            }

            stack <- Stack<string>()
            stack.push(""first"")
            stack.push(""second"")
            stack.push(""third"")

            peeked <- stack.peek()
            popped <- stack.pop()
            popped2 <- stack.pop()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var peeked = interpreter.Manager.GetValue(new LangId("peeked"));
        var popped = interpreter.Manager.GetValue(new LangId("popped"));
        var popped2 = interpreter.Manager.GetValue(new LangId("popped2"));

        Assert.IsType<StringLangValue>(peeked);
        Assert.Equal("third", ((StringLangValue)peeked).Value);

        Assert.IsType<StringLangValue>(popped);
        Assert.Equal("third", ((StringLangValue)popped).Value);

        Assert.IsType<StringLangValue>(popped2);
        Assert.Equal("second", ((StringLangValue)popped2).Value);
    }

    [Fact]
    public void GenericClass_WithConstructorParameters_InitializesCorrectly()
    {
        // Arrange
        var code = @"
            class Initialized<T> {
                private data:T

                func init(initialValue:T) {
                    this.data <- initialValue
                }

                func getData() -> T {
                    return this.data
                }
            }

            obj <- Initialized<double>(2.5)
            result <- obj.getData()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(2.5, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void GenericClass_ThreeTypeParameters_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            class Triple<A, B, C> {
                private first:A
                private second:B
                private third:C

                func setAll(a:A, b:B, c:C) -> void {
                    this.first <- a
                    this.second <- b
                    this.third <- c
                }

                func getFirst() -> A {
                    return this.first
                }

                func getSecond() -> B {
                    return this.second
                }

                func getThird() -> C {
                    return this.third
                }
            }

            triple <- Triple<int, string, double>()
            triple.setAll(1, ""two"", 3.0)

            a <- triple.getFirst()
            b <- triple.getSecond()
            c <- triple.getThird()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var a = interpreter.Manager.GetValue(new LangId("a"));
        var b = interpreter.Manager.GetValue(new LangId("b"));
        var c = interpreter.Manager.GetValue(new LangId("c"));

        Assert.IsType<IntLangValue>(a);
        Assert.Equal(1, ((IntLangValue)a).Value);

        Assert.IsType<StringLangValue>(b);
        Assert.Equal("two", ((StringLangValue)b).Value);

        Assert.IsType<DoubleLangValue>(c);
        Assert.Equal(3.0, ((DoubleLangValue)c).Value);
    }
}
