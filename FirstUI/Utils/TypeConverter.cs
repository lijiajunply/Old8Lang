using System.Collections;

namespace FirstUI.Utils;

/// <summary>
/// 类型转换工具
/// 用于在 Old8Lang 对象和 C# 对象之间进行转换
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// 将 Old8Lang 对象转换为字典
    /// </summary>
    public static Dictionary<string, object>? ToDictionary(object? old8Object)
    {
        if (old8Object == null)
            return null;

        // 如果已经是字典类型，直接转换
        if (old8Object is Dictionary<string, object> dict)
            return dict;

        // 如果是 IDictionary，转换为 Dictionary<string, object>
        if (old8Object is IDictionary idict)
        {
            var result = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in idict)
            {
                if (entry.Key is string key)
                {
                    result[key] = entry.Value ?? string.Empty;
                }
            }
            return result;
        }

        // 使用反射从对象属性创建字典
        var type = old8Object.GetType();
        var properties = type.GetProperties();
        var resultDict = new Dictionary<string, object>();

        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(old8Object);
                if (value != null)
                {
                    resultDict[prop.Name] = value;
                }
            }
            catch
            {
                // 忽略无法访问的属性
            }
        }

        return resultDict;
    }

    /// <summary>
    /// 将 Old8Lang 列表转换为 C# List
    /// </summary>
    public static List<T> ToList<T>(object? old8List)
    {
        if (old8List == null)
            return [];

        if (old8List is List<T> typedList)
            return typedList;

        if (old8List is IEnumerable enumerable)
        {
            var result = new List<T>();
            foreach (var item in enumerable)
            {
                if (item is T typedItem)
                    result.Add(typedItem);
            }
            return result;
        }

        return [];
    }

    /// <summary>
    /// 将 Old8Lang 函数包装为 C# Action
    /// </summary>
    public static Action WrapAction(object? old8Func)
    {
        if (old8Func == null)
            return () => { };

        // 如果已经是 Action，直接返回
        if (old8Func is Action action)
            return action;

        // 尝试通过反射调用 Invoke 方法
        return () =>
        {
            try
            {
                var invokeMethod = old8Func.GetType().GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    invokeMethod.Invoke(old8Func, null);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TypeConverter] Error invoking Old8Lang function: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// 将 Old8Lang 函数包装为 C# Action&lt;T&gt;
    /// </summary>
    public static Action<T> WrapAction<T>(object? old8Func)
    {
        if (old8Func == null)
            return _ => { };

        // 如果已经是 Action<T>，直接返回
        if (old8Func is Action<T> action)
            return action;

        // 尝试通过反射调用 Invoke 方法
        return (arg) =>
        {
            try
            {
                var invokeMethod = old8Func.GetType().GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    invokeMethod.Invoke(old8Func, [arg!]);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TypeConverter] Error invoking Old8Lang function: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// 将 Old8Lang 函数包装为 C# Func&lt;TResult&gt;
    /// </summary>
    public static Func<TResult> WrapFunc<TResult>(object? old8Func)
    {
        if (old8Func == null)
            return () => default(TResult)!;

        // 如果已经是 Func<TResult>，直接返回
        if (old8Func is Func<TResult> func)
            return func;

        // 尝试通过反射调用 Invoke 方法
        return () =>
        {
            try
            {
                var invokeMethod = old8Func.GetType().GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    var result = invokeMethod.Invoke(old8Func, null);
                    if (result is TResult typedResult)
                        return typedResult;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TypeConverter] Error invoking Old8Lang function: {ex.Message}");
            }
            return default(TResult)!;
        };
    }

    /// <summary>
    /// 从字典中获取值
    /// </summary>
    public static T? GetValue<T>(Dictionary<string, object>? dict, string key, T? defaultValue = default)
    {
        if (dict == null || !dict.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            if (value is T typedValue)
                return typedValue;

            // 尝试类型转换
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 从字典中获取字符串值
    /// </summary>
    public static string GetString(Dictionary<string, object>? dict, string key, string defaultValue = "")
    {
        return GetValue(dict, key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// 从字典中获取整数值
    /// </summary>
    public static int GetInt(Dictionary<string, object>? dict, string key, int defaultValue = 0)
    {
        var value = GetValue<object>(dict, key);
        if (value == null)
            return defaultValue;

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 从字典中获取双精度浮点数值
    /// </summary>
    public static double GetDouble(Dictionary<string, object>? dict, string key, double defaultValue = 0.0)
    {
        var value = GetValue<object>(dict, key);
        if (value == null)
            return defaultValue;

        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 从字典中获取布尔值
    /// </summary>
    public static bool GetBool(Dictionary<string, object>? dict, string key, bool defaultValue = false)
    {
        var value = GetValue<object>(dict, key);
        if (value == null)
            return defaultValue;

        try
        {
            return Convert.ToBoolean(value);
        }
        catch
        {
            return defaultValue;
        }
    }
}
