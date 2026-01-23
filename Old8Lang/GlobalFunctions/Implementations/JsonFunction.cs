using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Json 函数 - 将值序列化为 JSON 字符串
/// </summary>
public sealed class JsonFunction : BaseGlobalFunction
{
    public override string[] Names => ["Json", "json"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        // 支持多种类型的 JSON 序列化
        switch (results[0])
        {
            case AnyLangValue jsonAnyValue:
                return jsonAnyValue.ToJson();

            case DictionaryLangValue dictValue:
            {
                // 将字典转换为 JSON 字符串
                var dict = new Dictionary<string, object>();
                foreach (var (key, value) in dictValue.Value)
                {
                    var keyStr = key.ToDisplayString();
                    dict[keyStr] = value.GetValue();
                }

                var jsonStr = System.Text.Json.JsonSerializer.Serialize(dict);
                return new StringLangValue(jsonStr);
            }

            case ArrayLangValue arrayValue:
            {
                // 将数组转换为 JSON 字符串
                var list = arrayValue.GetItems().Select(item => item.GetValue()).ToList();
                var jsonStr = System.Text.Json.JsonSerializer.Serialize(list);
                return new StringLangValue(jsonStr);
            }

            case ListLangValue listValue:
            {
                // 将列表转换为 JSON 字符串
                var list = listValue.GetItems().Select(item => item.GetValue()).ToList();
                var jsonStr = System.Text.Json.JsonSerializer.Serialize(list);
                return new StringLangValue(jsonStr);
            }

            default:
                throw new TypeError(parameters[0], "AnyValue/DictionaryValue/ArrayValue/ListValue",
                    results[0].GetType().Name);
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持 JSON 序列化
        ilGenerator.Emit(OpCodes.Ldstr, "");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }
    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        // VM 模式下不支持 JSON 序列化,返回空字符串
        return "";
    }
}