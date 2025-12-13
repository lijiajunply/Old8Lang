using Old8LangLib;

namespace Old8Lang.Tests.Library;

/// <summary>
/// CSV处理模块测试
/// </summary>
public class CsvTests
{
    [Fact]
    public void ParseCsvLine_SimpleLine_ReturnsArray()
    {
        // Arrange
        string csvLine = "Name,Age,City";
        
        // Act
        var result = Csv.ParseCsvLine(csvLine);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("Name", result[0]);
        Assert.Equal("Age", result[1]);
        Assert.Equal("City", result[2]);
    }

    [Fact]
    public void ParseCsvLine_LineWithQuotes_ReturnsArray()
    {
        // Arrange
        string csvLine = "John,\"Doe, Jr.\",30";
        
        // Act
        var result = Csv.ParseCsvLine(csvLine);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("John", result[0]);
        Assert.Equal("Doe, Jr.", result[1]);
        Assert.Equal("30", result[2]);
    }

    [Fact]
    public void FormatCsvLine_Array_ReturnsCsvLine()
    {
        // Arrange
        string[] values = { "Name", "Age", "City" };
        
        // Act
        var result = Csv.FormatCsvLine(values);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Name,Age,City", result);
    }

    [Fact]
    public void FormatCsvLine_ArrayWithSpecialChars_ReturnsQuotedLine()
    {
        // Arrange
        string[] values = { "John", "Doe, Jr.", "New York" };
        
        // Act
        var result = Csv.FormatCsvLine(values);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("John,\"Doe, Jr.\",New York", result);
    }

    [Fact]
    public void ParseCsvContent_ValidCsv_ReturnsArray()
    {
        // Arrange
        string csvContent = "Name,Age\nJohn,30\nJane,25";
        
        // Act
        var result = Csv.ParseCsvContent(csvContent, true);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(2, result[0].Length);
        Assert.Equal("John", result[0][0]);
        Assert.Equal("30", result[0][1]);
        Assert.Equal("Jane", result[1][0]);
        Assert.Equal("25", result[1][1]);
    }

    [Fact]
    public void ParseCsvContent_EmptyCsv_ReturnsEmptyArray()
    {
        // Arrange
        string csvContent = string.Empty;
        
        // Act
        var result = Csv.ParseCsvContent(csvContent);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCsvLine_EmptyLine_ReturnsEmptyArray()
    {
        // Arrange
        string csvLine = string.Empty;
        
        // Act
        var result = Csv.ParseCsvLine(csvLine);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
