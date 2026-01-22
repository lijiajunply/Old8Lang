using System.Collections;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;

namespace Old8Lang.Bytecode;

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

    private object? Add(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            // 尝试调用 _add 方法
            var result = TryCallOperatorMethod(objA, "_add", b);
            if (result != null)
                return result;

            throw new UnsupportedOperationError(new SourcePosition(), "_add", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return anyA.Plus(bValue);
            }
            catch (InvalidOperationError)
            {
                // 如果没有定义 _add 方法，抛出错误
                throw new UnsupportedOperationError(new SourcePosition(), "_add", anyA.ClassId.IdName);
            }
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return langValueA.Plus(bValue);
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行加法: {ex.Message}");
            }
        }

        // 原有的基本类型处理逻辑
        if (a is int ia && b is int ib)
        {
            // 检查是否会溢出，如果会则使用 long
            long result = (long)ia + (long)ib;
            if (result > int.MaxValue || result < int.MinValue)
                return result;
            return (int)result;
        }
        if (a is long la && b is long lb) return la + lb;
        if (a is int ia3 && b is long lb2) return (long)ia3 + lb2;
        if (a is long la2 && b is int ib3) return la2 + (long)ib3;
        if (a is double da && b is double db) return da + db;
        if (a is int ia2 && b is double db2) return ia2 + db2;
        if (a is double da2 && b is int ib2) return da2 + ib2;
        if (a is long la3 && b is double db3) return la3 + db3;
        if (a is double da3 && b is long lb3) return da3 + lb3;
        if (a is string sa || b is string sb) return ToString(a) + ToString(b);

        // 数组拼接：arr + [item] 或 [item] + arr
        if (a is object[] arrA && b is object[] arrB)
        {
            var result = new object[arrA.Length + arrB.Length];
            Array.Copy(arrA, 0, result, 0, arrA.Length);
            Array.Copy(arrB, 0, result, arrA.Length, arrB.Length);
            return result;
        }

        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行加法");
    }

    private object? Sub(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_sub", b);
            if (result != null)
                return result;
            throw new UnsupportedOperationError(new SourcePosition(), "_sub", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return anyA.Minus(bValue);
            }
            catch (InvalidOperationError)
            {
                throw new UnsupportedOperationError(new SourcePosition(), "_sub", anyA.ClassId.IdName);
            }
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return langValueA.Minus(bValue);
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行减法: {ex.Message}");
            }
        }

        if (a is int ia && b is int ib) return ia - ib;
        if (a is long la && b is long lb) return la - lb;
        if (a is int ia3 && b is long lb2) return (long)ia3 - lb2;
        if (a is long la2 && b is int ib3) return la2 - (long)ib3;
        if (a is double da && b is double db) return da - db;
        if (a is int ia2 && b is double db2) return ia2 - db2;
        if (a is double da2 && b is int ib2) return da2 - ib2;
        if (a is long la3 && b is double db3) return la3 - db3;
        if (a is double da3 && b is long lb3) return da3 - lb3;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行减法");
    }

    private object? Mul(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_mul", b);
            if (result != null)
                return result;
            throw new UnsupportedOperationError(new SourcePosition(), "_mul", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return anyA.Times(bValue);
            }
            catch (InvalidOperationError)
            {
                throw new UnsupportedOperationError(new SourcePosition(), "_mul", anyA.ClassId.IdName);
            }
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return langValueA.Times(bValue);
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行乘法: {ex.Message}");
            }
        }

        if (a is int ia && b is int ib)
        {
            // 检查是否会溢出，如果会则使用 long
            long result = (long)ia * (long)ib;
            if (result > int.MaxValue || result < int.MinValue)
                return result;
            return (int)result;
        }
        if (a is long la && b is long lb) return la * lb;
        if (a is int ia3 && b is long lb2) return (long)ia3 * lb2;
        if (a is long la2 && b is int ib3) return la2 * (long)ib3;
        if (a is double da && b is double db) return da * db;
        if (a is int ia2 && b is double db2) return ia2 * db2;
        if (a is double da2 && b is int ib2) return da2 * ib2;
        if (a is long la3 && b is double db3) return la3 * db3;
        if (a is double da3 && b is long lb3) return da3 * lb3;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行乘法");
    }

    private object? Div(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_div", b);
            if (result != null)
                return result;
            throw new UnsupportedOperationError(new SourcePosition(), "_div", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return anyA.Divide(bValue);
            }
            catch (InvalidOperationError)
            {
                throw new UnsupportedOperationError(new SourcePosition(), "_div", anyA.ClassId.IdName);
            }
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return langValueA.Divide(bValue);
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行除法: {ex.Message}");
            }
        }

        if (a is int ia && b is int ib) return ia / ib;
        if (a is long la && b is long lb) return la / lb;
        if (a is int ia3 && b is long lb2) return (long)ia3 / lb2;
        if (a is long la2 && b is int ib3) return la2 / (long)ib3;
        if (a is double da && b is double db) return da / db;
        if (a is int ia2 && b is double db2) return ia2 / db2;
        if (a is double da2 && b is int ib2) return da2 / ib2;
        if (a is long la3 && b is double db3) return la3 / db3;
        if (a is double da3 && b is long lb3) return da3 / lb3;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行除法");
    }

    private object? Mod(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_mod", b);
            if (result != null)
                return result;
            throw new UnsupportedOperationError(new SourcePosition(), "_mod", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return anyA.Mod(bValue);
            }
            catch (InvalidOperationError)
            {
                throw new UnsupportedOperationError(new SourcePosition(), "_mod", anyA.ClassId.IdName);
            }
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return langValueA.Mod(bValue);
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行取模: {ex.Message}");
            }
        }

        if (a is int ia && b is int ib) return ia % ib;
        if (a is double da && b is double db) return da % db;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行取模");
    }

    private object? Pow(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_pow", b);
            if (result != null)
                return result;
            throw new UnsupportedOperationError(new SourcePosition(), "_pow", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return anyA.Power(bValue);
            }
            catch (InvalidOperationError)
            {
                throw new UnsupportedOperationError(new SourcePosition(), "_pow", anyA.ClassId.IdName);
            }
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            try
            {
                return langValueA.Power(bValue);
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行幂运算: {ex.Message}");
            }
        }

        double da = ToDouble(a);
        double db = ToDouble(b);
        return Math.Pow(da, db);
    }

    private object? Neg(object? a)
    {
        if (a is int ia) return -ia;
        if (a is double da) return -da;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 执行取反");
    }

    private new bool Equals(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_eq", b);
            if (result != null && result is bool boolResult)
                return boolResult;
            // 如果没有定义 _eq 方法，使用引用相等
            return ReferenceEquals(a, b);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            return anyA.Equal(bValue);
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            return langValueA.Equal(bValue);
        }

        if (a is int ia && b is int ib) return ia == ib;
        if (a is double da && b is double db) return Math.Abs(da - db) < 1e-10;
        if (a is bool ba && b is bool bb) return ba == bb;
        if (a is string sa && b is string sb) return sa == sb;

        // 处理枚举值比较
        if (a is EnumLangValue ea && b is EnumLangValue eb)
        {
            return ea.EnumTypeName == eb.EnumTypeName && ea.Value == eb.Value;
        }

        return object.Equals(a, b);
    }

    private bool Greater(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_gt", b);
            if (result != null && result is bool boolResult)
                return boolResult;
            throw new UnsupportedOperationError(new SourcePosition(), "_gt", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            return anyA.Greater(bValue);
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            return langValueA.Greater(bValue);
        }

        if (a is int ia && b is int ib) return ia > ib;
        if (a is double da && b is double db) return da > db;
        if (a is int ia2 && b is double db2) return ia2 > db2;
        if (a is double da2 && b is int ib2) return da2 > ib2;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行大于比较");
    }

    private bool Less(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_lt", b);
            if (result != null && result is bool boolResult)
                return boolResult;
            throw new UnsupportedOperationError(new SourcePosition(), "_lt", objA.ClassName);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            return anyA.Less(bValue);
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            return langValueA.Less(bValue);
        }

        if (a is int ia && b is int ib) return ia < ib;
        if (a is double da && b is double db) return da < db;
        if (a is int ia2 && b is double db2) return ia2 < db2;
        if (a is double da2 && b is int ib2) return da2 < ib2;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行小于比较");
    }

    private bool GreaterEqual(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_ge", b);
            if (result != null && result is bool boolResult)
                return boolResult;
            // 如果没有 _ge 方法，尝试使用 _gt 和 _eq
            return Greater(a, b) || Equals(a, b);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            return anyA.GreaterEqual(bValue);
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            return langValueA.GreaterEqual(bValue);
        }

        return Greater(a, b) || Equals(a, b);
    }

    private bool LessEqual(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_le", b);
            if (result != null && result is bool boolResult)
                return boolResult;
            // 如果没有 _le 方法，尝试使用 _lt 和 _eq
            return Less(a, b) || Equals(a, b);
        }

        // 检查是否是 AnyLangValue（运算符重载）
        if (a is AnyLangValue anyA)
        {
            var bValue = ConvertToLangValueType(b);
            return anyA.LessEqual(bValue);
        }

        // 检查是否是 LangValueType（IntLangValue, DoubleLangValue 等）
        if (a is LangValueType langValueA)
        {
            var bValue = ConvertToLangValueType(b);
            return langValueA.LessEqual(bValue);
        }

        return Less(a, b) || Equals(a, b);
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
    private object? CallNativeFunction(string funcName, object?[] args)
    {
        // 首先尝试从全局函数注册表中查找
        var globalFunction = GlobalFunctionRegistry.Instance.TryGetFunction(funcName);
        if (globalFunction != null)
        {
            try
            {
                return globalFunction.ExecuteInVM(args);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"调用全局函数 {funcName} 时发生错误: {ex.Message}");
            }
        }

        // 处理特殊的辅助函数（不在全局函数注册表中）
        switch (funcName)
        {
            case "System.String::Concat":
            {
                // 字符串拼接
                if (args.Length > 0 && args[0] is object[] array)
                {
                    return string.Concat(array.Select(ToString));
                }

                return string.Concat(args.Select(ToString));
            }

            case "CheckRange":
                // 参数: value, start, end, includeStart, includeEnd
                if (args.Length >= 5)
                {
                    double value = Convert.ToDouble(args[0]);
                    double start = Convert.ToDouble(args[1]);
                    double end = Convert.ToDouble(args[2]);
                    bool includeStart = Convert.ToBoolean(args[3]);
                    bool includeEnd = Convert.ToBoolean(args[4]);

                    bool inRange = true;
                    if (includeStart)
                        inRange &= value >= start;
                    else
                        inRange &= value > start;

                    if (includeEnd)
                        inRange &= value <= end;
                    else
                        inRange &= value < end;

                    return inRange;
                }

                return false;

            case "FlattenTuple":
                // 展平元组为列表
                if (args.Length > 0 && args[0] is TupleLangValue tuple)
                {
                    return FlattenTupleHelper(tuple);
                }

                return new List<object?>();

            case "GetCount":
                // 获取集合元素数量
                if (args.Length > 0)
                {
                    return args[0] switch
                    {
                        string str => str.Length,
                        Array array => array.Length,
                        IList list => list.Count,
                        _ => 0
                    };
                }

                return 0;

            case "ResourceManagerTryDispose":
                if (args.Length > 0)
                {
                    int resourceId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.TryDispose(resourceId);
                }

                return null;

            default:
                throw new MethodNotFoundError(new SourcePosition(), funcName);
        }
    }

    /// <summary>
    /// 对数组执行切片操作
    /// </summary>
    private object SliceArray(Array array, int start, int end, int step)
    {
        var length = array.Length;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new List<object?>();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Add(array.GetValue(i));
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Add(array.GetValue(i));
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 对列表执行切片操作
    /// </summary>
    private object? SliceList(IList list, int start, int end, int step)
    {
        var length = list.Count;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new List<object?>();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Add(list[i]);
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Add(list[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// 对字符串执行切片操作
    /// </summary>
    private string SliceString(string str, int start, int end, int step)
    {
        var length = str.Length;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new System.Text.StringBuilder();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Append(str[i]);
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Append(str[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 重新排列参数以匹配函数参数定义(支持命名参数)
    /// </summary>
    /// <param name="function">函数元数据</param>
    /// <param name="positionalArgs">位置参数</param>
    /// <param name="namedArgNames">命名参数名称数组</param>
    /// <param name="namedArgValues">命名参数值数组</param>
    /// <returns>按函数参数定义顺序排列的参数数组</returns>
    private object?[] ArrangeArgumentsWithNamed(FunctionMetadata function, object?[] positionalArgs,
        string[] namedArgNames, object?[] namedArgValues)
    {
        int paramCount = function.Parameters.Count;
        var args = new object?[paramCount];
        var filled = new bool[paramCount]; // 跟踪哪些参数位置已被填充

        // 首先填充位置参数
        for (int i = 0; i < positionalArgs.Length; i++)
        {
            if (i >= paramCount)
            {
                throw new ArgumentError(new SourcePosition(), $"函数 {function.Name} 期望 {paramCount} 个参数，但提供了过多的参数");
            }

            args[i] = positionalArgs[i];
            filled[i] = true;
        }

        // 然后根据命名参数填充剩余位置
        for (int i = 0; i < namedArgNames.Length; i++)
        {
            string paramName = namedArgNames[i];
            object? paramValue = namedArgValues[i];

            // 查找参数在函数参数列表中的位置
            int paramIndex = function.Parameters.IndexOf(paramName);
            if (paramIndex == -1)
            {
                throw new ArgumentError(new SourcePosition(), $"函数 {function.Name} 没有名为 '{paramName}' 的参数");
            }

            // 检查该位置是否已被位置参数占用
            if (filled[paramIndex])
            {
                throw new ArgumentError(new SourcePosition(), $"参数 '{paramName}' 已通过位置参数提供");
            }

            args[paramIndex] = paramValue;
            filled[paramIndex] = true;
        }

        // 检查是否所有参数都已提供，如果没有则使用默认值
        for (int i = 0; i < paramCount; i++)
        {
            if (!filled[i])
            {
                // 参数未提供，检查是否有默认值
                if (i < function.DefaultValues.Count && function.DefaultValues[i] != null)
                {
                    // 使用默认值
                    args[i] = function.DefaultValues[i];
                    filled[i] = true;
                }
                else
                {
                    // 没有默认值，抛出错误
                    throw new ArgumentError(new SourcePosition(), $"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
                }
            }
        }

        return args;
    }

    // ===== Task 管理 =====

    /// <summary>
    /// 注册 Task 并返回 ID
    /// </summary>
    private int RegisterTask(TaskLangValue task)
    {
        int taskId = _nextTaskId++;
        _tasks[taskId] = task;
        return taskId;
    }

    /// <summary>
    /// 获取 Task
    /// </summary>
    private TaskLangValue GetTask(int taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new StateError(new SourcePosition(), $"Task ID {taskId} 不存在");
        }

        return task;
    }

    /// <summary>
    /// 辅助方法：将 object? 转换为 LangValueType
    /// </summary>
    private LangValueType ConvertToLangValue(object? value)
    {
        if (value == null) return new VoidLangValue();
        if (value is LangValueType langValue) return langValue;
        if (value is int intValue) return new IntLangValue(intValue);
        if (value is double doubleValue) return new DoubleLangValue(doubleValue);
        if (value is string stringValue) return new StringLangValue(stringValue);
        if (value is bool boolValue) return new BoolLangValue(boolValue);
        return new VoidLangValue();
    }

    /// <summary>
    /// 展平元组为列表（用于 match 表达式的元组解构）
    /// 例如：((1, 2), 3) -> [1, 2, 3]
    /// </summary>
    private List<object?> FlattenTupleHelper(TupleLangValue tuple)
    {
        return tuple.GetItems().Cast<object?>().ToList();
    }

    /// <summary>
    /// 尝试调用 BytecodeObjectInstance 的运算符重载方法
    /// </summary>
    private object? TryCallOperatorMethod(BytecodeObjectInstance obj, string methodName, object? operand)
    {
        // 查找类的元数据
        var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == obj.ClassName);
        if (classMetadata == null)
            return null;

        // 查找方法
        var method = classMetadata.Methods.FirstOrDefault(m => m.Name == methodName);
        if (method == null)
            return null;

        // 调用方法（第一个参数是 this，第二个参数是操作数）
        var args = new object?[] { obj, operand };
        return ExecuteFunctionAndGetResult(method.Function, args);
    }

    /// <summary>
    /// 将虚拟机栈上的值转换为 LangValueType
    /// </summary>
    private LangValueType ConvertToLangValueType(object? value)
    {
        return value switch
        {
            null => new NullLangValue(),
            int i => new IntLangValue(i),
            long l => new IntLangValue((int)l), // 将 long 转换为 int（如果溢出会在运行时报错）
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            LangValueType lvt => lvt,
            _ => throw new CastError(new SourcePosition(), value.GetType().Name, "LangValueType")
        };
    }

    /// <summary>
    /// 调用类型的扩展方法或实例方法（类似于解释器模式中的 FromClassToResult）
    /// </summary>
    /// <param name="obj">要调用方法的对象</param>
    /// <param name="methodName">方法名</param>
    /// <param name="args">方法参数</param>
    /// <returns>方法返回值</returns>
    private object? InvokeTypeMethod(object obj, string methodName, object?[] args)
    {
        if (obj == null)
        {
            throw new NullReferenceError(new SourcePosition(), methodName);
        }

        // 特殊处理 ToStr：对于数字类型，使用自定义格式化
        if (methodName == "ToStr")
        {
            // 对于 double，如果是整数值，使用固定格式（不使用科学计数法）
            if (obj is double d)
            {
                if (Math.Abs(d - Math.Round(d)) < 0.0000001)
                {
                    return d.ToString("F0");
                }
                return d.ToString();
            }
            // 对于 long，直接转换为字符串
            if (obj is long l)
            {
                return l.ToString();
            }
        }

        Type? extensionType = null;
        System.Reflection.MethodInfo? method = null;

        // 对于 C# 原生类型，查找对应的扩展方法类
        if (obj is string)
        {
            extensionType = typeof(StringExtensions);
        }
        else if (obj is object[] && obj.GetType() == typeof(object[]))
        {
            extensionType = typeof(ArrayExtensions);
        }
        else if (obj is List<object?>)
        {
            extensionType = typeof(ListExtensions);
        }
        else if (obj is Dictionary<object, object?>)
        {
            extensionType = typeof(DictionaryExtensions);
        }
        // 对于基本类型(int, double, bool, char)，查找对应的扩展方法类
        else if (obj is int || obj is double || obj is bool || obj is char)
        {
            extensionType = typeof(PrimitiveExtensions);
        }
        // 对于 Old8Lang 类型，查找对应的扩展方法类
        else if (obj is DictionaryLangValue)
        {
            extensionType = typeof(DictionaryValueFuncStatic);
        }
        else if (obj is ListLangValue)
        {
            extensionType = typeof(ListValueFuncStatic);
        }
        else if (obj is TaskLangValue)
        {
            extensionType = typeof(TaskValueFuncStatic);
        }
        else if (obj is ThreadLangValue)
        {
            extensionType = typeof(ThreadValueFuncStatic);
        }
        else if (obj is StringLangValue)
        {
            extensionType = typeof(StringValueFuncStatic);
        }
        else if (obj is TupleLangValue)
        {
            extensionType = typeof(TupleValueFuncStatic);
        }
        else if (obj is ArrayLangValue)
        {
            extensionType = typeof(ArrayValueFuncStatic);
        }
        else if (obj is CharLangValue)
        {
            extensionType = typeof(CharValueFuncStatic);
        }

        // 如果找到扩展类型，尝试查找扩展方法
        if (extensionType != null)
        {
            var allMethods = extensionType.GetMethods().Where(x => x.Name == methodName).ToArray();
            if (allMethods.Length > 0)
            {
                // 预期参数数量 = 传入参数数量 + 1 (扩展方法的第一个参数是对象本身)
                var expectedParamCount = args.Length + 1;

                // 查找参数数量和类型都匹配的方法
                method = allMethods.FirstOrDefault(x =>
                {
                    var parameters = x.GetParameters();
                    if (parameters.Length != expectedParamCount) return false;

                    // 检查第一个参数（扩展方法的 'this' 参数）类型兼容性
                    if (obj != null && !parameters[0].ParameterType.IsInstanceOfType(obj))
                    {
                        return false;
                    }

                    return true;
                });

                // 如果没找到，查找有可选参数的方法
                if (method == null)
                {
                    method = allMethods.FirstOrDefault(x =>
                    {
                        var parameters = x.GetParameters();
                        if (parameters.Length < expectedParamCount) return false;

                        // 检查除了第一个参数（对象本身）之外，剩余的参数是否都是可选的
                        for (int i = expectedParamCount; i < parameters.Length; i++)
                        {
                            if (!parameters[i].IsOptional && !parameters[i].HasDefaultValue)
                                return false;
                        }

                        return true;
                    });
                }

                // 如果还是没找到，使用第一个方法
                method ??= allMethods[0];
            }
        }

        // 如果没有找到扩展方法，尝试在类型本身上查找实例方法
        if (method == null)
        {
            var objType = obj.GetType();

            // 特殊处理：将 ToStr 映射到 ToString
            var actualMethodName = methodName == "ToStr" ? "ToString" : methodName;

            var allInstanceMethods = objType.GetMethods().Where(x => x.Name == actualMethodName).ToArray();
            if (allInstanceMethods.Length > 0)
            {
                // 对于实例方法，预期参数数量 = 传入参数数量
                var expectedParamCount = args.Length;
                method = allInstanceMethods.FirstOrDefault(x => x.GetParameters().Length == expectedParamCount)
                         ?? allInstanceMethods[0];
            }
        }

        // 如果还是找不到，尝试 ValueTypeFuncStatic
        if (method == null)
        {
            var valueTypeFuncStatic = typeof(ValueTypeFuncStatic);
            method = valueTypeFuncStatic.GetMethod(methodName);
        }

        // 如果找不到方法，抛出异常
        if (method == null)
        {
            throw new MethodNotFoundError(new SourcePosition(), methodName, obj.GetType().Name);
        }

        // 准备方法调用参数
        var parameters = method.GetParameters();
        var invokeArgs = new List<object?>();

        // 对于静态方法（扩展方法），第一个参数是对象本身
        if (method.IsStatic && parameters.Length > 0)
        {
            invokeArgs.Add(obj);
        }

        // 添加传入的参数，并进行类型转换
        int startIndex = invokeArgs.Count; // 记录参数起始位置
        invokeArgs.AddRange(args);

        // 类型转换：将 C# 原始类型转换为 Old8Lang 类型（如果需要）
        for (int i = startIndex; i < invokeArgs.Count && i < parameters.Length; i++)
        {
            var arg = invokeArgs[i];
            var paramType = parameters[i].ParameterType;

            // 如果参数期望 LangValueType，但传入的是 C# 原始类型，则进行转换
            if (paramType == typeof(LangValueType) || paramType.IsSubclassOf(typeof(LangValueType)))
            {
                if (arg is not LangValueType)
                {
                    invokeArgs[i] = ConvertToLangValueType(arg);
                }
            }
        }

        // 补充缺失的可选参数
        if (invokeArgs.Count < parameters.Length)
        {
            for (int i = invokeArgs.Count; i < parameters.Length; i++)
            {
                if (parameters[i].IsOptional || parameters[i].HasDefaultValue)
                {
                    invokeArgs.Add(parameters[i].DefaultValue);
                }
            }
        }

        // 调用方法
        object? invokeInstance = method.IsStatic ? null : obj;
        return method.Invoke(invokeInstance, invokeArgs.ToArray());
    }

    // === 模块加载方法 ===

    /// <summary>
    /// 加载模块
    /// </summary>
    private void LoadModule(string moduleName)
    {
        // 检查模块是否已加载
        if (_moduleRegistry.IsModuleLoaded(moduleName))
        {
            return; // 模块已加载，直接返回
        }

        // 检测循环依赖
        if (!_moduleRegistry.MarkModuleLoading(moduleName))
        {
            throw new ImportError(new SourcePosition(), moduleName, $"检测到循环依赖：模块 '{moduleName}' 正在加载中");
        }

        try
        {
            // 加载并编译模块
            var moduleBytecode = _moduleLoader.LoadModule(moduleName);

            // 创建模块的全局变量空间
            var moduleGlobals = new Dictionary<string, object?>();
            foreach (var globalVar in moduleBytecode.GlobalVariables)
            {
                moduleGlobals[globalVar] = null;
            }

            // 执行模块的初始化代码（如果有入口点）
            if (moduleBytecode.EntryPointIndex >= 0)
            {
                // 创建临时虚拟机执行模块初始化
                var moduleVM = new VirtualMachine(moduleBytecode, _baseDirectory);

                // 复制模块注册表（避免重复加载依赖）
                foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                {
                    var loadedModule = _moduleRegistry.GetModule(loadedModuleName);
                    if (loadedModule != null)
                    {
                        moduleVM._moduleRegistry.RegisterModule(
                            loadedModuleName,
                            loadedModule.BytecodeFile,
                            loadedModule.Globals
                        );
                    }
                }

                // 执行模块初始化
                moduleVM.Execute();

                // 获取模块的全局变量
                moduleGlobals = moduleVM._globals;

                // 传递性导入：将模块VM加载的所有依赖模块也注册到当前VM的模块注册表中
                foreach (var depModuleName in moduleVM._moduleRegistry.GetLoadedModuleNames())
                {
                    // 跳过当前正在加载的模块自己
                    if (depModuleName == moduleName)
                    {
                        continue;
                    }

                    // 如果当前VM还没有加载这个依赖模块，则注册它
                    if (!_moduleRegistry.IsModuleLoaded(depModuleName))
                    {
                        var depModule = moduleVM._moduleRegistry.GetModule(depModuleName);
                        if (depModule != null)
                        {
                            _moduleRegistry.RegisterModule(
                                depModuleName,
                                depModule.BytecodeFile,
                                depModule.Globals
                            );
                        }
                    }
                }
            }

            // 注册模块
            _moduleRegistry.RegisterModule(moduleName, moduleBytecode, moduleGlobals);
        }
        catch (Exception ex)
        {
            throw new ImportError(new SourcePosition(), moduleName, $"加载模块 '{moduleName}' 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建对象实例（用于从模块导入的类的实例化）
    /// </summary>
    private BytecodeObjectInstance CreateObjectInstance(ClassMetadata classMetadata, object?[] constructorArgs)
    {
        // 创建对象实例
        var obj = new BytecodeObjectInstance(classMetadata.Name);

        // 初始化所有字段为默认值（包括父类字段）
        var allFields = new List<FieldMetadata>();
        var currentClass = classMetadata;
        while (currentClass != null)
        {
            allFields.AddRange(currentClass.Fields);

            // 查找父类（首先从当前字节码文件，然后从模块）
            if (!string.IsNullOrEmpty(currentClass.BaseClassName))
            {
                currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                if (currentClass == null)
                {
                    // 从模块中查找父类
                    foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                    {
                        try
                        {
                            var symbol =
                                _moduleRegistry.GetModuleSymbol(loadedModuleName, currentClass?.BaseClassName ?? "");
                            if (symbol is ClassMetadata baseClass)
                            {
                                currentClass = baseClass;
                                break;
                            }
                        }
                        catch
                        {
                            // 继续查找
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }

        // 初始化所有字段
        foreach (var field in allFields)
        {
            if (!obj.Fields.ContainsKey(field.Name))
            {
                obj.Fields[field.Name] = null;
            }
        }

        // 查找并调用构造函数（init方法）
        var initMethod = classMetadata.Methods.FirstOrDefault(m => m.Name == "init");
        if (initMethod != null)
        {
            // 准备方法调用参数：第一个参数是 this（对象本身）
            var methodArgs = new object?[constructorArgs.Length + 1];
            methodArgs[0] = obj;
            Array.Copy(constructorArgs, 0, methodArgs, 1, constructorArgs.Length);

            // 检查参数类型
            ValidateConstructorParameterTypes(initMethod.Function, methodArgs, classMetadata.Name);

            // 调用构造函数
            CallFunction(initMethod.Function, methodArgs);
        }

        return obj;
    }

    /// <summary>
    /// 验证构造函数参数类型
    /// </summary>
    private void ValidateConstructorParameterTypes(FunctionMetadata function, object?[] args, string className)
    {
        // 如果没有参数类型信息，跳过检查
        if (function.ParameterTypes == null || function.ParameterTypes.Count == 0)
            return;

        for (int i = 0; i < Math.Min(args.Length, function.ParameterTypes.Count); i++)
        {
            var expectedType = function.ParameterTypes[i];

            // 如果没有类型注解（空字符串），跳过检查
            if (string.IsNullOrEmpty(expectedType))
                continue;

            var actualValue = args[i];

            // 使用 CheckTypeMatch 进行类型检查
            if (!CheckTypeMatch(expectedType, actualValue))
            {
                var actualType = GetValueTypeName(actualValue);
                var paramName = i < function.Parameters.Count ? function.Parameters[i] : $"参数{i}";
                throw new TypeError(
                    new SourcePosition(0, 0),
                    expectedType,
                    actualType,
                    $"构造函数 '{className}' 的参数 '{paramName}' 类型不匹配"
                );
            }
        }
    }

    /// <summary>
    /// 验证字段赋值类型
    /// </summary>
    private void ValidateFieldType(string className, string fieldName, object? value, Instruction instruction)
    {
        // 查找类元数据
        var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == className);
        if (classMetadata == null)
            return;

        // 查找字段元数据（包括父类字段）
        FieldMetadata? fieldMetadata = null;
        var currentClass = classMetadata;
        while (currentClass != null && fieldMetadata == null)
        {
            fieldMetadata = currentClass.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (fieldMetadata == null && !string.IsNullOrEmpty(currentClass.BaseClassName))
            {
                currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
            }
            else
            {
                break;
            }
        }

        if (fieldMetadata == null || string.IsNullOrEmpty(fieldMetadata.TypeName))
            return;

        // 如果字段类型包含泛型类型参数，尝试从类的泛型类型映射中替换
        var fieldTypeName = fieldMetadata.TypeName;
        if (classMetadata.GenericTypeMapping != null && classMetadata.GenericTypeMapping.Count > 0)
        {
            // 检查 GenericTypeMapping 中是否包含泛型类型参数（如 Wrapper<T>）
            // 这是编译器的一个 bug，会导致类型解析错误
            // 作为临时变通方案，如果检测到这种情况，跳过类型检查
            bool hasNestedGenericMapping = classMetadata.GenericTypeMapping.Values.Any(v => v.Contains('<'));
            if (hasNestedGenericMapping)
            {
                // 跳过类型检查，因为编译器生成的 GenericTypeMapping 可能不正确
                return;
            }

            // 使用类的泛型类型映射替换字段类型中的泛型类型参数
            fieldTypeName = ResolveGenericType(fieldMetadata.TypeName, classMetadata.GenericTypeMapping);
        }

        // 检查类型匹配
        if (!CheckTypeMatch(fieldTypeName, value))
        {
            var actualType = GetValueTypeName(value);
            throw new TypeError(
                GetPosition(instruction),
                fieldTypeName,
                actualType,
                $"字段 '{className}.{fieldName}' 类型不匹配"
            );
        }
    }

    /// <summary>
    /// 验证静态字段赋值类型
    /// </summary>
    private void ValidateStaticFieldType(ClassMetadata classMetadata, string fieldName, object? value, Instruction instruction)
    {
        // 查找静态字段元数据
        var fieldMetadata = classMetadata.StaticFields.FirstOrDefault(f => f.Name == fieldName);
        if (fieldMetadata == null || string.IsNullOrEmpty(fieldMetadata.TypeName))
            return;

        // 检查类型匹配
        if (!CheckTypeMatch(fieldMetadata.TypeName, value))
        {
            var actualType = GetValueTypeName(value);
            throw new TypeError(
                GetPosition(instruction),
                fieldMetadata.TypeName,
                actualType,
                $"静态字段 '{classMetadata.Name}.{fieldName}' 类型不匹配"
            );
        }
    }

    /// <summary>
    /// 验证逻辑运算符的操作数类型
    /// </summary>
    private void ValidateLogicalOperand(object? value, string operatorName, Instruction instruction)
    {
        // 如果值是布尔类型，直接返回
        if (value is bool)
            return;

        // 如果值是 BoolLangValue，直接返回
        if (value is BoolLangValue)
            return;

        // 其他类型不允许用于逻辑运算
        var actualType = GetValueTypeName(value);
        throw new TypeError(
            GetPosition(instruction),
            "bool",
            actualType,
            $"逻辑运算符 '{operatorName}' 的操作数必须是布尔类型"
        );
    }

    /// <summary>
    /// 执行函数并获取结果（用于异步调用）
    /// </summary>
    public object? ExecuteFunctionAndGetResult(FunctionMetadata function, object?[] args)
    {
        CallFunction(function, args);
        return _stack.Count > 0 ? _stack.Pop() : null;
    }
}

/// <summary>
/// 虚拟机异常包装类
/// 用于在C#异常机制中传递Old8Lang的异常对象
/// </summary>
public class VmException : Exception
{
    public object? Value { get; }

    public VmException(object? value) : base(GetMessage(value))
    {
        Value = value;
    }

    private static string GetMessage(object? value)
    {
        if (value == null) return "null";
        if (value is LangValueType langValue) return langValue.ToDisplayString();
        return value.ToString() ?? "";
    }
}

/// <summary>
/// 对象相等性比较器 - 用于 GroupBy 操作的键比较
/// </summary>
internal class ObjectEqualityComparer : IEqualityComparer<object>
{
    public new bool Equals(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;

        // 使用对象的 Equals 方法进行比较
        return x.Equals(y);
    }

    public int GetHashCode(object obj)
    {
        return obj?.GetHashCode() ?? 0;
    }
}