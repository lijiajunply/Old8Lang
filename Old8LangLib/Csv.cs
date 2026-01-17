using System.Text;
using System.Text.Json;

namespace Old8LangLib;

/// <summary>
/// CSV处理模块，用于CSV文件的读写和解析
/// </summary>
public static class Csv
{
    /// <summary>
    /// 从CSV文件中读取数据，返回二维字符串数组
    /// </summary>
    /// <param name="filePath">CSV文件路径</param>
    /// <param name="hasHeader">是否包含表头</param>
    /// <param name="delimiter">分隔符，默认为逗号</param>
    /// <param name="quoteChar">引号字符，默认为双引号</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <returns>二维字符串数组</returns>
    public static string[][] ReadCsv(string filePath, bool hasHeader = true, char delimiter = ',', char quoteChar = '"',
        Encoding? encoding = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV文件不存在: '{filePath}'", filePath);
        }

        try
        {
            var lines = File.ReadAllLines(filePath, encoding ?? Encoding.UTF8);
            if (lines.Length == 0)
            {
                return [];
            }

            int startIndex = hasHeader ? 1 : 0;
            var result = new List<string[]>();

            for (int i = startIndex; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    result.Add(ParseCsvLine(lines[i], delimiter, quoteChar));
                }
            }

            return result.ToArray();
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CsvException($"读取CSV文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从CSV文件中读取数据，返回字典列表
    /// </summary>
    /// <param name="filePath">CSV文件路径</param>
    /// <param name="delimiter">分隔符，默认为逗号</param>
    /// <param name="quoteChar">引号字符，默认为双引号</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    /// <returns>字典列表</returns>
    public static List<Dictionary<string, string>> ReadCsvAsDictionary(string filePath, char delimiter = ',',
        char quoteChar = '"', Encoding? encoding = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV文件不存在: '{filePath}'", filePath);
        }

        try
        {
            var lines = File.ReadAllLines(filePath, encoding ?? Encoding.UTF8);
            if (lines.Length < 2)
            {
                return [];
            }

            var headers = ParseCsvLine(lines[0], delimiter, quoteChar);
            var result = new List<Dictionary<string, string>>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    var values = ParseCsvLine(lines[i], delimiter, quoteChar);
                    var row = new Dictionary<string, string>();

                    for (int j = 0; j < headers.Length; j++)
                    {
                        row[headers[j]] = j < values.Length ? values[j] : string.Empty;
                    }

                    result.Add(row);
                }
            }

            return result;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CsvException($"读取CSV文件为字典列表失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将二维字符串数组写入CSV文件
    /// </summary>
    /// <param name="filePath">CSV文件路径</param>
    /// <param name="data">二维字符串数组</param>
    /// <param name="headers">表头数组，可选</param>
    /// <param name="delimiter">分隔符，默认为逗号</param>
    /// <param name="quoteChar">引号字符，默认为双引号</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void WriteCsv(string filePath, string[][] data, string[]? headers = null, char delimiter = ',',
        char quoteChar = '"', Encoding? encoding = null)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "数据不能为空");
        }

        try
        {
            using var writer = new StreamWriter(filePath, false, encoding ?? Encoding.UTF8);

            // 写入表头
            if (headers is { Length: > 0 })
            {
                writer.WriteLine(FormatCsvLine(headers, delimiter, quoteChar));
            }

            // 写入数据行
            foreach (var row in data)
            {
                writer.WriteLine(FormatCsvLine(row, delimiter, quoteChar));
            }
        }
        catch (Exception ex)
        {
            throw new CsvException($"写入CSV文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将字典列表写入CSV文件
    /// </summary>
    /// <param name="filePath">CSV文件路径</param>
    /// <param name="data">字典列表</param>
    /// <param name="delimiter">分隔符，默认为逗号</param>
    /// <param name="quoteChar">引号字符，默认为双引号</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void WriteCsvFromDictionary(string filePath, List<Dictionary<string, string>> data,
        char delimiter = ',', char quoteChar = '"', Encoding? encoding = null)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "数据不能为空");
        }

        try
        {
            if (data.Count == 0)
            {
                File.WriteAllText(filePath, string.Empty, encoding ?? Encoding.UTF8);
                return;
            }

            // 获取所有唯一的表头
            var headers = new HashSet<string>();
            foreach (var row in data)
            {
                foreach (var key in row.Keys)
                {
                    headers.Add(key);
                }
            }

            var headerArray = headers.ToArray();

            using var writer = new StreamWriter(filePath, false, encoding ?? Encoding.UTF8);
            writer.WriteLine(FormatCsvLine(headerArray, delimiter, quoteChar));

            foreach (var row in data)
            {
                var values = new string[headerArray.Length];
                for (int i = 0; i < headerArray.Length; i++)
                {
                    values[i] = row.TryGetValue(headerArray[i], out var value) ? value : string.Empty;
                }

                writer.WriteLine(FormatCsvLine(values, delimiter, quoteChar));
            }
        }
        catch (Exception ex)
        {
            throw new CsvException($"从字典列表写入CSV文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 解析CSV行字符串为字符串数组
    /// </summary>
    /// <param name="line">CSV行字符串</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="quoteChar">引号字符</param>
    /// <returns>解析后的字符串数组</returns>
    public static string[] ParseCsvLine(string line, char delimiter = ',', char quoteChar = '"')
    {
        if (string.IsNullOrEmpty(line))
        {
            return [];
        }

        var result = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;
        bool escapeNext = false;

        foreach (var c in line)
        {
            if (escapeNext)
            {
                currentValue.Append(c);
                escapeNext = false;
            }
            else if (c == '\\')
            {
                escapeNext = true;
            }
            else if (c == quoteChar)
            {
                inQuotes = !inQuotes;
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        result.Add(currentValue.ToString());
        return result.ToArray();
    }

    /// <summary>
    /// 将字符串数组格式化为CSV行字符串
    /// </summary>
    /// <param name="values">字符串数组</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="quoteChar">引号字符</param>
    /// <returns>格式化后的CSV行字符串</returns>
    public static string FormatCsvLine(string[]? values, char delimiter = ',', char quoteChar = '"')
    {
        if (values == null)
        {
            return string.Empty;
        }

        var result = new StringBuilder();

        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                result.Append(delimiter);
            }

            string value = values[i];
            bool needsQuotes = value.Contains(delimiter) || value.Contains(quoteChar) || value.Contains('\n') ||
                               value.Contains('\r');

            if (needsQuotes)
            {
                result.Append(quoteChar);
                result.Append(value.Replace(quoteChar.ToString(), quoteChar + quoteChar.ToString()));
                result.Append(quoteChar);
            }
            else
            {
                result.Append(value);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 从CSV字符串解析数据
    /// </summary>
    /// <param name="csvContent">CSV字符串</param>
    /// <param name="hasHeader">是否包含表头</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="quoteChar">引号字符</param>
    /// <returns>二维字符串数组</returns>
    public static string[][] ParseCsvContent(string csvContent, bool hasHeader = true, char delimiter = ',',
        char quoteChar = '"')
    {
        if (string.IsNullOrEmpty(csvContent))
        {
            return [];
        }

        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return [];
        }

        int startIndex = hasHeader ? 1 : 0;
        var result = new List<string[]>();

        for (int i = startIndex; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                result.Add(ParseCsvLine(lines[i], delimiter, quoteChar));
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 将CSV转换为JSON
    /// </summary>
    /// <param name="csvContent">CSV字符串</param>
    /// <param name="hasHeader">是否包含表头</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="quoteChar">引号字符</param>
    /// <returns>JSON字符串</returns>
    public static string ConvertCsvToJson(string csvContent, bool hasHeader = true, char delimiter = ',',
        char quoteChar = '"')
    {
        if (string.IsNullOrEmpty(csvContent))
        {
            throw new ArgumentNullException(nameof(csvContent), "CSV内容不能为空");
        }

        try
        {
            var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return "[]";
            }

            var headers = hasHeader ? ParseCsvLine(lines[0], delimiter, quoteChar) : null;
            var startIndex = hasHeader ? 1 : 0;
            var result = new List<Dictionary<string, string>>();

            for (int i = startIndex; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    var values = ParseCsvLine(lines[i], delimiter, quoteChar);
                    var row = new Dictionary<string, string>();

                    for (int j = 0; j < values.Length; j++)
                    {
                        var key = headers != null && j < headers.Length ? headers[j] : j.ToString();
                        row[key] = values[j];
                    }

                    result.Add(row);
                }
            }

            return JsonLib.Serialize(result);
        }
        catch (Exception ex)
        {
            throw new CsvException($"CSV转换为JSON失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将JSON转换为CSV
    /// </summary>
    /// <param name="jsonContent">JSON字符串</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="quoteChar">引号字符</param>
    /// <returns>CSV字符串</returns>
    public static string ConvertJsonToCsv(string jsonContent, char delimiter = ',', char quoteChar = '"')
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            throw new ArgumentNullException(nameof(jsonContent), "JSON内容不能为空");
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                throw new CsvException("JSON必须是数组格式");
            }

            var rows = root.EnumerateArray().ToList();
            if (rows.Count == 0)
            {
                return string.Empty;
            }

            // 获取所有唯一的键作为表头
            var headers = new HashSet<string>();
            foreach (var row in rows)
            {
                if (row.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in row.EnumerateObject())
                    {
                        headers.Add(property.Name);
                    }
                }
            }

            var headerArray = headers.ToArray();
            var csvLines = new List<string> { FormatCsvLine(headerArray, delimiter, quoteChar) };

            // 生成数据行
            foreach (var row in rows)
            {
                if (row.ValueKind == JsonValueKind.Object)
                {
                    var values = new string[headerArray.Length];
                    for (int i = 0; i < headerArray.Length; i++)
                    {
                        if (row.TryGetProperty(headerArray[i], out var propertyValue))
                        {
                            values[i] = propertyValue.GetString() ?? propertyValue.ToString();
                        }
                        else
                        {
                            values[i] = string.Empty;
                        }
                    }

                    csvLines.Add(FormatCsvLine(values, delimiter, quoteChar));
                }
            }

            return string.Join(Environment.NewLine, csvLines);
        }
        catch (JsonException ex)
        {
            throw new CsvException($"JSON格式错误: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new CsvException($"JSON转换为CSV失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将CSV文件转换为JSON文件
    /// </summary>
    /// <param name="csvFilePath">CSV文件路径</param>
    /// <param name="jsonFilePath">JSON文件路径</param>
    /// <param name="hasHeader">是否包含表头</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="quoteChar">引号字符</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void ConvertCsvToJsonFile(string csvFilePath, string jsonFilePath, bool hasHeader = true,
        char delimiter = ',', char quoteChar = '"', Encoding? encoding = null)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"CSV文件不存在: '{csvFilePath}'", csvFilePath);
        }

        try
        {
            var csvContent = File.ReadAllText(csvFilePath, encoding ?? Encoding.UTF8);
            var jsonContent = ConvertCsvToJson(csvContent, hasHeader, delimiter, quoteChar);
            File.WriteAllText(jsonFilePath, jsonContent, encoding ?? Encoding.UTF8);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CsvException($"CSV文件转换为JSON文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将JSON文件转换为CSV文件
    /// </summary>
    /// <param name="jsonFilePath">JSON文件路径</param>
    /// <param name="csvFilePath">CSV文件路径</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="quoteChar">引号字符</param>
    /// <param name="encoding">文件编码，默认为UTF-8</param>
    public static void ConvertJsonToCsvFile(string jsonFilePath, string csvFilePath, char delimiter = ',',
        char quoteChar = '"', Encoding? encoding = null)
    {
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"JSON文件不存在: '{jsonFilePath}'", jsonFilePath);
        }

        try
        {
            var jsonContent = File.ReadAllText(jsonFilePath, encoding ?? Encoding.UTF8);
            var csvContent = ConvertJsonToCsv(jsonContent, delimiter, quoteChar);
            File.WriteAllText(csvFilePath, csvContent, encoding ?? Encoding.UTF8);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CsvException($"JSON文件转换为CSV文件失败: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// CSV异常类
/// </summary>
public class CsvException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public CsvException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public CsvException(string message) : base(message)
    {
    }
}