using Old8LangLib;
using Xunit;

namespace Old8Lang.Tests.Library;

/// <summary>
/// 排序算法模块测试
/// </summary>
public class SortLibTests
{
    private readonly int[] _unsortedArray = { 3, 1, 4, 1, 5, 9, 2, 6 };
    private readonly int[] _sortedArray = { 1, 1, 2, 3, 4, 5, 6, 9 };
    private readonly int[] _emptyArray = Array.Empty<int>();
    private readonly int[] _singleElementArray = { 42 };
    private readonly string[] _unsortedStringArray = { "banana", "apple", "cherry", "date", "blueberry" };
    private readonly string[] _sortedStringArray = { "apple", "banana", "blueberry", "cherry", "date" };

    [Fact]
    public void QuickSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.QuickSort(_unsortedArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray, result);
    }

    [Fact]
    public void QuickSort_AlreadySortedArray_ReturnsSameArray()
    {
        // Act
        var result = SortLib.QuickSort(_sortedArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray, result);
    }

    [Fact]
    public void QuickSort_EmptyArray_ReturnsEmptyArray()
    {
        // Act
        var result = SortLib.QuickSort(_emptyArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void QuickSort_SingleElementArray_ReturnsSameArray()
    {
        // Act
        var result = SortLib.QuickSort(_singleElementArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_singleElementArray, result);
    }

    [Fact]
    public void QuickSort_StringArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.QuickSort(_unsortedStringArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedStringArray, result);
    }

    [Fact]
    public void MergeSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.MergeSort(_unsortedArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray, result);
    }

    [Fact]
    public void BubbleSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.BubbleSort(_unsortedArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray, result);
    }

    [Fact]
    public void SelectionSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.SelectionSort(_unsortedArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray, result);
    }

    [Fact]
    public void InsertionSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.InsertionSort(_unsortedArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray, result);
    }

    [Fact]
    public void HeapSort_UnsortedArray_ReturnsSortedArray()
    {
        // Act
        var result = SortLib.HeapSort(_unsortedArray);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray, result);
    }

    [Fact]
    public void IsSorted_SortedArray_ReturnsTrue()
    {
        // Act
        var result = SortLib.IsSorted(_sortedArray);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSorted_UnsortedArray_ReturnsFalse()
    {
        // Act
        var result = SortLib.IsSorted(_unsortedArray);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SortList_QuickSort_ReturnsSortedList()
    {
        // Arrange
        var list = _unsortedArray.ToList();
        
        // Act
        var result = SortLib.SortList(list, "quick");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_MergeSort_ReturnsSortedList()
    {
        // Arrange
        var list = _unsortedArray.ToList();
        
        // Act
        var result = SortLib.SortList(list, "merge");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_BubbleSort_ReturnsSortedList()
    {
        // Arrange
        var list = _unsortedArray.ToList();
        
        // Act
        var result = SortLib.SortList(list, "bubble");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_SelectionSort_ReturnsSortedList()
    {
        // Arrange
        var list = _unsortedArray.ToList();
        
        // Act
        var result = SortLib.SortList(list, "selection");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_InsertionSort_ReturnsSortedList()
    {
        // Arrange
        var list = _unsortedArray.ToList();
        
        // Act
        var result = SortLib.SortList(list, "insertion");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray.ToList(), result);
    }

    [Fact]
    public void SortList_HeapSort_ReturnsSortedList()
    {
        // Arrange
        var list = _unsortedArray.ToList();
        
        // Act
        var result = SortLib.SortList(list, "heap");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sortedArray.ToList(), result);
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
        var list = _unsortedArray.ToList();
        
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
