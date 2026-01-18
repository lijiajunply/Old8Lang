using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机泛型约束测试
/// 测试泛型类型参数的约束功能
/// </summary>
public class VMGenericConstraintsTests
{
    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericFunction_SingleConstraint_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IComparable {
                func compareTo(other:any) -> int
            }

            class Number {
                public value:int

                func init(v:int) -> void {
                    this.value <- v
                }

                func compareTo(other:any) -> int {
                    otherNum <- other as Number
                    if this.value > otherNum.value {
                        return 1
                    } elif this.value < otherNum.value {
                        return -1
                    } else {
                        return 0
                    }
                }
            }

            func compare<T: IComparable>(a:T, b:T) -> int {
                return a.compareTo(b)
            }

            n1 <- Number(10)
            n2 <- Number(20)
            result <- compare<Number>(n1, n2)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(-1, result);
    }

    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericFunction_MultipleConstraintsWithAnd_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface ISerializable {
                func serialize() -> string
            }

            interface ICloneable {
                func clone() -> any
            }

            class Data {
                public value:string

                func init(v:string) -> void {
                    this.value <- v
                }

                func serialize() -> string {
                    return this.value
                }

                func clone() -> any {
                    return Data(this.value)
                }
            }

            func process<T: ISerializable & ICloneable>(item:T) -> string {
                cloned <- item.clone()
                return item.serialize()
            }

            data <- Data(""test"")
            result <- process<Data>(data)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("test", result);
    }

    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericFunction_WhereClause_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IComparable {
                func compareTo(other:any) -> int
            }

            class Item {
                public id:int

                func init(i:int) -> void {
                    this.id <- i
                }

                func compareTo(other:any) -> int {
                    otherItem <- other as Item
                    if this.id > otherItem.id {
                        return 1
                    } elif this.id < otherItem.id {
                        return -1
                    } else {
                        return 0
                    }
                }
            }

            func findMax<T>(a:T, b:T) -> T where T: IComparable {
                if a.compareTo(b) > 0 {
                    return a
                } else {
                    return b
                }
            }

            item1 <- Item(5)
            item2 <- Item(10)
            maxItem <- findMax<Item>(item1, item2)
            result <- maxItem.id
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(10, result);
    }

    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericClass_SingleConstraint_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IComparable {
                func compareTo(other:any) -> int
            }

            class Number {
                public value:int

                func init(v:int) -> void {
                    this.value <- v
                }

                func compareTo(other:any) -> int {
                    otherNum <- other as Number
                    if this.value > otherNum.value {
                        return 1
                    } elif this.value < otherNum.value {
                        return -1
                    } else {
                        return 0
                    }
                }
            }

            class Container<T: IComparable> {
                public item:T

                func init(i:T) -> void {
                    this.item <- i
                }

                func compare(other:T) -> int {
                    return this.item.compareTo(other)
                }
            }

            n1 <- Number(15)
            n2 <- Number(10)
            container <- Container<Number>(n1)
            result <- container.compare(n2)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(1, result);
    }

    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericClass_MultipleConstraints_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface ISerializable {
                func serialize() -> string
            }

            interface ICloneable {
                func clone() -> any
            }

            class Entity {
                public name:string

                func init(n:string) -> void {
                    this.name <- n
                }

                func serialize() -> string {
                    return ""Entity:"" + this.name
                }

                func clone() -> any {
                    return Entity(this.name)
                }
            }

            class Repository<T: ISerializable & ICloneable> {
                public data:T

                func init(d:T) -> void {
                    this.data <- d
                }

                func save() -> string {
                    return this.data.serialize()
                }

                func duplicate() -> any {
                    return this.data.clone()
                }
            }

            entity <- Entity(""User"")
            repo <- Repository<Entity>(entity)
            result <- repo.save()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Entity:User", result);
    }

    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericFunction_MultipleTypeParametersWithConstraints_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IComparable {
                func compareTo(other:any) -> int
            }

            interface ISerializable {
                func serialize() -> string
            }

            class Key {
                public id:int

                func init(i:int) -> void {
                    this.id <- i
                }

                func compareTo(other:any) -> int {
                    otherKey <- other as Key
                    if this.id > otherKey.id {
                        return 1
                    } elif this.id < otherKey.id {
                        return -1
                    } else {
                        return 0
                    }
                }
            }

            class Value {
                public data:string

                func init(d:string) -> void {
                    this.data <- d
                }

                func serialize() -> string {
                    return this.data
                }
            }

            func createPair<K, V>(key:K, value:V) -> string where K: IComparable, V: ISerializable {
                return key.id.ToStr() + "":"" + value.serialize()
            }

            k <- Key(1)
            v <- Value(""test"")
            result <- createPair<Key, Value>(k, v)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("1:test", result);
    }

    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericFunction_ConstraintWithInheritance_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IBase {
                func getValue() -> int
            }

            interface IDerived extends IBase {
                func getDoubleValue() -> int
            }

            class MyClass {
                public value:int

                func init(v:int) -> void {
                    this.value <- v
                }

                func getValue() -> int {
                    return this.value
                }

                func getDoubleValue() -> int {
                    return this.value * 2
                }
            }

            func process<T: IDerived>(item:T) -> int {
                return item.getDoubleValue()
            }

            obj <- MyClass(5)
            result <- process<MyClass>(obj)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(10, result);
    }

    [Fact(Skip = "虚拟机泛型约束功能可能需要进一步实现")]
    public void GenericClass_NestedConstraints_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IComparable {
                func compareTo(other:any) -> int
            }

            class Number {
                public value:int

                func init(v:int) -> void {
                    this.value <- v
                }

                func compareTo(other:any) -> int {
                    otherNum <- other as Number
                    if this.value > otherNum.value {
                        return 1
                    } elif this.value < otherNum.value {
                        return -1
                    } else {
                        return 0
                    }
                }
            }

            class SortedList<T: IComparable> {
                public items:list

                func init() -> void {
                    this.items <- {}
                }

                func add(item:T) -> void {
                    this.items.Add(item)
                }

                func getFirst() -> T {
                    return this.items[0]
                }
            }

            class Container<T: IComparable> {
                public list:SortedList<T>

                func init() -> void {
                    this.list <- SortedList<T>()
                }

                func addItem(item:T) -> void {
                    this.list.add(item)
                }

                func getFirstValue() -> int {
                    first <- this.list.getFirst()
                    return first.value
                }
            }

            container <- Container<Number>()
            container.addItem(Number(10))
            container.addItem(Number(20))
            result <- container.getFirstValue()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(10, result);
    }
}
