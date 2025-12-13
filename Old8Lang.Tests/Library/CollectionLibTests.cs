using Old8LangLib;
using Xunit;

namespace Old8Lang.Tests.Library;

/// <summary>
/// 集合处理模块测试
/// </summary>
public class CollectionLibTests
{
    [Fact]
    public void ListFilter_Predicate_ReturnsFilteredList()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };
        Func<int, bool> predicate = x => x % 2 == 0;
        
        // Act
        var result = CollectionLib.ListFilter(list, predicate);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(2, result);
        Assert.Contains(4, result);
        Assert.DoesNotContain(1, result);
        Assert.DoesNotContain(3, result);
        Assert.DoesNotContain(5, result);
    }

    [Fact]
    public void ListMap_Selector_ReturnsTransformedList()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        Func<int, string> selector = x => x.ToString();
        
        // Act
        var result = CollectionLib.ListMap(list, selector);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
    }

    [Fact]
    public void ListFold_InitialValueAndFunc_ReturnsAccumulatedValue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };
        Func<int, int, int> func = (acc, x) => acc + x;
        
        // Act
        var result = CollectionLib.ListFold(list, 0, func);
        
        // Assert
        Assert.Equal(15, result);
    }

    [Fact]
    public void ListFind_Predicate_ReturnsFirstMatch()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };
        Func<int, bool> predicate = x => x > 3;
        
        // Act
        var result = CollectionLib.ListFind(list, predicate);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result);
    }

    [Fact]
    public void ListSort_Array_ReturnsSortedList()
    {
        // Arrange
        var list = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };
        
        // Act
        var result = CollectionLib.ListSort(list);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.Count);
        Assert.Equal(1, result[0]);
        Assert.Equal(1, result[1]);
        Assert.Equal(2, result[2]);
        Assert.Equal(3, result[3]);
        Assert.Equal(4, result[4]);
        Assert.Equal(5, result[5]);
        Assert.Equal(6, result[6]);
        Assert.Equal(9, result[7]);
    }

    [Fact]
    public void IsEmpty_EmptyCollection_ReturnsTrue()
    {
        // Arrange
        var emptyList = new List<int>();
        
        // Act
        var result = CollectionLib.IsEmpty(emptyList);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEmpty_NonEmptyCollection_ReturnsFalse()
    {
        // Arrange
        var nonEmptyList = new List<int> { 1, 2, 3 };
        
        // Act
        var result = CollectionLib.IsEmpty(nonEmptyList);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Length_Collection_ReturnsCount()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };
        
        // Act
        var result = CollectionLib.Length(list);
        
        // Assert
        Assert.Equal(5, result);
    }
}