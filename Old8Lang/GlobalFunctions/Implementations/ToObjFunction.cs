using System.Reflection.Emit;
using System.Text.Json;
using Old8Lang.AST;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// ToObj 函数 - 将 JSON 字符串反序列化为对象
/// </summary>
public sealed class ToObjFunction : BaseGlobalFunction
{
    public override string[] Names => ["ToObj", "toObj"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        
        if (results[0] is not StringLangValue stringValue)
            throw new TypeError(parameters[0], "StringValue", results[0].GetType().Name);
            
        return stringValue.ToObj();
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持 JSON 反序列化
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(AnyLangValue);
    }
    
    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        if (arguments.Length < 1 || arguments[0] is not string jsonStr)
            return null;

        try
        {
            var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
            if (jsonObject == null) return null;

            var result = new Dictionary<string, object?>();
            foreach (var kvp in jsonObject)
            {
                if (kvp.Value is JsonElement element)
                {
                    result[kvp.Key] = ConvertJsonElement(element);
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => ConvertJsonObject(element),
            _ => null
        };
    }

    private Dictionary<string, object?> ConvertJsonObject(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElement(property.Value);
        }
        return dict;
    }
}