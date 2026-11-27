using System.Text.Json;
using Old8Lang.Error;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

public class VMJsonSerializeTests
{
    [Fact]
    public void JsonSerialize_ClassInstance_WritesOnlyInstanceFields()
    {
        using var json = Serialize("""
            class Payload {
                a <- 1
                b <- 2

                func init(first, second) {
                    this.a <- first
                    this.b <- second
                }

                func Sum() {
                    return this.a + this.b
                }
            }

            result <- Json(Payload(2, 3))
            """);

        var fields = json.RootElement;
        Assert.Equal(2, fields.EnumerateObject().Count());
        Assert.Equal(2, fields.GetProperty("a").GetInt32());
        Assert.Equal(3, fields.GetProperty("b").GetInt32());
    }

    [Fact]
    public void JsonSerialize_NestedCollections_PreservesJsonValueTypes()
    {
        using var json = Serialize("""
            class Child {
                active <- true
            }

            data <- {"line": "a\"b\nc", 42: {null, Child()}, "array": [1, 2.5]}
            result <- JsonSerialize(data)
            """);

        var root = json.RootElement;
        Assert.Equal("a\"b\nc", root.GetProperty("line").GetString());
        var list = root.GetProperty("42");
        Assert.Equal(JsonValueKind.Null, list[0].ValueKind);
        Assert.True(list[1].GetProperty("active").GetBoolean());
        var array = root.GetProperty("array");
        Assert.Equal(1, array[0].GetInt32());
        Assert.Equal(2.5, array[1].GetDouble());
    }

    [Theory]
    [InlineData("JsonSerialize", "null", JsonValueKind.Null)]
    [InlineData("jsonSerialize", "12", JsonValueKind.Number)]
    [InlineData("json", "false", JsonValueKind.False)]
    public void JsonSerialize_Aliases_SerializeScalarValues(string function, string value, JsonValueKind expected)
    {
        using var json = Serialize($"result <- {function}({value})");

        Assert.Equal(expected, json.RootElement.ValueKind);
    }

    [Fact]
    public void JsonSerialize_CyclicDictionary_ReportsLanguageError()
    {
        var vm = new VM(CompileHelper.CompileToBytecode("""
            data <- {"self": null}
            data["self"] <- data
            result <- JsonSerialize(data)
            """));

        var error = Assert.Throws<InvalidOperationError>(() => vm.Execute());
        Assert.Contains("循环引用", error.OriginalMessage);
    }

    private static JsonDocument Serialize(string code)
    {
        var vm = new VM(CompileHelper.CompileToBytecode(code));
        vm.Execute();
        return JsonDocument.Parse(Assert.IsType<string>(vm.GetGlobalVariable("result")));
    }
}
