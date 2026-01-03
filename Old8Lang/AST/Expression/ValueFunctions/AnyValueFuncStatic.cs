using System.Text;
using System.Text.Json;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// AnyLangValue类型的扩展方法类，提供JSON序列化和反序列化功能
/// </summary>
public static class AnyValueFuncStatic
{
    /// <summary>
    /// 将JsonElement转换为Old8Lang值类型
    /// </summary>
    /// <param name="element">要转换的JsonElement</param>
    /// <param name="node">用于错误报告的节点</param>
    /// <returns>转换后的Old8Lang值类型</returns>
    private static LangValueType GetJsonElement(JsonElement element, IOldLangTree node)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => new StringLangValue(element.GetString() ?? ""),
            JsonValueKind.Number => new IntLangValue(element.GetInt32()),
            JsonValueKind.True => new BoolLangValue(true),
            JsonValueKind.False => new BoolLangValue(),
            JsonValueKind.Null => new VoidLangValue(),
            JsonValueKind.Array => new ArrayLangValue(
                element.EnumerateArray().Select(x => GetJsonElement(x, node)).ToList()),
            JsonValueKind.Undefined => new VoidLangValue(),
            JsonValueKind.Object => new StringLangValue(element.ToString()).ToObj(),
            _ => throw new InvalidOperationError(node, "不支持的JSON值类型")
        };
    }

    /// <summary>
    /// 将AnyLangValue对象转换为JSON字符串
    /// </summary>
    /// <param name="type">要转换的AnyLangValue对象</param>
    /// <returns>包含JSON表示的StringLangValue</returns>
    public static StringLangValue ToJson(this AnyLangValue type)
    {
        var builder = new StringBuilder();
        builder.Append('{');
        var i = 0;
        foreach (var (key, value) in type.InstanceData)
        {
            if (value is FuncLangValue or Instance or NativeAnyLangValue or NativeStaticAny
                or VoidLangValue) continue;
            builder.Append($"{(i == 0 ? "" : ",")}\"{key}\":{value}");
            i++;
        }

        builder.Append('}');
        return new StringLangValue(builder.ToString());
    }

    /// <summary>
    /// 将JSON字符串转换为AnyLangValue对象
    /// </summary>
    /// <param name="json">包含JSON数据的StringLangValue</param>
    /// <returns>转换后的AnyLangValue对象</returns>
    public static AnyLangValue ToObj(this StringLangValue json)
    {
        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(json.Value) ??
                         new Dictionary<string, object>();

        // 创建一个临时的ClassMetadata用于JSON对象
        var metadata = new ClassMetadata(
            className: "__JsonObject__",
            parentClassName: null,
            interfaceNames: new List<string>(),
            mixinNames: new List<string>(),
            isInterface: false,
            isAbstract: false,
            isMixin: false
        );

        // 创建AnyLangValue实例
        var anyValue = new AnyLangValue(
            classId: new LangId("__JsonObject__"),
            metadata: metadata,
            position: default
        );

        // 填充InstanceData
        foreach (var (key, value) in jsonObject)
        {
            LangValueType langValue;
            if (value is JsonElement element)
            {
                langValue = GetJsonElement(element, json);
            }
            else
            {
                langValue = LangValueType.ObjToValue(value);
            }

            anyValue.InstanceData[key] = langValue;
        }

        return anyValue;
    }
}