using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 编译器模式下的高级类功能测试 - 泛型类
/// </summary>
public class GenericClassTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void BasicGenericClass_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Box<T> {
                public value : T
                
                func init(value:T) {
                    this.value <- value
                }
                
                func getValue() -> T {
                    return this.value
                }
                
                func setValue(newValue:T) -> void {
                    this.value <- newValue
                }
            }
            
            intBox <- Box<int>(42)
            strBox <- Box<string>(""Hello"")
            
            Assert.Equal(42, intBox.getValue())
            Assert.Equal(""Hello"", strBox.getValue())
            
            intBox.setValue(100)
            strBox.setValue(""World"")
            
            Assert.Equal(100, intBox.getValue())
            Assert.Equal(""World"", strBox.getValue())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithMultipleTypeParameters_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Pair<T, U> {
                public first : T
                public second : U
                
                func init(first:T, second:U) {
                    this.first <- first
                    this.second <- second
                }
                
                func getFirst() -> T {
                    return this.first
                }
                
                func getSecond() -> U {
                    return this.second
                }
                
                func swap() -> Pair<U, T> {
                    return Pair<U, T>(this.second, this.first)
                }
            }
            
            pair1 <- Pair<int, string>(1, ""one"")
            pair2 <- Pair<string, double>(""pi"", 3.14)
            
            Assert.Equal(1, pair1.getFirst())
            Assert.Equal(""one"", pair1.getSecond())
            
            Assert.Equal(""pi"", pair2.getFirst())
            Assert.Equal(3.14, pair2.getSecond())
            
            swapped <- pair1.swap()
            Assert.Equal(""one"", swapped.getFirst())
            Assert.Equal(1, swapped.getSecond())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithConstraints_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class NumericBox<T> where T : int {
                public value : T
                
                func init(value:T) {
                    this.value <- value
                }
                
                func add(other:T) -> T {
                    return this.value + other
                }
                
                func multiply(other:T) -> T {
                    return this.value * other
                }
            }
            
            box <- NumericBox<int>(10)
            
            result1 <- box.add(5)
            result2 <- box.multiply(3)
            
            Assert.Equal(15, result1)
            Assert.Equal(30, result2)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithCollections_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Stack<T> {
                private items : list
                
                func init() {
                    this.items <- {}
                }
                
                func push(item:T) -> void {
                    this.items.Add(item)
                }
                
                func pop() -> T? {
                    if this.items.Count() > 0 {
                        item <- this.items[this.items.Count() - 1]
                        this.items.RemoveAt(this.items.Count() - 1)
                        return item
                    }
                    return null
                }
                
                func peek() -> T? {
                    if this.items.Count() > 0 {
                        return this.items[this.items.Count() - 1]
                    }
                    return null
                }
                
                func isEmpty() -> bool {
                    return this.items.Count() == 0
                }
                
                func getCount() -> int {
                    return this.items.Count()
                }
            }
            
            intStack <- Stack<int>()
            
            Assert.True(intStack.isEmpty())
            
            intStack.push(1)
            intStack.push(2)
            intStack.push(3)
            
            Assert.Equal(3, intStack.getCount())
            Assert.Equal(3, intStack.peek())
            
            item1 <- intStack.pop()
            item2 <- intStack.pop()
            
            Assert.Equal(3, item1)
            Assert.Equal(2, item2)
            Assert.Equal(1, intStack.getCount())
            
            strStack <- Stack<string>()
            strStack.push(""hello"")
            strStack.push(""world"")
            
            Assert.Equal(""world"", strStack.pop())
            Assert.Equal(""hello"", strStack.pop())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Container<T> {
                protected value : T
                
                func init(value:T) {
                    this.value <- value
                }
                
                func getValue() -> T {
                    return this.value
                }
                
                func setValue(newValue:T) -> void {
                    this.value <- newValue
                }
            }
            
            class ReadOnlyContainer<T> : Container<T> {
                func init(value:T) {
                    super.init(value)
                }
                
                func setValue(newValue:T) -> void {
                }
            }
            
            class ValidatedContainer<T> : Container<T> {
                private validator : func(T) -> bool
                
                func init(value:T, validator:func(T) -> bool) {
                    super.init(value)
                    this.validator <- validator
                }
                
                func setValue(newValue:T) -> void {
                    if this.validator(newValue) {
                        this.value <- newValue
                    }
                }
            }
            
            readOnly <- ReadOnlyContainer<int>(10)
            Assert.Equal(10, readOnly.getValue())
            
            readOnly.setValue(20)
            Assert.Equal(10, readOnly.getValue())
            
            validated <- ValidatedContainer<int>(5, func(x:int) -> bool {
                return x > 0
            })
            Assert.Equal(5, validated.getValue())
            
            validated.setValue(10)
            Assert.Equal(10, validated.getValue())
            
            validated.setValue(-5)
            Assert.Equal(10, validated.getValue())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithStaticMembers_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Counter<T> {
                public static count <- 0
                
                public static func increment() -> int {
                    Counter<T>.count <- Counter<T>.count + 1
                    return Counter<T>.count
                }
                
                public static func getCount() -> int {
                    return Counter<T>.count
                }
                
                public static func reset() -> void {
                    Counter<T>.count <- 0
                }
            }
            
            result1 <- Counter<int>.increment()
            result2 <- Counter<int>.increment()
            result3 <- Counter<int>.getCount()
            
            Assert.Equal(1, result1)
            Assert.Equal(2, result2)
            Assert.Equal(2, result3)
            
            Counter<int>.reset()
            result4 <- Counter<int>.getCount()
            Assert.Equal(0, result4)
            
            Counter<string>.increment()
            Counter<string>.increment()
            Counter<string>.increment()
            
            Assert.Equal(3, Counter<string>.getCount())
            Assert.Equal(0, Counter<int>.getCount())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithMethods_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class ListProcessor<T> {
                private items : list
                
                func init(items:list) {
                    this.items <- items
                }
                
                func filter(predicate:func(T) -> bool) -> list {
                    result <- {}
                    i <- 0
                    while i < this.items.Count() {
                        item <- this.items[i]
                        if predicate(item) {
                            result.Add(item)
                        }
                        i <- i + 1
                    }
                    return result
                }
                
                func map(transformer:func(T) -> T) -> list {
                    result <- {}
                    i <- 0
                    while i < this.items.Count() {
                        item <- this.items[i]
                        result.Add(transformer(item))
                        i <- i + 1
                    }
                    return result
                }
                
                func reduce(initializer:T, accumulator:func(T, T) -> T) -> T {
                    result <- initializer
                    i <- 0
                    while i < this.items.Count() {
                        item <- this.items[i]
                        result <- accumulator(result, item)
                        i <- i + 1
                    }
                    return result
                }
            }
            
            processor <- ListProcessor<int>({1, 2, 3, 4, 5})
            
            filtered <- processor.filter(func(x:int) -> bool {
                return x % 2 == 0
            })
            Assert.Equal({2, 4}, filtered)
            
            mapped <- processor.map(func(x:int) -> int {
                return x * x
            })
            Assert.Equal({1, 4, 9, 16, 25}, mapped)
            
            reduced <- processor.reduce(0, func(acc:int, x:int) -> int {
                return acc + x
            })
            Assert.Equal(15, reduced)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithNestedGenerics_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Entry<K, V> {
                public key : K
                public value : V
                
                func init(key:K, value:V) {
                    this.key <- key
                    this.value <- value
                }
            }
            
            class Dictionary<K, V> {
                private entries : list
                
                func init() {
                    this.entries <- {}
                }
                
                func put(key:K, value:V) -> void {
                    this.entries.Add(Entry<K, V>(key, value))
                }
                
                func get(key:K) -> V? {
                    i <- 0
                    while i < this.entries.Count() {
                        entry <- this.entries[i]
                        if entry.key == key {
                            return entry.value
                        }
                        i <- i + 1
                    }
                    return null
                }
                
                func count() -> int {
                    return this.entries.Count()
                }
            }
            
            dict <- Dictionary<string, int>()
            dict.put(""one"", 1)
            dict.put(""two"", 2)
            dict.put(""three"", 3)
            
            Assert.Equal(3, dict.count())
            Assert.Equal(1, dict.get(""one""))
            Assert.Equal(2, dict.get(""two""))
            Assert.Equal(3, dict.get(""three""))
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithDefaultValues_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Container<T> {
                public value : T
                
                func init(value:T) {
                    this.value <- value
                }
                
                func getValue() -> T {
                    return this.value
                }
                
                func setValue(newValue:T) -> void {
                    this.value <- newValue
                }
                
                func resetToDefault(defaultValue:T) -> void {
                    this.value <- defaultValue
                }
            }
            
            intContainer <- Container<int>(100)
            strContainer <- Container<string>(""initial"")
            boolContainer <- Container<bool>(true)
            
            intContainer.resetToDefault(0)
            strContainer.resetToDefault("""")
            boolContainer.resetToDefault(false)
            
            Assert.Equal(0, intContainer.getValue())
            Assert.Equal("""", strContainer.getValue())
            Assert.Equal(false, boolContainer.getValue())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericClassWithComparisons_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class ComparableBox<T> where T : int {
                public value : T
                
                func init(value:T) {
                    this.value <- value
                }
                
                func compare(other:ComparableBox<T>) -> int {
                    if this.value < other.value {
                        return -1
                    } else if this.value > other.value {
                        return 1
                    } else {
                        return 0
                    }
                }
                
                func equals(other:ComparableBox<T>) -> bool {
                    return this.value == other.value
                }
            }
            
            box1 <- ComparableBox<int>(10)
            box2 <- ComparableBox<int>(20)
            box3 <- ComparableBox<int>(10)
            
            Assert.Equal(-1, box1.compare(box2))
            Assert.Equal(1, box2.compare(box1))
            Assert.Equal(0, box1.compare(box3))
            Assert.True(box1.equals(box3))
            Assert.False(box1.equals(box2))
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
