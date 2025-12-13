using System.Globalization;
using System.Text;

namespace Old8LangLib;

/// <summary>
/// CSV处理模块，用于CSV文件的读写和解析
/// </summary>
public static class CsvLib
{
    /// <summary>
    /// 从CSV文件中读取数据，返回二维字符串数组
    /// </summary>
    /// <param name="filePath">CSV文件路径</param>
    /// <param name="hasHeader">是否包含表头</param>
    /// <param name="delimiter">分隔符，默认为逗号</param>
    /// <param name="quoteChar">引号字符，默认为双引号</param>
    /// <returns>二维字符串数组</returns>
    public static string[][] ReadCsv(string filePath, bool hasHeader = true, char delimiter = ',', char quoteChar = '"')
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV文件不存在: '{filePath}'", filePath);
        }

        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
            {
                return Array.Empty<string[]>();
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
    /// <returns>字典列表</returns>
    public static List<Dictionary<string, string>> ReadCsvAsDictionary(string filePath, char delimiter = ',', char quoteChar = '"')
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV文件不存在: '{filePath}'", filePath);
        }

        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                return new List<Dictionary<string, string>>();
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
    public static void WriteCsv(string filePath, string[][] data, string[]? headers = null, char delimiter = ',', char quoteChar = '"')
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "数据不能为空");
        }

        try
        {
            using var writer = new StreamWriter(filePath);

            // 写入表头
            if (headers != null && headers.Length > 0)
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
    public static void WriteCsvFromDictionary(string filePath, List<Dictionary<string, string>> data, char delimiter = ',', char quoteChar = '"')
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "数据不能为空");
        }

        try
        {
            if (data.Count == 0)
            {
                File.WriteAllText(filePath, string.Empty);
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

            using var writer = new StreamWriter(filePath);
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
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;
        bool escapeNext = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

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
    public static string FormatCsvLine(string[] values, char delimiter = ',', char quoteChar = '"')
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

            string value = values[i] ?? string.Empty;
            bool needsQuotes = value.Contains(delimiter) || value.Contains(quoteChar) || value.Contains('\n') || value.Contains('\r');

            if (needsQuotes)
            {
                result.Append(quoteChar);
                result.Append(value.Replace(quoteChar.ToString(), quoteChar.ToString() + quoteChar.ToString()));
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
    public static string[][] ParseCsvContent(string csvContent, bool hasHeader = true, char delimiter = ',', char quoteChar = '"')
    {
        if (string.IsNullOrEmpty(csvContent))
        {
            return Array.Empty<string[]>();
        }

        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return Array.Empty<string[]>();
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
}

/// <summary>
/// CSV异常类
/// </summary>
public class CsvException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public CsvException() : base() { }

    /// <summary>
    /// 构造函数
    /// </summary>
    public CsvException(string message) : base(message) { }

    /// <summary>
    /// 构造函数
    /// </summary>
    public CsvException(string message, Exception innerException) : base(message, innerException) { }
}