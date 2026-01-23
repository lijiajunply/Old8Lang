using System.Collections;
using Old8Lang.AST.Expression;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 类型转换
/// </summary>
public partial class VirtualMachine
{
    private Dictionary<object, object?> ConvertToDict(object? value)
    {
        if (value == null) return new Dictionary<object, object?>();
        if (value is Dictionary<object, object?> dict) return dict;
        if (value is IDictionary d)
        {
            var newDict = new Dictionary<object, object?>();
            foreach (DictionaryEntry entry in d)
            {
                newDict[entry.Key] = entry.Value;
            }

            return newDict;
        }

        throw new InvalidCastException($"无法将类型 {value?.GetType().Name ?? "null"} 转换为 dict");
    }

    private bool ToBool(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is double d) return Math.Abs(d) > 1e-10;
        if (value is string s) return !string.IsNullOrEmpty(s);
        return true;
    }


    private double ToDouble(object? value)
    {
        if (value is int i) return i;
        if (value is double d) return d;
        if (value is string s && double.TryParse(s, out double result)) return result;
        throw new CastError(new SourcePosition(), value?.GetType().Name ?? "null", "double");
    }


    private string ToString(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return s;

        // 处理 LangValueType（使用 ToDisplayString 而不是 ToString）
        if (value is LangValueType langValue)
        {
            return langValue.ToDisplayString();
        }

        // 处理数组
        if (value is Array array)
        {
            var items = (from object? item in array select ToString(item)).ToList();

            return "[" + string.Join(", ", items) + "]";
        }

        // 处理列表
        if (value is IList list)
        {
            var items = (from object? item in list select ToString(item)).ToList();

            return "{" + string.Join(", ", items) + "}";
        }

        // 处理字典
        if (value is IDictionary dict)
        {
            var items = (from object? key in dict.Keys select $"{ToString(key)}: {ToString(dict[key])}").ToList();

            return "{" + string.Join(", ", items) + "}";
        }

        // 处理数字类型，确保不使用科学计数法
        if (value is int || value is long || value is short || value is byte)
        {
            return value.ToString() ?? "";
        }

        if (value is double d)
        {
            // 对于 double，如果是整数值，显示为整数（不使用科学计数法）
            if (Math.Abs(d - Math.Round(d)) < 0.0000001)
            {
                // 使用 "F0" 格式强制显示为固定格式（无小数点）
                return d.ToString("F0");
            }
            return d.ToString();
        }

        return value.ToString() ?? "";
    }

    /// <summary>
    /// 调用原生函数
    /// </summary>

}
