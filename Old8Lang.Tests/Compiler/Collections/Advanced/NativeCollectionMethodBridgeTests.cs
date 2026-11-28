using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

/// <summary>
/// IL 模式下原生集合接收者通过运行期桥接调用解释器实例实现的测试。
/// 覆盖未实现 <c>IIlNativeValueInstanceMethod</c> 的 List / Dictionary / Array 剩余方法。
/// </summary>
[Collection("Sequential")]
public class NativeCollectionMethodBridgeTests
{
    private static void RunInIlMode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #region List 纯函数

    [Fact]
    public void ListChunk_AndChainedCount_Works()
    {
        RunInIlMode(@"
            chunks <- {1, 2, 3, 4, 5}.Chunk(2)
            Assert.Equal(3, chunks.Count())

            firstChunk <- chunks[0]
            Assert.Equal(2, firstChunk.Count())
            Assert.Equal(1, firstChunk.First())
            Assert.Equal(2, firstChunk.Last())
        ");
    }

    [Fact]
    public void ListFlattenPairwiseWindow_Work()
    {
        RunInIlMode(@"
            flat <- {{1, 2}, {3, 4, 5}}.Flatten()
            Assert.Equal(5, flat.Count())
            Assert.Equal(1, flat.First())
            Assert.Equal(5, flat.Last())

            pairs <- {1, 2, 3, 4}.Pairwise()
            Assert.Equal(3, pairs.Count())

            windows <- {1, 2, 3, 4}.Window(3)
            Assert.Equal(2, windows.Count())
        ");
    }

    [Fact]
    public void ListSliceTakeLastSkipLast_Work()
    {
        RunInIlMode(@"
            part <- {1, 2, 3, 4, 5}.Slice(1, 4)
            Assert.Equal(3, part.Count())
            Assert.Equal(2, part.First())
            Assert.Equal(4, part.Last())

            tail <- {1, 2, 3, 4, 5}.TakeLast(2)
            Assert.Equal(2, tail.Count())
            Assert.Equal(5, tail.Last())

            head <- {1, 2, 3, 4, 5}.SkipLast(2)
            Assert.Equal(3, head.Count())
            Assert.Equal(3, head.Last())
        ");
    }

    [Fact]
    public void ListAggregate_Works()
    {
        RunInIlMode(@"
            total <- {1, 2, 3, 4}.Aggregate((acc:int, x:int) -> acc + x)
            Assert.Equal(10, total)

            seeded <- {1, 2, 3}.Aggregate((acc:int, x:int) -> acc + x, 100)
            Assert.Equal(106, seeded)
        ");
    }

    [Fact]
    public void ListSetOperations_Work()
    {
        RunInIlMode(@"
            Assert.True({1, 2}.IsSubsetOf({1, 2, 3}))
            Assert.True({1, 2, 3}.IsSupersetOf({2, 3}))
            Assert.True({1, 2, 3}.SetEquals({3, 2, 1}))
            Assert.True({1, 2, 3}.Overlaps({3, 4}))
            Assert.Equal(3, {1, 2, 3}.SymmetricExcept({3, 4}).Count())
        ");
    }

    [Fact]
    public void ListSortHelpers_Work()
    {
        RunInIlMode(@"
            sorted <- {3, 1, 2}.BubbleSort()
            Assert.Equal(3, sorted.Count())
            Assert.Equal(1, sorted.First())
            Assert.Equal(3, sorted.Last())

            quick <- {5, 2, 9, 1}.QuickSort()
            Assert.Equal(1, quick.First())
            Assert.Equal(9, quick.Last())
        ");
    }

    [Fact]
    public void ListCombinatorics_Work()
    {
        RunInIlMode(@"
            pairs <- {1, 2}.CartesianProduct({3, 4})
            Assert.Equal(4, pairs.Count())

            combos <- {1, 2, 3}.Combinations(2)
            Assert.Equal(3, combos.Count())

            perms <- {1, 2, 3}.Permutations()
            Assert.Equal(6, perms.Count())
        ");
    }

    #endregion

    #region List 变异

    [Fact]
    public void ListMutators_ModifyNativeReceiver()
    {
        RunInIlMode(@"
            items <- {1, 2, 3}
            items.Insert(1, 9)
            items.Remove(2)
            removed <- items.RemoveAt(0)
            Assert.Equal(1, removed)
            last <- items.Pop()
            Assert.Equal(3, last)

            Assert.Equal(1, items.Count())
            Assert.Equal(9, items.First())

            items.Clear()
            Assert.True(items.IsEmpty())
        ");
    }

    [Fact]
    public void ListRemove_ReturnsRemovedElement()
    {
        RunInIlMode(@"
            numbers <- {1, 2, 3}
            removed <- numbers.Remove(2)
            Assert.Equal(2, removed)
            Assert.Equal(2, numbers.Count())
            Assert.Equal(1, numbers.First())
            Assert.Equal(3, numbers.Last())
        ");
    }

    [Fact]
    public void ListClear_VisibleThroughAlias()
    {
        RunInIlMode(@"
            a <- {1, 2, 3}
            b <- a
            a.Clear()
            Assert.True(b.IsEmpty())
        ");
    }

    #endregion

    #region Dictionary

    [Fact]
    public void DictionaryQueryMethods_Work()
    {
        RunInIlMode(@"
            d <- {""a"": 1, ""b"": 2}
            Assert.True(d.ContainsKey(""a""))
            Assert.True(!d.ContainsKey(""z""))
            Assert.True(d.ContainsValue(2))
            Assert.Equal(1, d.GetValue(""a""))
            Assert.Equal(99, d.GetOrElse(""z"", 99))
            Assert.Equal(2, d.Count())
            Assert.True(!d.IsEmpty())
        ");
    }

    [Fact]
    public void DictionaryUpdate_ModifiesNativeReceiver()
    {
        RunInIlMode(@"
            d <- {""a"": 1, ""b"": 2}
            d.Update({""b"": 20, ""c"": 30})
            Assert.Equal(20, d[""b""])
            Assert.Equal(30, d[""c""])
            Assert.Equal(3, d.Count())
        ");
    }

    [Fact]
    public void DictionaryCloneAndMerge_ReturnIndependentDictionaries()
    {
        RunInIlMode(@"
            d <- {""a"": 1, ""b"": 2}
            copy <- d.Clone()
            Assert.True(copy.ContainsKey(""b""))
            copy.Add(""c"", 3)
            Assert.Equal(2, d.Count())
            Assert.Equal(3, copy.Count())

            merged <- d.Merge({""b"": 999, ""d"": 4})
            Assert.Equal(2, merged[""b""])
            Assert.Equal(4, merged[""d""])
        ");
    }

    #endregion

    #region Array

    [Fact]
    public void ArrayGetSetSlice_Work()
    {
        RunInIlMode(@"
            arr <- [1, 2, 3, 4, 5]
            Assert.Equal(3, arr.Get(2))

            arr.Set(0, 100)
            Assert.Equal(100, arr[0])

            part <- arr.Slice(1, 4)
            Assert.Equal(3, part.Length)
            Assert.Equal(2, part[0])
        ");
    }

    [Fact]
    public void ArraySet_VisibleThroughAlias()
    {
        RunInIlMode(@"
            a <- [1, 2, 3]
            b <- a
            a.Set(1, 20)
            Assert.Equal(20, b[1])
        ");
    }

    #endregion

    #region 旧桩与动态接收者

    [Fact]
    public void ListGroupBy_PreviouslyLdnull_Works()
    {
        RunInIlMode(@"
            grouped <- {1, 2, 3, 4}.GroupBy((x:int) -> x % 2)
            keys <- grouped.Keys()
            Assert.Equal(2, keys.Count())

            evens <- grouped[""0""]
            Assert.Equal(2, evens.Count())
            Assert.Equal(2, evens.First())
            Assert.Equal(4, evens.Last())
        ");
    }

    [Fact]
    public void ListZipWithZipper_PreviouslyUnsupported_Works()
    {
        RunInIlMode(@"
            summed <- {1, 2, 3}.Zip({10, 20, 30}, (a:int, b:int) -> a + b)
            Assert.Equal(3, summed.Count())
            Assert.Equal(11, summed.First())
            Assert.Equal(33, summed.Last())
        ");
    }

    [Fact]
    public void DynamicReceiver_ChainedBridgeCall_Works()
    {
        RunInIlMode(@"
            nested <- {""data"": {1, 2, 3, 4}}
            values <- nested[""data""]
            chunks <- values.Chunk(2)
            Assert.Equal(2, chunks.Count())
        ");
    }

    #endregion

    #region 其他 List 方法

    [Fact]
    public void ListWithIndexGroupAdjacent_Work()
    {
        RunInIlMode(@"
            indexed <- {""a"", ""b""}.WithIndex()
            Assert.Equal(2, indexed.Count())

            groups <- {1, 1, 2, 2, 3}.GroupAdjacent()
            Assert.Equal(3, groups.Count())
        ");
    }

    [Fact]
    public void ListPredicateAndSearchHelpers_Work()
    {
        RunInIlMode(@"
            Assert.Equal(4, {1, 2, 3, 4}.First((x:int) -> x > 3))
            Assert.Equal(4, {1, 2, 3, 4}.Last((x:int) -> x < 5))
            Assert.Equal(9, {1, 2}.ElementAtOrDefault(5, 9))
            Assert.Equal(1, {1, 2, 3}.LastIndexOf(2))

            evens <- {1, 2, 3, 4}.FindAll((x:int) -> x % 2 == 0)
            Assert.Equal(2, evens.Count())

            head <- {1, 2, 3, 4}.TakeWhile((x:int) -> x < 3)
            Assert.Equal(2, head.Count())

            deep <- {1, {2, {3}}}.FlattenDeep()
            Assert.Equal(3, deep.Count())
        ");
    }

    [Fact]
    public void ListSortWithComparer_Works()
    {
        RunInIlMode(@"
            sorted <- {3, 1, 2}.SortWith((a:int, b:int) -> a - b)
            Assert.Equal(1, sorted.First())
            Assert.Equal(3, sorted.Last())
        ");
    }

    [Fact]
    public void ChainedTypedResult_ContinuesResolvingMethods()
    {
        RunInIlMode(@"
            chunks <- {3, 1, 2, 4}.Sort().Chunk(2)
            Assert.Equal(2, chunks.Count())

            first <- chunks[0]
            Assert.Equal(1, first.First())
            Assert.Equal(2, first.Last())
        ");
    }

    #endregion

    #region 其他 Dictionary 方法

    [Fact]
    public void DictionaryAddRemoveClear_ModifyNativeReceiver()
    {
        RunInIlMode(@"
            d <- {""a"": 1}
            d.Add(""b"", 2)
            Assert.Equal(2, d.Count())

            row <- d.ToList()
            Assert.Equal(2, row.Length)
            firstPair <- row.First()
            Assert.Equal(""a"", firstPair[0])
            Assert.Equal(1, firstPair[1])

            Assert.True(d.Remove(""a""))
            Assert.True(!d.ContainsKey(""a""))
            Assert.Equal(1, d.Count())

            d.Clear()
            Assert.True(d.IsEmpty())
        ");
    }

    [Fact]
    public void DictionaryDeleteAlias_SyncsNativeReceiver()
    {
        RunInIlMode(@"
            d <- {""a"": 1, ""b"": 2}
            Assert.True(d.Delete(""a""))
            Assert.True(!d.ContainsKey(""a""))
            Assert.Equal(1, d.Count())
        ");
    }

    #endregion

    #region 其他 Array 方法

    [Fact]
    public void ArrayQueryAndGroupMethods_Work()
    {
        RunInIlMode(@"
            arr <- [1, 2, 3]
            Assert.True(arr.Contains(2))

            groups <- [1, 1, 2].GroupAdjacent()
            Assert.Equal(2, groups.Count())

            perms <- [1, 2].Permutations()
            Assert.Equal(2, perms.Count())

            Assert.True([1, 2].SetEquals([2, 1]))
            Assert.True([1, 2].Overlaps([2, 3]))
        ");
    }

    [Fact]
    public void HotspotNativeIlMethods_ChunkFlattenSliceAndAllSorts_WorkWithoutBridge()
    {
        RunInIlMode(@"
            chunks <- {1, 2, 3, 4, 5}.Chunk(2)
            Assert.Equal(3, chunks.Count())
            Assert.Equal({1, 2}, chunks[0])
            Assert.Equal({5}, chunks[2])

            flat <- {{1, 2}, {3}, 4}.Flatten()
            Assert.Equal({1, 2, 3, 4}, flat)

            deep <- {1, {2, {3, {4}}}}.FlattenDeep()
            Assert.Equal({1, 2, 3, 4}, deep)

            ls <- {10, 20, 30, 40}.Slice(1, 3)
            Assert.Equal({20, 30}, ls)

            aslice <- [10, 20, 30, 40, 50].Slice(1, 5, 2)
            Assert.Equal([20, 40], aslice)

            words <- {""z"", ""aa"", ""b""}
            Assert.Equal({""aa"", ""b"", ""z""}, words.QuickSort())
            Assert.Equal({""aa"", ""b"", ""z""}, words.MergeSort())
            Assert.Equal({""aa"", ""b"", ""z""}, words.HeapSort())
            Assert.Equal({""aa"", ""b"", ""z""}, words.InsertionSort())
            Assert.Equal({""aa"", ""b"", ""z""}, words.SelectionSort())
            Assert.Equal({""aa"", ""b"", ""z""}, words.BubbleSort())

            arrWords <- [""z"", ""aa"", ""b""]
            Assert.Equal([""aa"", ""b"", ""z""], arrWords.QuickSort())
            Assert.Equal([""aa"", ""b"", ""z""], arrWords.MergeSort())
            Assert.Equal([""aa"", ""b"", ""z""], arrWords.HeapSort())
            Assert.Equal([""aa"", ""b"", ""z""], arrWords.InsertionSort())
            Assert.Equal([""aa"", ""b"", ""z""], arrWords.SelectionSort())
            Assert.Equal([""aa"", ""b"", ""z""], arrWords.BubbleSort())
        ");
    }

    [Fact]
    public void NestedDictionaryBridge_AndTupleDestructuring_WorkInIl()
    {
        RunInIlMode(@"
            // 嵌套字典经桥接边界深度填充
            nested <- {""outer"": {""a"": 1, ""b"": 2}}
            cloned <- nested.Clone()
            Assert.Equal(1, cloned[""outer""][""a""])
            Assert.Equal(2, cloned[""outer""][""b""])

            listOfDicts <- {{""k"": 10}, {""k"": 20}}
            chunks <- listOfDicts.Chunk(1)
            Assert.Equal(10, chunks[0][0][""k""])
            Assert.Equal(20, chunks[1][0][""k""])

            // 桥接结果中的元组可直接解构
            zipped <- {1, 2}.Zip({10, 20})
            (za, zb) <- zipped[0]
            Assert.Equal(1, za)
            Assert.Equal(10, zb)

            indexed <- {""x"", ""y""}.WithIndex()
            (idx, val) <- indexed[1]
            Assert.Equal(1, idx)
            Assert.Equal(""y"", val)

            d <- {""only"": 42}
            rows <- d.ToList()
            (dk, dv) <- rows[0]
            Assert.Equal(""only"", dk)
            Assert.Equal(42, dv)
        ");
    }

    [Fact]
    public void DictionaryAddDuplicateKey_ConsistentAcrossAllThreeModes()
    {
        const string code = @"
            d <- {""a"": 1}
            d.Add(""a"", 99)
            cnt <- d.Count()
            val <- d[""a""]
        ";
        // Interpreter
        var interp = new LangInterpreter();
        interp.Build(code).Run(interp.Manager);
        Assert.Equal(1, ((AST.Expression.Value.IntLangValue)interp.Manager.GetValue(new AST.Expression.LangId("cnt"))!).Value);
        Assert.Equal(99, ((AST.Expression.Value.IntLangValue)interp.Manager.GetValue(new AST.Expression.LangId("val"))!).Value);

        // IL
        RunInIlMode(code + "\nAssert.Equal(1, cnt)\nAssert.Equal(99, val)");

        // VM
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(VirtualMachine.CompileHelper.CompileToBytecode(code));
        vm.Execute();
        Assert.Equal(1, vm.GetGlobalVariable("cnt"));
        Assert.Equal(99, vm.GetGlobalVariable("val"));
    }

    [Fact]
    public void AuditedValueTypeCharAndDynamicIndexMethods_EmitValidIl()
    {
        RunInIlMode(@"
            h <- (42).ToHash()
            Assert.True(h == (42).ToHash())
            Assert.True((42).Equal(42))
            Assert.True(!(42).Equal(43))
            t <- (42).ToType()
            Assert.Equal(""Int"", t.ToStr())

            Assert.True('5'.IsDigit())
            Assert.True('a'.CompareTo('b') < 0)

            indices <- {1}
            idx <- indices[0]
            Assert.Equal(20, {10, 20, 30}.ElementAt(idx))
        ");
    }

    #endregion

    #region 解释器对照

    [Fact]
    public void BridgeBehaviors_MatchInterpreterMode()
    {
        const string code = @"
            total <- {1, 2, 3, 4}.Aggregate((acc:int, x:int) -> acc + x)
            Assert.Equal(10, total)

            items <- {1, 2, 3}
            items.Insert(1, 9)
            items.Remove(2)
            Assert.Equal(3, items.Count())
            Assert.Equal(9, items[1])

            groups <- {1, 2, 3, 4}.GroupBy((x:int) -> x % 2)
            Assert.Equal(2, groups.Keys().Count())
        ";

        RunInInterpreterMode(code);
        RunInIlMode(code);
    }

    private static void RunInInterpreterMode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
    }

    #endregion
}
