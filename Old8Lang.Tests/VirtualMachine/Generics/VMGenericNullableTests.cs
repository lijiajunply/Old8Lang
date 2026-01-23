using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机可空泛型类型参数测试
/// 测试泛型类型参数的可空特性
/// </summary>
public class VMGenericNullableTests
{
    [Fact]
    public void GenericFunction_NullableTypeParameter_AcceptsNull()
    {
        // Arrange
        var code = @"
            func getValue<T?>(value:T?) -> string {
                if value == null {
                    return ""null""
                } else {
                    return value.ToStr()
                }
            }

            result1 <- getValue<int?>(null)
            result2 <- getValue<int?>(42)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("null", result1);
        Assert.Equal("42", result2);
    }

    [Fact]
    public void GenericFunction_NullableTypeParameter_WithStringType()
    {
        // Arrange
        var code = @"
            func processString<T?>(value:T?) -> string {
                if value == null {
                    return ""empty""
                } else {
                    return value.ToStr()
                }
            }

            result1 <- processString<string?>(null)
            result2 <- processString<string?>(""hello"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("empty", result1);
        Assert.Equal("hello", result2);
    }

    [Fact]
    public void GenericClass_NullableTypeParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Container<T?> {
                public value:T?

                func init(v:T?) -> void {
                    this.value <- v
                }

                func hasValue() -> bool {
                    return this.value != null
                }

                func getValue() -> string {
                    if this.value == null {
                        return ""no value""
                    } else {
                        return this.value.ToStr()
                    }
                }
            }

            container1 <- Container<int?>(null)
            container2 <- Container<int?>(100)
            result1 <- container1.hasValue()
            result2 <- container2.hasValue()
            result3 <- container1.getValue()
            result4 <- container2.getValue()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        var result4 = vm.GetGlobalVariable("result4");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.NotNull(result4);
        Assert.False((bool)result1);
        Assert.True((bool)result2);
        Assert.Equal("no value", result3);
        Assert.Equal("100", result4);
    }

    [Fact]
    public void GenericFunction_NullableWithDefaultValue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getOrDefault<T?>(value:T?, defaultValue:T) -> T {
                if value == null {
                    return defaultValue
                } else {
                    return value
                }
            }

            result1 <- getOrDefault<int?>(null, 0)
            result2 <- getOrDefault<int?>(42, 0)
            result3 <- getOrDefault<string?>(null, ""default"")
            result4 <- getOrDefault<string?>(""test"", ""default"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        var result4 = vm.GetGlobalVariable("result4");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.NotNull(result4);
        Assert.Equal(0, result1);
        Assert.Equal(42, result2);
        Assert.Equal("default", result3);
        Assert.Equal("test", result4);
    }

    [Fact]
    public void GenericClass_NullableWithMultipleTypeParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Pair<K?, V?> {
                public key:K?
                public value:V?

                func init(k:K?, v:V?) -> void {
                    this.key <- k
                    this.value <- v
                }

                func hasKey() -> bool {
                    return this.key != null
                }

                func hasValue() -> bool {
                    return this.value != null
                }

                func toString() -> string {
                    keyStr <- ""null""
                    valueStr <- ""null""
                    if this.key != null {
                        keyStr <- this.key.ToStr()
                    }
                    if this.value != null {
                        valueStr <- this.value.ToStr()
                    }
                    return keyStr + "":"" + valueStr
                }
            }

            pair1 <- Pair<int?, string?>(1, ""one"")
            pair2 <- Pair<int?, string?>(null, ""two"")
            pair3 <- Pair<int?, string?>(3, null)
            pair4 <- Pair<int?, string?>(null, null)

            result1 <- pair1.toString()
            result2 <- pair2.toString()
            result3 <- pair3.toString()
            result4 <- pair4.toString()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        var result4 = vm.GetGlobalVariable("result4");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.NotNull(result4);
        Assert.Equal("1:one", result1);
        Assert.Equal("null:two", result2);
        Assert.Equal("3:null", result3);
        Assert.Equal("null:null", result4);
    }

    [Fact]
    public void GenericFunction_NullableWithComplexType_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string
                public age:int

                func init(n:string, a:int) -> void {
                    this.name <- n
                    this.age <- a
                }

                func toString() -> string {
                    return this.name + ""("" + this.age.ToStr() + "")""
                }
            }

            func describePerson<T?>(person:T?) -> string {
                if person == null {
                    return ""No person""
                } else {
                    return person.toString()
                }
            }

            p1 <- Person(""Alice"", 30)
            result1 <- describePerson<Person?>(p1)
            result2 <- describePerson<Person?>(null)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("Alice(30)", result1);
        Assert.Equal("No person", result2);
    }

    [Fact]
    public void GenericClass_NullableWithList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class NullableList<T?> {
                public items:list

                func init() -> void {
                    this.items <- {}
                }

                func add(item:T?) -> void {
                    this.items.Add(item)
                }

                func get(index:int) -> T? {
                    return this.items[index]
                }

                func count() -> int {
                    return this.items.Count()
                }

                func countNonNull() -> int {
                    count <- 0
                    for item in this.items {
                        if item != null {
                            count <- count + 1
                        }
                    }
                    return count
                }
            }

            list <- NullableList<int?>()
            list.add(1)
            list.add(null)
            list.add(3)
            list.add(null)
            list.add(5)

            result1 <- list.count()
            result2 <- list.countNonNull()
            result3 <- list.get(0)
            result4 <- list.get(1)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        var result4 = vm.GetGlobalVariable("result4");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(5, result1);
        Assert.Equal(3, result2);
        Assert.Equal(1, result3);
        Assert.Null(result4);
    }

    [Fact]
    public void GenericFunction_NullableWithConditionalReturn_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func findFirst<T?>(items:list, predicate:any) -> T? {
                for item in items {
                    if predicate(item) {
                        return item
                    }
                }
                return null
            }

            numbers <- {1, 2, 3, 4, 5}
            result1 <- findFirst<int?>(numbers, (n:int) -> n > 3)
            result2 <- findFirst<int?>(numbers, (n:int) -> n > 10)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result1);
        Assert.Equal(4, result1);
        Assert.Null(result2);
    }
}
