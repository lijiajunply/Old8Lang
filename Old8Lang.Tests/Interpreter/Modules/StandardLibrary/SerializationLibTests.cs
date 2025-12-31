using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// SerializationLib 库测试 - 测试序列化功能（MessagePack 和 Protobuf）
/// </summary>
public class SerializationLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Serialization_ShouldWorkCorrectly()
    {
        var code = @"
import Serialization

PrintLine(""Serialization library imported"")
";
        CreateTempModuleFile("./StandardLibrary/serialization_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/serialization_test.old8");

        Assert.Null(exception);
        var serLib = interpreter.Manager.GetValue(new LangId("Serialization"));
        Assert.NotNull(serLib);
        Assert.IsAssignableFrom<IModuleValueType>(serLib);
    }

    [Fact]
    public void MsgPackSerialize_ShouldSerializeObject()
    {
        var code = @"
import Serialization

data <- {""name"": ""Alice"", ""age"": 30}
bytes <- Serialization.MsgPackSerialize(data)
PrintLine($""MessagePack serialized, bytes length: {len(bytes)}"")
";
        CreateTempModuleFile("./StandardLibrary/msgpack_serialize_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/msgpack_serialize_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void MsgPackSerializeToFile_ShouldSerializeToFile()
    {
        var code = @"
import Serialization
import File

testPath <- ""./test_msgpack_data.bin""

data <- {""name"": ""Bob"", ""age"": 25}
Serialization.MsgPackSerializeToFile(data, testPath)
PrintLine($""MessagePack serialized to file: {testPath}"")

// Verify file exists
exists <- File.FileExists(testPath)
PrintLine($""File exists: {exists}"")

// Clean up
File.DeleteFile(testPath)
";
        CreateTempModuleFile("./StandardLibrary/msgpack_serialize_file_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/msgpack_serialize_file_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void MsgPackToJson_ShouldConvertToJson()
    {
        var code = @"
import Serialization

data <- {""name"": ""Charlie"", ""age"": 35}
json <- Serialization.MsgPackToJson(data)
PrintLine($""MessagePack to JSON: {json}"")
";
        CreateTempModuleFile("./StandardLibrary/msgpack_tojson_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/msgpack_tojson_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ProtobufSerialize_ShouldSerializeObject()
    {
        var code = @"
import Serialization

data <- {""name"": ""David"", ""age"": 40}
bytes <- Serialization.ProtobufSerialize(data)
PrintLine($""Protobuf serialized, bytes length: {bytes.Length}"")
";
        CreateTempModuleFile("./StandardLibrary/protobuf_serialize_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/protobuf_serialize_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ProtobufSerializeToFile_ShouldSerializeToFile()
    {
        var code = @"
import Serialization
import File

testPath <- ""./test_protobuf_data.bin""

data <- {""name"": ""Eve"", ""age"": 28}
Serialization.ProtobufSerializeToFile(data, testPath)
PrintLine($""Protobuf serialized to file: {testPath}"")

// Verify file exists
exists <- File.FileExists(testPath)
PrintLine($""File exists: {exists}"")

// Clean up
File.DeleteFile(testPath)
";
        CreateTempModuleFile("./StandardLibrary/protobuf_serialize_file_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/protobuf_serialize_file_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ProtobufToJson_ShouldConvertToJson()
    {
        var code = @"
import Serialization

data <- {""name"": ""Frank"", ""age"": 45}
json <- Serialization.ProtobufToJson(data)
PrintLine($""Protobuf to JSON: {json}"")
";
        CreateTempModuleFile("./StandardLibrary/protobuf_tojson_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/protobuf_tojson_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void DeepClone_ShouldCloneObject()
    {
        var code = @"
import Serialization

original <- {""name"": ""Grace"", ""age"": 33, ""hobbies"": [""reading"", ""coding""]}
cloned <- Serialization.DeepClone(original)
PrintLine($""Original: {original}"")
PrintLine($""Cloned: {cloned}"")
";
        CreateTempModuleFile("./StandardLibrary/deepclone_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/deepclone_test.old8");

        Assert.Null(exception);
    }
}
