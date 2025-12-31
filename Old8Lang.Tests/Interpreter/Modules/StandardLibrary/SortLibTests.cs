using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// SortLib 库测试 - 测试排序算法功能
/// </summary>
public class SortLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Sort_ShouldWorkCorrectly()
    {
        var code = @"
import Sort

PrintLine(""Sort library imported"")
";
        CreateTempModuleFile("./StandardLibrary/sort_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/sort_test.old8");

        Assert.Null(exception);
        var sortLib = interpreter.Manager.GetValue(new LangId("Sort"));
        Assert.NotNull(sortLib);
        Assert.IsAssignableFrom<IModuleValueType>(sortLib);
    }

    [Fact]
    public void QuickSort_ShouldSortArray()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9, 3, 7]
sorted <- Sort.QuickSort(arr)
PrintLine($""Original: {arr}"")
PrintLine($""Sorted: {sorted}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_quicksort_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_quicksort_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void MergeSort_ShouldSortArray()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9, 3, 7]
sorted <- Sort.MergeSort(arr)
PrintLine($""Original: {arr}"")
PrintLine($""Sorted: {sorted}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_mergesort_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_mergesort_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void BubbleSort_ShouldSortArray()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9]
sorted <- Sort.BubbleSort(arr)
PrintLine($""Original: {arr}"")
PrintLine($""Sorted: {sorted}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_bubblesort_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_bubblesort_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void SelectionSort_ShouldSortArray()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9]
sorted <- Sort.SelectionSort(arr)
PrintLine($""Original: {arr}"")
PrintLine($""Sorted: {sorted}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_selectionsort_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_selectionsort_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void InsertionSort_ShouldSortArray()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9]
sorted <- Sort.InsertionSort(arr)
PrintLine($""Original: {arr}"")
PrintLine($""Sorted: {sorted}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_insertionsort_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_insertionsort_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HeapSort_ShouldSortArray()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9]
sorted <- Sort.HeapSort(arr)
PrintLine($""Original: {arr}"")
PrintLine($""Sorted: {sorted}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_heapsort_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_heapsort_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void IsSorted_ShouldReturnTrue_ForSortedArray()
    {
        var code = @"
import Sort

arr <- [1, 2, 3, 4, 5]
result <- Sort.IsSorted(arr)
PrintLine($""Array: {arr}"")
PrintLine($""Is sorted: {result}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_issorted_true_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_issorted_true_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void IsSorted_ShouldReturnFalse_ForUnsortedArray()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9]
result <- Sort.IsSorted(arr)
PrintLine($""Array: {arr}"")
PrintLine($""Is sorted: {result}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_issorted_false_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_issorted_false_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void MultipleAlgorithms_ShouldProduceSameResult()
    {
        var code = @"
import Sort

arr <- [5, 2, 8, 1, 9, 3, 7, 6, 4]
sorted1 <- Sort.QuickSort(arr)
sorted2 <- Sort.MergeSort(arr)
sorted3 <- Sort.BubbleSort(arr)
PrintLine($""Original: {arr}"")
PrintLine($""QuickSort: {sorted1}"")
PrintLine($""MergeSort: {sorted2}"")
PrintLine($""BubbleSort: {sorted3}"")
";
        CreateTempModuleFile("./StandardLibrary/sort_multiple_algorithms_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/sort_multiple_algorithms_test.old8");

        Assert.Null(exception);
    }
}
