using Old8LangLib;

namespace Old8Lang.Tests.Library;

/// <summary>
/// JSON处理模块测试
/// </summary>
public class JsonLibTests
{
    [Fact]
    public void Serialize_Object_ReturnsValidJson()
    {
        // Arrange
        var testObject = new { Name = "Test", Age = 30, IsActive = true };
        
        // Act
        var json = JsonLib.Serialize(testObject);
        
        // Assert
        Assert.NotNull(json);
        Assert.Contains("Name", json);
        Assert.Contains("Test", json);
        Assert.Contains("Age", json);
        Assert.Contains("30", json);
        Assert.Contains("IsActive", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsObject()
    {
        // Arrange
        string json = "{\"Name\":\"Test\",\"Age\":30,\"IsActive\":true}";
        
        // Act
        var result = JsonLib.Deserialize<TestClass>(json);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(30, result.Age);
        Assert.True(result.IsActive);
    }

    [Fact]
    public void Deserialize_ValidJsonToDictionary_ReturnsDictionary()
    {
        // Arrange
        string json = "{\"Name\":\"Test\",\"Age\":30}";
        
        // Act
        var result = JsonLib.Deserialize<TestClass>(json);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void IsValidJson_ValidJson_ReturnsTrue()
    {
        // Arrange
        string json = "{\"Name\":\"Test\"}";
        
        // Act
        var result = JsonLib.IsValidJson(json);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidJson_InvalidJson_ReturnsFalse()
    {
        // Arrange
        string json = "{Name:Test}"; // Invalid JSON (missing quotes)
        
        // Act
        var result = JsonLib.IsValidJson(json);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Serialize_NullObject_ThrowsException()
    {
        // Arrange
        object nullObject = null;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => JsonLib.Serialize(nullObject));
    }

    [Fact]
    public void Deserialize_EmptyString_ThrowsException()
    {
        // Arrange
        string emptyJson = string.Empty;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => JsonLib.Deserialize<TestClass>(emptyJson));
    }

    [Fact]
    public void DeserializeDynamic_ValidJson_ReturnsDynamicObject()
    {
        // Arrange
        string json = "{\"Name\":\"Test\",\"Age\":30}";
        
        // Act
        var result = JsonLib.DeserializeDynamic(json);
        
        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void SerializeToFile_ValidObject_WritesToFile()
    {
        // Arrange
        var testObject = new TestClass { Name = "Test", Age = 30, IsActive = true };
        string tempFile = Path.GetTempFileName();
        
        try
        {
            // Act
            JsonLib.SerializeToFile(testObject, tempFile);
            
            // Assert
            Assert.True(File.Exists(tempFile));
            string fileContent = File.ReadAllText(tempFile);
            Assert.Contains("Test", fileContent);
            Assert.Contains("30", fileContent);
            Assert.Contains("true", fileContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void DeserializeFromFile_ValidFile_ReturnsObject()
    {
        // Arrange
        var testObject = new TestClass { Name = "Test", Age = 30, IsActive = true };
        string tempFile = Path.GetTempFileName();
        string json = JsonLib.Serialize(testObject);
        File.WriteAllText(tempFile, json);
        
        try
        {
            // Act
            var result = JsonLib.DeserializeFromFile<TestClass>(tempFile);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(30, result.Age);
            Assert.True(result.IsActive);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void DeserializeFromFile_NonExistentFile_ThrowsException()
    {
        // Arrange
        string nonExistentFile = "non_existent_file.json";
        
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => JsonLib.DeserializeFromFile<TestClass>(nonExistentFile));
    }

    // 测试类
    private class TestClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }
}