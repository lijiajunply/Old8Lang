using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机嵌套泛型测试
/// 测试嵌套泛型类型的功能
/// </summary>
public class VMGenericNestedTests
{
    [Fact]
    public void GenericFunction_NestedList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createNestedList<T>() -> list {
                outer <- {}
                inner1 <- {}
                inner2 <- {}
                outer.Add(inner1)
                outer.Add(inner2)
                return outer
            }

            result <- createNestedList<int>()
            count <- result.Count()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var count = vm.GetGlobalVariable("count");
        Assert.NotNull(count);
        Assert.Equal(2, count);
    }

    [Fact]
    public void GenericClass_ListOfLists_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Matrix<T> {
                public rows:list

                func init() -> void {
                    this.rows <- {}
                }

                func addRow(row:list) -> void {
                    this.rows.Add(row)
                }

                func get(row:int, col:int) -> T {
                    rowData <- this.rows[row]
                    return rowData[col]
                }

                func rowCount() -> int {
                    return this.rows.Count()
                }
            }

            matrix <- Matrix<int>()
            row1 <- {1, 2, 3}
            row2 <- {4, 5, 6}
            matrix.addRow(row1)
            matrix.addRow(row2)

            result1 <- matrix.get(0, 0)
            result2 <- matrix.get(0, 2)
            result3 <- matrix.get(1, 1)
            result4 <- matrix.rowCount()
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
        Assert.Equal(1, result1);
        Assert.Equal(3, result2);
        Assert.Equal(5, result3);
        Assert.Equal(2, result4);
    }

    [Fact]
    public void GenericClass_DictionaryOfLists_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class GroupedData<K, V> {
                public groups:dict

                func init() {
                    this.groups <- dict()
                }

                func addToGroup(key:K, value:V) -> void {
                    if this.groups.ContainsKey(key) {
                        group1 <- this.groups[key]
                        group1.Add(value)
                    } else {
                        newGroup <- {}
                        newGroup.Add(value)
                        this.groups[key] <- newGroup
                    }
                }

                func getGroup(key:K) -> list {
                    return this.groups[key]
                }

                func groupCount() -> int {
                    return this.groups.Count()
                }
            }

            data <- GroupedData<string, int>()
            data.addToGroup(""A"", 1)
            data.addToGroup(""A"", 2)
            data.addToGroup(""B"", 3)
            data.addToGroup(""A"", 4)

            groupA <- data.getGroup(""A"")
            groupB <- data.getGroup(""B"")
            result1 <- groupA.Count()
            result2 <- groupB.Count()
            result3 <- data.groupCount()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(3, result1);
        Assert.Equal(1, result2);
        Assert.Equal(2, result3);
    }

    [Fact]
    public void GenericClass_NestedGenericClass_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Box<T> {
                public value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            class Container<T> {
                public box:Box<T>

                func init(v:T) -> void {
                    this.box <- Box<T>(v)
                }

                func getBoxedValue() -> T {
                    return this.box.getValue()
                }
            }

            container <- Container<int>(42)
            result <- container.getBoxedValue()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(42, result);
    }

    [Fact]
    public void GenericFunction_NestedGenericParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Wrapper<T> {
                public data:T

                func init(d:T) -> void {
                    this.data <- d
                }

                func getData() -> T {
                    return this.data
                }
            }

            func wrapTwice<T>(value:T) -> Wrapper<Wrapper<T>> {
                inner <- Wrapper<T>(value)
                outer <- Wrapper<Wrapper<T>>(inner)
                return outer
            }

            wrapped <- wrapTwice<int>(100)
            inner <- wrapped.getData()
            result <- inner.getData()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(100, result);
    }

    [Fact]
    public void GenericClass_ListOfDictionaries_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class DataStore<K, V> {
                public records:list

                func init() -> void {
                    this.records <- {}
                }

                func addRecord(record:dict) -> void {
                    this.records.Add(record)
                }

                func getRecord(index:int) -> dict {
                    return this.records[index]
                }

                func recordCount() -> int {
                    return this.records.Count()
                }
            }

            store <- DataStore<string, int>()
            record1 <- {""a"": 1, ""b"": 2}
            record2 <- {""c"": 3, ""d"": 4}
            store.addRecord(record1)
            store.addRecord(record2)

            r1 <- store.getRecord(0)
            r2 <- store.getRecord(1)
            result1 <- r1[""a""]
            result2 <- r2[""c""]
            result3 <- store.recordCount()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(1, result1);
        Assert.Equal(3, result2);
        Assert.Equal(2, result3);
    }

    [Fact]
    public void GenericClass_ThreeLevelNesting_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Level1<T> {
                public value:T

                func init(v:T) -> void {
                    this.value <- v
                }
            }

            class Level2<T> {
                public level1:Level1<T>

                func init(v:T) -> void {
                    this.level1 <- Level1<T>(v)
                }
            }

            class Level3<T> {
                public level2:Level2<T>

                func init(v:T) -> void {
                    this.level2 <- Level2<T>(v)
                }

                func getValue() -> T {
                    return this.level2.level1.value
                }
            }

            level3 <- Level3<string>(""nested"")
            result <- level3.getValue()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("nested", result);
    }

    [Fact]
    public void GenericFunction_ComplexNestedStructure_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Pair<K, V> {
                public key:K
                public value:V

                func init(k:K, v:V) -> void {
                    this.key <- k
                    this.value <- v
                }
            }

            func createNestedPairs<K, V>(k1:K, v1:V, k2:K, v2:V) -> Pair<K, Pair<K, V>> {
                innerPair <- Pair<K, V>(k2, v2)
                outerPair <- Pair<K, Pair<K, V>>(k1, innerPair)
                return outerPair
            }

            nested <- createNestedPairs<string, int>(""outer"", 1, ""inner"", 2)
            result1 <- nested.key
            result2 <- nested.value.key
            result3 <- nested.value.value
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal("outer", result1);
        Assert.Equal("inner", result2);
        Assert.Equal(2, result3);
    }

    [Fact]
    public void GenericClass_NestedWithDifferentTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Container<T> {
                public data:T

                func init(d:T) -> void {
                    this.data <- d
                }

                func getData() -> T {
                    return this.data
                }
            }

            intContainer <- Container<int>(42)
            stringContainer <- Container<string>(""test"")
            nestedContainer <- Container<Container<int>>(intContainer)

            result1 <- stringContainer.getData()
            result2 <- nestedContainer.getData().getData()
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
        Assert.Equal("test", result1);
        Assert.Equal(42, result2);
    }

    [Fact]
    public void GenericFunction_NestedListTransformation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func flattenList<T>(nestedList:list) -> list {
                result <- {}
                for innerList in nestedList {
                    for item in innerList {
                        result.Add(item)
                    }
                }
                return result
            }

            nested <- {{1, 2}, {3, 4}, {5, 6}}
            flattened <- flattenList<int>(nested)
            result1 <- flattened.Count()
            result2 <- flattened[0]
            result3 <- flattened[5]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(6, result1);
        Assert.Equal(1, result2);
        Assert.Equal(6, result3);
    }
}
