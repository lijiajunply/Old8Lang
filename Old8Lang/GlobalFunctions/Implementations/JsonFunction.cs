using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Json 函数 - 将值序列化为 JSON 字符串
/// </summary>
public sealed class JsonSerializeFunction : BaseGlobalFunction
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public override string[] Names => ["JsonSerialize", "jsonSerialize", "json"];
    public override string[] ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        try
        {
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

                    var jsonStr = JsonSerializer.Serialize(dict, DefaultOptions);
                    return new StringLangValue(jsonStr);
                }

                case ArrayLangValue arrayValue:
                {
                    // 将数组转换为 JSON 字符串
                    var list = arrayValue.GetItems().Select(item => item.GetValue()).ToList();
                    var jsonStr = JsonSerializer.Serialize(list, DefaultOptions);
                    return new StringLangValue(jsonStr);
                }

                case ListLangValue listValue:
                {
                    // 将列表转换为 JSON 字符串
                    var list = listValue.GetItems().Select(item => item.GetValue()).ToList();
                    var jsonStr = JsonSerializer.Serialize(list, DefaultOptions);
                    return new StringLangValue(jsonStr);
                }

                default:
                {
                    var obj = results[0].GetValue();
                    if (obj == null)
                    {
                        throw new InvalidOperationError(position, "序列化对象不能为空");
                    }
                    var jsonStr = JsonSerializer.Serialize(obj, DefaultOptions);
                    return new StringLangValue(jsonStr);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"JSON序列化失败: {ex.Message}");
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

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        // VM 模式下不支持 JSON 序列化,返回空字符串
        return "";
    }
}

/// <summary>
/// JsonDeserialize 函数 - 将 JSON 字符串反序列化为动态对象
/// </summary>
public sealed class JsonDeserializeFunction : BaseGlobalFunction
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public override string[] Names => ["JsonDeserialize", "jsonDeserialize"];
    public override string[] ParameterNames => ["json"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results[0] is not StringLangValue jsonStr)
        {
            throw new TypeError(parameters[0], "string", results[0].GetType().Name);
        }

        var json = jsonStr.Value;
        if (string.IsNullOrEmpty(json))
        {
            throw new InvalidOperationError(position, "JSON字符串不能为空");
        }

        try
        {
            return jsonStr.ToObj();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"JSON反序列化失败: {ex.Message}");
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return null;
    }
}

/// <summary>
/// JsonDeserializeFromFile 函数 - 从 JSON 文件中读取并反序列化
/// </summary>
public sealed class JsonDeserializeFromFileFunction : BaseGlobalFunction
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public override string[] Names => ["JsonDeserializeFromFile", "jsonDeserializeFromFile"];
    public override string[] ParameterNames => ["filePath"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results[0] is not StringLangValue filePathValue)
        {
            throw new TypeError(parameters[0], "string", results[0].GetType().Name);
        }

        var filePath = filePathValue.Value;
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationError(position, $"JSON文件不存在: '{filePath}'");
        }

        try
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            var jsonStr = new StringLangValue(json);
            return jsonStr.ToObj();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"从文件读取JSON并反序列化失败: {ex.Message}");
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return null;
    }
}

/// <summary>
/// JsonSerializeToFile 函数 - 将对象序列化并写入 JSON 文件
/// </summary>
public sealed class JsonSerializeToFileFunction : BaseGlobalFunction
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public override string[] Names => ["JsonSerializeToFile", "jsonSerializeToFile"];
    public override string[] ParameterNames => ["obj", "filePath"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        var obj = results[0].GetValue();
        if (obj == null)
        {
            throw new InvalidOperationError(position, "序列化对象不能为空");
        }

        if (results[1] is not StringLangValue filePathValue)
        {
            throw new TypeError(parameters[1], "string", results[1].GetType().Name);
        }

        var filePath = filePathValue.Value;

        try
        {
            string json = JsonSerializer.Serialize(obj, DefaultOptions);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            return new VoidLangValue();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"将对象序列化并写入文件失败: {ex.Message}");
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return null;
    }
}

/// <summary>
/// JsonIsValid 函数 - 验证 JSON 字符串是否有效
/// </summary>
public sealed class JsonIsValidFunction : BaseGlobalFunction
{
    public override string[] Names => ["JsonIsValid", "jsonIsValid"];
    public override string[] ParameterNames => ["json"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results[0] is not StringLangValue jsonStr)
        {
            return new BoolLangValue();
        }

        var json = jsonStr.Value;
        if (string.IsNullOrEmpty(json))
        {
            return new BoolLangValue();
        }

        try
        {
            JsonDocument.Parse(json);
            return new BoolLangValue(true);
        }
        catch
        {
            return new BoolLangValue();
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return false;
    }
}

/// <summary>
/// JsonMinify 函数 - 压缩 JSON 字符串
/// </summary>
public sealed class JsonMinifyFunction : BaseGlobalFunction
{
    public override string[] Names => ["JsonMinify", "jsonMinify"];
    public override string[] ParameterNames => ["json"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results[0] is not StringLangValue jsonStr)
        {
            throw new TypeError(parameters[0], "string", results[0].GetType().Name);
        }

        var json = jsonStr.Value;
        if (string.IsNullOrEmpty(json))
        {
            throw new InvalidOperationError(position, "JSON字符串不能为空");
        }

        try
        {
            var doc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions { WriteIndented = false };
            var minified = JsonSerializer.Serialize(doc, options);
            return new StringLangValue(minified);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"压缩JSON字符串失败: {ex.Message}");
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldstr, "");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return "";
    }
}

/// <summary>
/// JsonPrettify 函数 - 美化 JSON 字符串
/// </summary>
public sealed class JsonPrettifyFunction : BaseGlobalFunction
{
    public override string[] Names => ["JsonPrettify", "jsonPrettify"];
    public override string[] ParameterNames => ["json"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results[0] is not StringLangValue jsonStr)
        {
            throw new TypeError(parameters[0], "string", results[0].GetType().Name);
        }

        var json = jsonStr.Value;
        if (string.IsNullOrEmpty(json))
        {
            throw new InvalidOperationError(position, "JSON字符串不能为空");
        }

        try
        {
            var doc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var prettified = JsonSerializer.Serialize(doc, options);
            return new StringLangValue(prettified);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"美化JSON字符串失败: {ex.Message}");
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldstr, "");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return "";
    }
}

/// <summary>
/// JsonMerge 函数 - 合并多个 JSON 对象
/// </summary>
public sealed class JsonMergeFunction : BaseGlobalFunction
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public override string[] Names => ["JsonMerge", "jsonMerge"];
    public override string[] ParameterNames => ["jsonObjects"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => -1; // 可变参数

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results.Count == 0)
        {
            throw new InvalidOperationError(position, "JSON对象数组不能为空");
        }

        try
        {
            var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in results)
            {
                if (result is not StringLangValue jsonStr)
                {
                    throw new InvalidOperationError(position, "所有参数必须是字符串类型");
                }

                var json = jsonStr.Value;
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationError(position, "只能合并JSON对象");
                }

                foreach (var property in root.EnumerateObject())
                {
                    merged[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText())!;
                }
            }

            var mergedJson = JsonSerializer.Serialize(merged, DefaultOptions);
            return new StringLangValue(mergedJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationError(position, $"合并JSON对象失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"合并JSON对象失败: {ex.Message}");
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldstr, "");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return "";
    }
}

/// <summary>
/// JsonCompare 函数 - 比较两个 JSON 字符串是否相等
/// </summary>
public sealed class JsonCompareFunction : BaseGlobalFunction
{
    public override string[] Names => ["JsonCompare", "jsonCompare"];
    public override string[] ParameterNames => ["json1", "json2"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results[0] is not StringLangValue json1Str || results[1] is not StringLangValue json2Str)
        {
            return new BoolLangValue();
        }

        var json1 = json1Str.Value;
        var json2 = json2Str.Value;

        if (string.IsNullOrEmpty(json1) || string.IsNullOrEmpty(json2))
        {
            return new BoolLangValue(json1 == json2);
        }

        try
        {
            var doc1 = JsonDocument.Parse(json1);
            var doc2 = JsonDocument.Parse(json2);
            var isEqual = CompareJsonElements(doc1.RootElement, doc2.RootElement);
            return new BoolLangValue(isEqual);
        }
        catch
        {
            return new BoolLangValue();
        }
    }

    private static bool CompareJsonElements(JsonElement element1, JsonElement element2)
    {
        if (element1.ValueKind != element2.ValueKind)
        {
            return false;
        }

        switch (element1.ValueKind)
        {
            case JsonValueKind.Object:
                var properties1 = element1.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);
                var properties2 = element2.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);

                if (properties1.Count != properties2.Count)
                {
                    return false;
                }

                foreach (var (name, value1) in properties1)
                {
                    if (!properties2.TryGetValue(name, out var value2) || !CompareJsonElements(value1, value2))
                    {
                        return false;
                    }
                }

                return true;

            case JsonValueKind.Array:
                var array1 = element1.EnumerateArray().ToList();
                var array2 = element2.EnumerateArray().ToList();

                if (array1.Count != array2.Count)
                {
                    return false;
                }

                for (int i = 0; i < array1.Count; i++)
                {
                    if (!CompareJsonElements(array1[i], array2[i]))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return element1.GetRawText() == element2.GetRawText();
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return false;
    }
}

/// <summary>
/// JsonGetValue 函数 - 根据路径获取 JSON 值
/// </summary>
public sealed class JsonGetValueFunction : BaseGlobalFunction
{
    public override string[] Names => ["JsonGetValue", "jsonGetValue"];
    public override string[] ParameterNames => ["json", "path"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);

        if (results[0] is not StringLangValue jsonStr)
        {
            throw new TypeError(parameters[0], "string", results[0].GetType().Name);
        }

        if (results[1] is not StringLangValue pathStr)
        {
            throw new TypeError(parameters[1], "string", results[1].GetType().Name);
        }

        var json = jsonStr.Value;
        var path = pathStr.Value;

        if (string.IsNullOrEmpty(json))
        {
            throw new InvalidOperationError(position, "JSON字符串不能为空");
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationError(position, "JSON路径不能为空");
        }

        try
        {
            var doc = JsonDocument.Parse(json);
            var current = doc.RootElement;
            var pathParts = path.Split('.');

            foreach (var part in pathParts)
            {
                if (part.Contains('[') && part.Contains(']'))
                {
                    // 处理数组访问，例如: items[0]
                    var arrayPart = part.Split('[', ']');
                    var arrayName = arrayPart[0];
                    var index = int.Parse(arrayPart[1]);

                    current = current.GetProperty(arrayName);
                    current = current[index];
                }
                else
                {
                    // 处理对象属性访问，例如: user
                    current = current.GetProperty(part);
                }
            }

            return new StringLangValue(current.GetRawText());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(position, $"根据路径获取JSON值失败: {ex.Message}");
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldstr, "");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return "";
    }
}