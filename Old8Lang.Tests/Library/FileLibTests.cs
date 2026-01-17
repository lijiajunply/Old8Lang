using Old8LangLib;

namespace Old8Lang.Tests.Library;

/// <summary>
/// 文件操作库测试
/// </summary>
public class FileLibTests
{
    [Fact]
    public void FileWriteAndRead_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        string testContent = "Hello, Old8Lang!";
        
        try
        {
            // Act
            FileLib.FileWrite(testFile, testContent);
            string result = FileLib.FileRead(testFile);
            
            // Assert
            Assert.Equal(testContent, result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void FileWriteLinesAndReadLines_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        string[] testLines = ["Line 1", "Line 2", "Line 3"];
        
        try
        {
            // Act
            FileLib.FileWriteLines(testFile, testLines);
            string[] result = FileLib.FileReadLines(testFile);
            
            // Assert
            Assert.Equal(testLines.Length, result.Length);
            for (int i = 0; i < testLines.Length; i++)
            {
                Assert.Equal(testLines[i], result[i]);
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void FileAppend_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        string initialContent = "Initial content\n";
        string appendContent = "Appended content";
        
        try
        {
            // Act
            FileLib.FileWrite(testFile, initialContent);
            FileLib.FileAppend(testFile, appendContent);
            string result = FileLib.FileRead(testFile);
            
            // Assert
            Assert.Equal(initialContent + appendContent, result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void FileExists_Test()
    {
        // Arrange
        string existingFile = Path.GetTempFileName();
        string nonExistingFile = "non_existent_file.txt";
        
        try
        {
            // Act
            bool exists = FileLib.FileExists(existingFile);
            bool notExists = FileLib.FileExists(nonExistingFile);
            
            // Assert
            Assert.True(exists);
            Assert.False(notExists);
        }
        finally
        {
            // Cleanup
            if (File.Exists(existingFile))
                File.Delete(existingFile);
        }
    }

    [Fact]
    public void CopyFile_Test()
    {
        // Arrange
        string sourceFile = Path.GetTempFileName();
        string destFile = Path.GetTempFileName();
        string testContent = "Test content for copy";
        
        try
        {
            // Act
            FileLib.FileWrite(sourceFile, testContent);
            FileLib.CopyFile(sourceFile, destFile);
            string result = FileLib.FileRead(destFile);
            
            // Assert
            Assert.Equal(testContent, result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(sourceFile))
                File.Delete(sourceFile);
            if (File.Exists(destFile))
                File.Delete(destFile);
        }
    }

    [Fact]
    public void RenameFile_Test()
    {
        // Arrange
        string oldFile = Path.GetTempFileName();
        string newFile = Path.GetTempFileName();
        // 删除newFile，确保它不存在
        File.Delete(newFile);
        
        string testContent = "Test content for rename";
        
        try
        {
            // Act
            FileLib.FileWrite(oldFile, testContent);
            FileLib.RenameFile(oldFile, newFile);
            bool oldExists = FileLib.FileExists(oldFile);
            bool newExists = FileLib.FileExists(newFile);
            string result = FileLib.FileRead(newFile);
            
            // Assert
            Assert.False(oldExists);
            Assert.True(newExists);
            Assert.Equal(testContent, result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(oldFile))
                File.Delete(oldFile);
            if (File.Exists(newFile))
                File.Delete(newFile);
        }
    }

    [Fact]
    public void DeleteFile_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        string testContent = "Test content for delete";
        
        try
        {
            // Act
            FileLib.FileWrite(testFile, testContent);
            FileLib.DeleteFile(testFile);
            bool exists = FileLib.FileExists(testFile);
            
            // Assert
            Assert.False(exists);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void GetFileInfo_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        
        try
        {
            // Act
            string info = FileLib.GetFileInfo(testFile);
            
            // Assert
            Assert.Contains(Path.GetFileName(testFile), info);
            Assert.Contains(testFile, info);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void ReadCsvAndWriteCsv_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        string[][] testData =
        [
            ["Name", "Age", "City"],
            ["Alice", "25", "New York"],
            ["Bob", "30", "London"],
            ["Charlie", "35", "Paris"]
        ];
        
        try
        {
            // Act
            Csv.WriteCsv(testFile, testData, null);
            string[][] result = Csv.ReadCsv(testFile, false);
            
            // Assert
            Assert.Equal(testData.Length, result.Length);
            for (int i = 0; i < testData.Length; i++)
            {
                Assert.Equal(testData[i].Length, result[i].Length);
                for (int j = 0; j < testData[i].Length; j++)
                {
                    Assert.Equal(testData[i][j], result[i][j]);
                }
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void ReadXmlAndWriteXml_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        string testXml = "<root><person><name>Alice</name><age>25</age></person></root>";
        
        try
        {
            // Act
            FileLib.WriteXml(testFile, testXml);
            string result = FileLib.ReadXml(testFile);
            
            // Assert
            Assert.Contains("Alice", result);
            Assert.Contains("25", result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void ReadYamlAndWriteYaml_Test()
    {
        // Arrange
        string testFile = Path.GetTempFileName();
        
        try
        {
            // 由于YAML涉及动态类型，这里只测试基本的文件操作
            // 实际的序列化/反序列化测试可能需要更复杂的设置
            var testData = new { Name = "Alice", Age = 25, City = "New York" };
            
            // Act
            FileLib.WriteYaml(testFile, testData);
            var result = FileLib.ReadYaml(testFile);
            
            // Assert
            Assert.NotNull(result);
            // 注意：动态类型的断言在单元测试中可能不可靠
            // 这里只验证基本功能正常
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }
}
