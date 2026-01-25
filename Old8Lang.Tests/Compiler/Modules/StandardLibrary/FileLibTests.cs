using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.StandardLibrary;

/// <summary>
/// FileLib 库测试 - 测试文件和目录操作功能
/// </summary>
[Collection("Sequential")]
public class FileLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_File_ShouldWorkCorrectly()
    {
        var code = @"
import File

PrintLine(""File library imported"")
";
        CreateTempModuleFile("./StandardLibrary/file_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/file_test.old8");

        Assert.Null(exception);
        var fileLib = interpreter.Manager.GetValue(new LangId("File"));
        Assert.NotNull(fileLib);
        Assert.IsAssignableFrom<IModuleValueType>(fileLib);
    }

    [Fact]
    public void FileWrite_And_FileRead_ShouldWorkCorrectly()
    {
        var code = @"
import File

testPath <- ""./test_file_write_read.txt""
content <- ""Hello Old8Lang!""

File.FileWrite(testPath, content)
PrintLine($""File written: {testPath}"")

readContent <- File.FileRead(testPath)
PrintLine($""File read: {readContent}"")

// Clean up
File.DeleteFile(testPath)
";
        CreateTempModuleFile("./StandardLibrary/file_write_read_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_write_read_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void FileWriteLines_And_FileReadLines_ShouldWorkCorrectly()
    {
        var code = @"
import File

testPath <- ""./test_file_lines.txt""
lines <- [""Line 1"", ""Line 2"", ""Line 3""]

File.FileWriteLines(testPath, lines)
PrintLine($""Lines written to {testPath}"")

readLines <- File.FileReadLines(testPath)
PrintLine($""Lines read: {len(readLines)}"")

// Clean up
File.DeleteFile(testPath)
";
        CreateTempModuleFile("./StandardLibrary/file_writelines_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_writelines_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void FileAppend_ShouldAppendContent()
    {
        var code = @"
import File

testPath <- ""./test_file_append.txt""

File.FileWrite(testPath, ""First line\n"")
File.FileAppend(testPath, ""Second line\n"")
File.FileAppend(testPath, ""Third line"")

content <- File.FileRead(testPath)
PrintLine($""File content:\n{content}"")

// Clean up
File.DeleteFile(testPath)
";
        CreateTempModuleFile("./StandardLibrary/file_append_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_append_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void FileExists_ShouldCheckFileExistence()
    {
        var code = @"
import File

testPath <- ""./test_file_exists.txt""

// File doesn't exist yet
exists <- File.FileExists(testPath)
PrintLine($""File exists (before): {exists}"")

// Create file
File.FileWrite(testPath, ""test content"")

// File exists now
exists <- File.FileExists(testPath)
PrintLine($""File exists (after): {exists}"")

// Clean up
File.DeleteFile(testPath)
";
        CreateTempModuleFile("./StandardLibrary/file_exists_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_exists_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CopyFile_ShouldCopyFileContent()
    {
        var code = @"
import File

sourcePath <- ""./test_file_copy_source.txt""
destPath <- ""./test_file_copy_dest.txt""

File.FileWrite(sourcePath, ""Content to copy"")
File.CopyFile(sourcePath, destPath)

content <- File.FileRead(destPath)
PrintLine($""Copied content: {content}"")

// Clean up
File.DeleteFile(sourcePath)
File.DeleteFile(destPath)
";
        CreateTempModuleFile("./StandardLibrary/file_copy_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_copy_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void RenameFile_ShouldRenameFile()
    {
        var code = @"
import File

oldPath <- ""./test_file_old_name.txt""
newPath <- ""./test_file_new_name.txt""

File.FileWrite(oldPath, ""Content"")
File.RenameFile(oldPath, newPath)

exists <- File.FileExists(newPath)
PrintLine($""File renamed successfully: {exists}"")

// Clean up
File.DeleteFile(newPath)
";
        CreateTempModuleFile("./StandardLibrary/file_rename_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_rename_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetFileInfo_ShouldReturnFileInfo()
    {
        var code = @"
import File

testPath <- ""./test_file_info.txt""
File.FileWrite(testPath, ""Test content for file info"")

info <- File.GetFileInfo(testPath)
PrintLine($""File info:\n{info}"")

// Clean up
File.DeleteFile(testPath)
";
        CreateTempModuleFile("./StandardLibrary/file_getinfo_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_getinfo_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CreateDirectory_And_DirectoryExists_ShouldWorkCorrectly()
    {
        var code = @"
import File

dirPath <- ""./test_directory""

File.CreateDirectory(dirPath)
exists <- File.DirectoryExists(dirPath)
PrintLine($""Directory created: {exists}"")

// Clean up
File.DeleteDirectory(dirPath, false)
";
        CreateTempModuleFile("./StandardLibrary/file_createdir_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_createdir_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetDirectoryInfo_ShouldReturnDirectoryInfo()
    {
        var code = @"
import File

dirPath <- ""./test_directory_info""
File.CreateDirectory(dirPath)

info <- File.GetDirectoryInfo(dirPath)
PrintLine($""Directory info:\n{info}"")

// Clean up
File.DeleteDirectory(dirPath, false)
";
        CreateTempModuleFile("./StandardLibrary/file_getdirinfo_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_getdirinfo_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetFiles_ShouldReturnFileList()
    {
        var code = @"
import File

dirPath <- ""./test_getfiles_dir""
File.CreateDirectory(dirPath)

// Create some test files
File.FileWrite(dirPath + ""/file1.txt"", ""content1"")
File.FileWrite(dirPath + ""/file2.txt"", ""content2"")
File.FileWrite(dirPath + ""/file3.log"", ""content3"")

// Get all files
files <- File.GetFiles(dirPath)
PrintLine($""Total files: {len(files)}"")

// Get only .txt files
txtFiles <- File.GetFiles(dirPath, ""*.txt"")
PrintLine($""Text files: {len(txtFiles)}"")

// Clean up
File.DeleteDirectory(dirPath, true)
";
        CreateTempModuleFile("./StandardLibrary/file_getfiles_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_getfiles_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetDirectories_ShouldReturnSubdirectories()
    {
        var code = @"
import File

dirPath <- ""./test_getdirs_parent""
File.CreateDirectory(dirPath)
File.CreateDirectory(dirPath + ""/subdir1"")
File.CreateDirectory(dirPath + ""/subdir2"")

subdirs <- File.GetDirectories(dirPath)
PrintLine($""Subdirectories: {len(subdirs)}"")

// Clean up
File.DeleteDirectory(dirPath, true)
";
        CreateTempModuleFile("./StandardLibrary/file_getdirs_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_getdirs_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void MoveDirectory_ShouldMoveDirectory()
    {
        var code = @"
import File

oldPath <- ""./test_move_old""
newPath <- ""./test_move_new""

File.CreateDirectory(oldPath)
File.FileWrite(oldPath + ""/test.txt"", ""content"")

File.MoveDirectory(oldPath, newPath)

exists <- File.DirectoryExists(newPath)
PrintLine($""Directory moved: {exists}"")

// Clean up
File.DeleteDirectory(newPath, true)
";
        CreateTempModuleFile("./StandardLibrary/file_movedir_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/file_movedir_test.old8");

        Assert.Null(exception);
    }
}
