using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// JsonLib 库测试 - 测试 JSON 序列化和反序列化功能
/// </summary>
public class JsonLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Json_ShouldWorkCorrectly()
    {
        var code = @"
import Json

PrintLine(""Json library imported"")
";
        CreateTempModuleFile("./StandardLibrary/json_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/json_test.old8");

        Assert.Null(exception);
        var jsonLib = interpreter.Manager.GetValue(new LangId("Json"));
        Assert.NotNull(jsonLib);
        Assert.IsAssignableFrom<IModuleValueType>(jsonLib);
    }

    [Fact]
    public void Serialize_ShouldConvertDictionaryToJson()
    {
        var code = @"
import Json

data <- {""name"": ""Alice"", ""age"": 30}
json <- Json.Serialize(data)
PrintLine($""Data: {data}"")
PrintLine($""JSON: {json}"")
";
        CreateTempModuleFile("./StandardLibrary/json_serialize_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/json_serialize_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Deserialize_ShouldConvertJsonToObject()
    {
        var code = @"
import Json

json <- ""{\""name\"":\""Bob\"",\""age\"":25}""
data <- Json.Deserialize(json)
PrintLine($""JSON: {json}"")
PrintLine($""Data: {data}"")
";
        CreateTempModuleFile("./StandardLibrary/json_deserialize_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/json_deserialize_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Serialize_Then_Deserialize_ShouldBeReversible()
    {
        var code = @"
import Json

original <- {""product"": ""Laptop"", ""price"": 999.99, ""inStock"": true}
json <- Json.Serialize(original)
restored <- Json.Deserialize(json)
PrintLine($""Original: {original}"")
PrintLine($""JSON: {json}"")
PrintLine($""Restored: {restored}"")
";
        CreateTempModuleFile("./StandardLibrary/json_reversible_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/json_reversible_test.old8");

        Assert.Null(exception);
    }
}
