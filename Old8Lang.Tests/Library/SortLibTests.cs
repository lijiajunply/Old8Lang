using Old8LangLib;

namespace Old8Lang.Tests.Library;

/// <summary>
/// 排序算法模块测试
/// </summary>
public class SortLibTests
{
    private readonly int[] UnsortedArray = [3, 1, 4, 1, 5, 9, 2, 6];
    private readonly int[] SortedArray = [1, 1, 2, 3, 4, 5, 6, 9];
    private readonly int[] EmptyArray = [];
    private readonly int[] SingleElementArray = [42];
    private readonly string[] UnsortedStringArray = ["banana", "apple", "cherry", "date", "blueberry"];
    private readonly string[] SortedStringArray = ["apple", "banana", "blueberry", "cherry", "date"];

    [Fact]
    public void QuickSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.QuickSort(UnsortedArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray, result);
    }

    [Fact]
    public void QuickSort_AlreadySortedArray_ReturnsSameArray()
    {
        // Act
        var result = SortLib.QuickSort(SortedArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray, result);
    }

    [Fact]
    public void QuickSort_EmptyArray_ReturnsEmptyArray()
    {
        // Act
        var result = SortLib.QuickSort(EmptyArray);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void QuickSort_SingleElementArray_ReturnsSameArray()
    {
        // Act
        var result = SortLib.QuickSort(SingleElementArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SingleElementArray, result);
    }

    [Fact]
    public void QuickSort_StringArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.QuickSort(UnsortedStringArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedStringArray, result);
    }

    [Fact]
    public void MergeSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.MergeSort(UnsortedArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray, result);
    }

    [Fact]
    public void BubbleSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.BubbleSort(UnsortedArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray, result);
    }

    [Fact]
    public void SelectionSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.SelectionSort(UnsortedArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray, result);
    }

    [Fact]
    public void InsertionSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.InsertionSort(UnsortedArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray, result);
    }

    [Fact]
    public void HeapSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.HeapSort(UnsortedArray);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray, result);
    }

    [Fact]
    public void IsSorted_SortedArray_ReturnsTrue()
    {
        // Act
        var result = SortLib.IsSorted(SortedArray);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSorted_UnsortedArray_ReturnsFalse()
    {
        // Act
        var result = SortLib.IsSorted(UnsortedArray);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SortList_QuickSort_ReturnsSortedList()
    {
        // Arrange
        var list = UnsortedArray.ToList();

        // Act
        var result = SortLib.SortList(list);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_MergeSort_ReturnsSortedList()
    {
        // Arrange
        var list = UnsortedArray.ToList();

        // Act
        var result = SortLib.SortList(list, "merge");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_BubbleSort_ReturnsSortedList()
    {
        // Arrange
        var list = UnsortedArray.ToList();

        // Act
        var result = SortLib.SortList(list, "bubble");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_SelectionSort_ReturnsSortedList()
    {
        // Arrange
        var list = UnsortedArray.ToList();

        // Act
        var result = SortLib.SortList(list, "selection");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_InsertionSort_ReturnsSortedList()
    {
        // Arrange
        var list = UnsortedArray.ToList();

        // Act
        var result = SortLib.SortList(list, "insertion");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_HeapSort_ReturnsSortedList()
    {
        // Arrange
        var list = UnsortedArray.ToList();

        // Act
        var result = SortLib.SortList(list, "heap");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SortedArray.ToList(), result);
    }

    [Fact]
    public void MergeSort_NullArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SortLib.MergeSort<int>(null!));
    }

    [Fact]
    public void SortList_InvalidSortType_ThrowsArgumentException()
    {
        // Arrange
        var list = UnsortedArray.ToList();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SortLib.SortList(list, "invalid"));
    }

    [Fact]
    public void QuickSort_NullArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SortLib.QuickSort<int>(null!));
    }

    [Fact]
    public void IsSorted_NullArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SortLib.IsSorted<int>(null!));
    }

    [Fact]
    public void SortList_NullList_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SortLib.SortList<int>(null!, "quick"));
    }
}