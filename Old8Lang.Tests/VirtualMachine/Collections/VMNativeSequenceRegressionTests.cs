namespace Old8Lang.Tests.VirtualMachine.Collections;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

public class VMNativeSequenceRegressionTests
{
    [Fact]
    public void ArraySortingAlgorithms_CompareStringsOrdinally()
    {
        const string code = @"
            values <- [""z"", ""aa"", ""b""]
            quick <- values.QuickSort().ToStr()
            heap <- values.HeapSort().ToStr()
            selection <- values.SelectionSort().ToStr()
            insertion <- values.InsertionSort().ToStr()
            merge <- values.MergeSort().ToStr()
            bubble <- values.BubbleSort().ToStr()
        ";
        var vm = new VM(CompileHelper.CompileToBytecode(code));
        vm.Execute();
        foreach (var name in new[] { "quick", "heap", "selection", "insertion", "merge", "bubble" })
            Assert.Equal("[\"aa\", \"b\", \"z\"]", vm.GetGlobalVariable(name));
    }

    [Fact]
    public void ArrayAndListSequenceMethods_AcceptMixedRepresentations()
    {
        const string code = @"
            a <- [1, 2, 2, 3]
            b <- {2, 4}
            take <- a.Take(2).ToStr()
            skip <- a.Skip(2).ToStr()
            concat <- a.Concat(b).ToStr()
            union <- a.Union(b).ToStr()
            intersect <- a.Intersect(b).ToStr()
            except <- a.Except(b).ToStr()
            reverse <- a.Reverse().ToStr()
            distinct <- a.Distinct().ToStr()
            mixed <- b.Concat(a).ToStr()
            union2 <- b.Union(a).ToStr()
            empty <- [].Take(2).ToStr()
            negative <- a.Skip(-1).ToStr()
            total <- b.Sum()
            average <- a.Average()
            minimum <- a.Min()
            maximum <- b.Max()
        ";
        var vm = new VM(CompileHelper.CompileToBytecode(code));
        vm.Execute();
        Assert.Equal("[1, 2]", vm.GetGlobalVariable("take"));
        Assert.Equal("[2, 3]", vm.GetGlobalVariable("skip"));
        Assert.Equal("[1, 2, 2, 3, 2, 4]", vm.GetGlobalVariable("concat"));
        Assert.Equal("[1, 2, 2, 3, 4]", vm.GetGlobalVariable("union"));
        Assert.Equal("[2]", vm.GetGlobalVariable("intersect"));
        Assert.Equal("[1, 3]", vm.GetGlobalVariable("except"));
        Assert.Equal("[3, 2, 2, 1]", vm.GetGlobalVariable("reverse"));
        Assert.Equal("[1, 2, 3]", vm.GetGlobalVariable("distinct"));
        Assert.Equal("[2, 4, 1, 2, 2, 3]", vm.GetGlobalVariable("mixed"));
        Assert.Equal("[2, 4, 1, 3]", vm.GetGlobalVariable("union2"));
        Assert.Equal("[]", vm.GetGlobalVariable("empty"));
        Assert.Equal("[1, 2, 2, 3]", vm.GetGlobalVariable("negative"));
        Assert.Equal(6, vm.GetGlobalVariable("total"));
        Assert.Equal(2.0, vm.GetGlobalVariable("average"));
        Assert.Equal(1, vm.GetGlobalVariable("minimum"));
        Assert.Equal(4, vm.GetGlobalVariable("maximum"));
    }

    [Fact]
    public void LangListMethods_WorkOnNativeArraysAndListsWithEdgeCases()
    {
        const string code = @"
            arr <- [3, 1, 2]
            lst <- {10, 20, 30}

            firstArr <- arr.First()
            lastArr <- arr.Last()
            firstDef <- [].FirstOrDefault(99)
            lastDef <- {}.LastOrDefault(-1)
            elemNeg <- arr.ElementAt(-1)
            hasTwo <- arr.Contains(2)
            idxTwo <- arr.IndexOf(2)
            cntArr <- arr.Count()
            mapped <- arr.Map((x) -> x * 2).ToStr()
            filtered <- arr.Filter((x) -> x >= 2).ToStr()
            found <- arr.Find((x) -> x == 2)
            anyGt <- arr.Any((x) -> x > 2)
            allPos <- arr.All((x) -> x > 0)
            reduced <- arr.Reduce((a, b) -> a + b)
            joined <- arr.Join(""-"")
            zipped <- arr.Zip(lst).Count()
            zip3Count <- arr.Zip3(lst, [7, 8, 9]).Count()
            selectMany <- arr.SelectMany((x) -> [x, x * 10]).ToStr()
            sortedMixed <- [10, 2.5, 3].SortBy((x) -> x).ToStr()
            stableDesc <- {{""k"": 1, ""id"": ""a""}, {""k"": 2, ""id"": ""b""}, {""k"": 1, ""id"": ""c""}}.SortBy((x) -> x[""k""], false).Map((x) -> x[""id""]).ToStr()
            minMixed <- [10, 2.5, 3].Min((x) -> x)
            maxMixed <- [10, 2.5, 3].Max((x) -> x)
            sumSel <- arr.Sum((x) -> x * 2)
            avgSel <- arr.Average((x) -> x * 2)
            convertedList <- arr.ToList()
            convertedList.Add(99)
            convertedListStr <- convertedList.ToStr()
            convertedArrLen <- lst.ToArray().Count()
            isSortedArr <- [1, 2, 3].IsSorted()
        ";
        var vm = new VM(CompileHelper.CompileToBytecode(code));
        vm.Execute();

        Assert.Equal(3, vm.GetGlobalVariable("firstArr"));
        Assert.Equal(2, vm.GetGlobalVariable("lastArr"));
        Assert.Equal(99, vm.GetGlobalVariable("firstDef"));
        Assert.Equal(-1, vm.GetGlobalVariable("lastDef"));
        Assert.Equal(2, vm.GetGlobalVariable("elemNeg"));
        Assert.Equal(true, vm.GetGlobalVariable("hasTwo"));
        Assert.Equal(2, vm.GetGlobalVariable("idxTwo"));
        Assert.Equal(3, vm.GetGlobalVariable("cntArr"));
        Assert.Equal("[6, 2, 4]", vm.GetGlobalVariable("mapped"));
        Assert.Equal("[3, 2]", vm.GetGlobalVariable("filtered"));
        Assert.Equal(2, vm.GetGlobalVariable("found"));
        Assert.Equal(true, vm.GetGlobalVariable("anyGt"));
        Assert.Equal(true, vm.GetGlobalVariable("allPos"));
        Assert.Equal(6, vm.GetGlobalVariable("reduced"));
        Assert.Equal("3-1-2", vm.GetGlobalVariable("joined"));
        Assert.Equal(3, vm.GetGlobalVariable("zipped"));
        Assert.Equal(3, vm.GetGlobalVariable("zip3Count"));
        Assert.Equal("[3, 30, 1, 10, 2, 20]", vm.GetGlobalVariable("selectMany"));
        Assert.Equal("[2.5, 3, 10]", vm.GetGlobalVariable("sortedMixed"));
        Assert.Equal("[\"b\", \"a\", \"c\"]", vm.GetGlobalVariable("stableDesc"));
        Assert.Equal(2.5, vm.GetGlobalVariable("minMixed"));
        Assert.Equal(10, vm.GetGlobalVariable("maxMixed"));
        Assert.Equal(12, vm.GetGlobalVariable("sumSel"));
        Assert.Equal(4.0, vm.GetGlobalVariable("avgSel"));
        Assert.Equal("[3, 1, 2, 99]", vm.GetGlobalVariable("convertedListStr"));
        Assert.Equal(3, vm.GetGlobalVariable("convertedArrLen"));
        Assert.Equal(true, vm.GetGlobalVariable("isSortedArr"));
    }

    [Fact]
    public void EmptyFirstLastAndOutOfRangeElementAt_ThrowExpectedErrorsInVm()
    {
        var emptyFirst = new VM(CompileHelper.CompileToBytecode("x <- [].First()"));
        Assert.Throws<InvalidOperationException>(() => emptyFirst.Execute());

        var emptyLast = new VM(CompileHelper.CompileToBytecode("x <- {}.Last()"));
        Assert.Throws<InvalidOperationException>(() => emptyLast.Execute());

        var outOfRange = new VM(CompileHelper.CompileToBytecode("x <- [1, 2].ElementAt(-5)"));
        Assert.Throws<IndexOutOfRangeException>(() => outOfRange.Execute());
    }
}
