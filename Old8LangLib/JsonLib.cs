using System.Text.Json;

namespace Old8LangLib;

/// <summary>
/// JSON处理模块，用于JSON序列化和反序列化
/// </summary>
public static class JsonLib
{
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// 将对象序列化为JSON字符串
    /// </summary>
    /// <param name="obj">要序列化的对象</param>
    /// <returns>JSON字符串</returns>
    public static string Serialize(object obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "序列化对象不能为空");
        }

        try
        {
            return JsonSerializer.Serialize(obj, DefaultOptions);
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
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentNullException(nameof(json), "JSON字符串不能为空");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
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
    /// <returns>动态对象</returns>
    public static dynamic? DeserializeDynamic(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentNullException(nameof(json), "JSON字符串不能为空");
        }

        try
        {
            return JsonSerializer.Deserialize<dynamic>(json, DefaultOptions);
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
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public static T? DeserializeFromFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"JSON文件不存在: '{filePath}'", filePath);
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return Deserialize<T>(json);
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
    public static void SerializeToFile(object obj, string filePath)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "序列化对象不能为空");
        }

        try
        {
            string json = Serialize(obj);
            File.WriteAllText(filePath, json);
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
}