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
}
