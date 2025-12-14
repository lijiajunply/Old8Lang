using System.Text.Json;
using System.Text;

namespace Old8LangLib;

/// <summary>
/// JSON处理模块，用于JSON序列化和反序列化
/// </summary>
public static class JsonLib
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// 将对象序列化为JSON字符串
    /// </summary>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="options">序列化选项</param>
    /// <returns>JSON字符串</returns>
    public static string Serialize(object obj, JsonSerializerOptions? options = null)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "序列化对象不能为空");
        }

        try
        {
            return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new JsonException($"JSON序列化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将JSON字符串反序列化为指定类型的对象
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="options">反序列化选项</param>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentNullException(nameof(json), "JSON字符串不能为空");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new JsonException($"JSON反序列化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将JSON字符串反序列化为动态对象
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="options">反序列化选项</param>
    /// <returns>动态对象</returns>
    public static dynamic? DeserializeDynamic(string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentNullException(nameof(json), "JSON字符串不能为空");
        }

        try
        {
            return JsonSerializer.Deserialize<dynamic>(json, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new JsonException($"JSON反序列化为动态对象失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从JSON文件中读取并反序列化为指定类型的对象
    /// </summary>
    /// <param name="filePath">JSON文件路径</param>
    /// <param name="options">反序列化选项</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public static T? DeserializeFromFile<T>(string filePath, JsonSerializerOptions? options = null,
        Encoding? encoding = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"JSON文件不存在: '{filePath}'", filePath);
        }

        try
        {
            string json = File.ReadAllText(filePath, encoding ?? Encoding.UTF8);
            return Deserialize<T>(json, options);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JsonException($"从文件读取JSON并反序列化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将对象序列化并写入JSON文件
    /// </summary>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="filePath">JSON文件路径</param>
    /// <param name="options">序列化选项</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void SerializeToFile(object obj, string filePath, JsonSerializerOptions? options = null,
        Encoding? encoding = null)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "序列化对象不能为空");
        }

        try
        {
            string json = Serialize(obj, options);
            File.WriteAllText(filePath, json, encoding ?? Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new JsonException($"将对象序列化并写入文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 验证JSON字符串是否有效
    /// </summary>
    /// <param name="json">要验证的JSON字符串</param>
    /// <returns>如果JSON有效则返回true，否则返回false</returns>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 将对象序列化到流
    /// </summary>
    /// <param name="stream">目标流</param>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="options">序列化选项</param>
    /// <param name="encoding">编码，默认为UTF-8</param>
    public static void SerializeToStream(Stream stream, object obj, JsonSerializerOptions? options = null,
        Encoding? encoding = null)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream), "流不能为空");
        }

        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "序列化对象不能为空");
        }

        try
        {
            JsonSerializer.Serialize(stream, obj, obj.GetType(), options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new JsonException($"将对象序列化到流失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从流反序列化对象
    /// </summary>
    /// <param name="stream">源流</param>
    /// <param name="options">反序列化选项</param>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public static T? DeserializeFromStream<T>(Stream stream, JsonSerializerOptions? options = null)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream), "流不能为空");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(stream, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new JsonException($"从流反序列化对象失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 压缩JSON字符串
    /// </summary>
    /// <param name="json">要压缩的JSON字符串</param>
    /// <returns>压缩后的JSON字符串</returns>
    public static string MinifyJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentNullException(nameof(json), "JSON字符串不能为空");
        }

        try
        {
            var doc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions { WriteIndented = false };
            return JsonSerializer.Serialize(doc, options);
        }
        catch (Exception ex)
        {
            throw new JsonException($"压缩JSON字符串失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 美化JSON字符串
    /// </summary>
    /// <param name="json">要美化的JSON字符串</param>
    /// <returns>美化后的JSON字符串</returns>
    public static string PrettifyJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentNullException(nameof(json), "JSON字符串不能为空");
        }

        try
        {
            var doc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(doc, options);
        }
        catch (Exception ex)
        {
            throw new JsonException($"美化JSON字符串失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 合并多个JSON对象
    /// </summary>
    /// <param name="jsonObjects">要合并的JSON对象字符串数组</param>
    /// <returns>合并后的JSON字符串</returns>
    public static string MergeJson(params string[] jsonObjects)
    {
        if (jsonObjects == null || jsonObjects.Length == 0)
        {
            throw new ArgumentNullException(nameof(jsonObjects), "JSON对象数组不能为空");
        }

        try
        {
            var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var json in jsonObjects)
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("只能合并JSON对象");
                }

                foreach (var property in root.EnumerateObject())
                {
                    merged[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText())!;
                }
            }

            return Serialize(merged);
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JsonException($"合并JSON对象失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 比较两个JSON字符串是否相等
    /// </summary>
    /// <param name="json1">第一个JSON字符串</param>
    /// <param name="json2">第二个JSON字符串</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
    public static bool CompareJson(string json1, string json2)
    {
        if (string.IsNullOrEmpty(json1) || string.IsNullOrEmpty(json2))
        {
            return json1 == json2;
        }

        try
        {
            var doc1 = JsonDocument.Parse(json1);
            var doc2 = JsonDocument.Parse(json2);
            return CompareJsonElements(doc1.RootElement, doc2.RootElement);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 比较两个JSON元素是否相等
    /// </summary>
    /// <param name="element1">第一个JSON元素</param>
    /// <param name="element2">第二个JSON元素</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
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

    /// <summary>
    /// 根据路径获取JSON值
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="path">JSON路径，例如: "user.name" 或 "items[0].id"
    /// <returns>JSON值</returns>
    public static string GetJsonValue(string json, string path)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentNullException(nameof(json), "JSON字符串不能为空");
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path), "JSON路径不能为空");
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

            return current.GetRawText();
        }
        catch (Exception ex)
        {
            throw new JsonException($"根据路径获取JSON值失败: {ex.Message}", ex);
        }
    }
}