using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 比较运算（类型化热路径 + 动态/重载慢路径）
/// </summary>
public partial class VirtualMachine
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Equals(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return a.AsInt == b.AsInt;

        if (a.Kind == VmValueKind.Null && b.Kind == VmValueKind.Null)
            return true;

        if (a.Kind == VmValueKind.Null || b.Kind == VmValueKind.Null)
            return false;

        if (a.Kind == VmValueKind.Bool && b.Kind == VmValueKind.Bool)
            return a.AsBool == b.AsBool;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return Math.Abs(a.AsDouble - b.AsDouble) < 1e-10;

        if (a.Kind == VmValueKind.Char && b.Kind == VmValueKind.Char)
            return a.AsChar == b.AsChar;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return a.AsLong == b.AsLong;

        // 混合基础数值类型：与既有逻辑一致（3 == 3.0 为 true）
        if (a.Kind is VmValueKind.Int or VmValueKind.Long or VmValueKind.Double &&
            b.Kind is VmValueKind.Int or VmValueKind.Long or VmValueKind.Double)
        {
            return a.ToDoubleValue() == b.ToDoubleValue();
        }

        if (a.Kind == VmValueKind.Object && b.Kind == VmValueKind.Object &&
            a.ObjectRef is string sa && b.ObjectRef is string sb)
        {
            return string.Equals(sa, sb, StringComparison.Ordinal);
        }

        return Equals(a.ToObject(), b.ToObject());
    }

    private new bool Equals(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // 热路径：整数相等优先判定，避免每次都先走运算符重载分支
        if (a is int fastIa && b is int fastIb) return fastIa == fastIb;

        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_eq", b);
            if (result is bool boolResult)
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

        if (a is double da && b is double db) return Math.Abs(da - db) < 1e-10;
        // 混合数值类型按数值比较：解释器里 `3 == 3.0` 为 true。
        // 这里不处理会落到 object.Equals，int 与 double 永远是 false，
        // 连带 `3 >= 3.0` / `3.0 <= 3` 也被判成 false。
        if (IsNumericValue(a) && IsNumericValue(b))
        {
            return Convert.ToDouble(a) == Convert.ToDouble(b);
        }

        if (a is bool ba && b is bool bb) return ba == bb;
        if (a is string sa && b is string sb) return sa == sb;

        // 处理枚举值比较
        if (a is EnumLangValue ea && b is EnumLangValue eb)
        {
            return ea.EnumTypeName == eb.EnumTypeName && ea.Value == eb.Value;
        }

        return object.Equals(a, b);
    }

    /// <summary>
    /// 判断值是否是 CLR 原生数值（用于混合数值类型的相等比较）。
    /// </summary>
    private static bool IsNumericValue(object? value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Greater(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return a.AsInt > b.AsInt;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return a.AsDouble > b.AsDouble;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return a.AsInt > b.AsDouble;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return a.AsDouble > b.AsInt;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return a.AsLong > b.AsLong;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return (long)a.AsInt > b.AsLong;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return a.AsLong > (long)b.AsInt;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Double)
            return a.AsLong > b.AsDouble;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Long)
            return a.AsDouble > b.AsLong;

        return GreaterSlow(a.ToObject(), b.ToObject());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool GreaterSlow(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_gt", b);
            if (result is bool boolResult)
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

        if (a is string sa && b is string sb) return string.CompareOrdinal(sa, sb) > 0;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行大于比较");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Less(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return a.AsInt < b.AsInt;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return a.AsDouble < b.AsDouble;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return a.AsInt < b.AsDouble;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return a.AsDouble < b.AsInt;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return a.AsLong < b.AsLong;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return (long)a.AsInt < b.AsLong;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return a.AsLong < (long)b.AsInt;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Double)
            return a.AsLong < b.AsDouble;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Long)
            return a.AsDouble < b.AsLong;

        return LessSlow(a.ToObject(), b.ToObject());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool LessSlow(object? a, object? b)
    {
        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_lt", b);
            if (result is bool boolResult)
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

        if (a is string sa && b is string sb) return string.CompareOrdinal(sa, sb) < 0;
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行小于比较");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GreaterEqual(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return a.AsInt >= b.AsInt;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return a.AsDouble >= b.AsDouble || Math.Abs(a.AsDouble - b.AsDouble) < 1e-10;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return a.AsInt >= b.AsDouble;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return a.AsDouble >= b.AsInt;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return a.AsLong >= b.AsLong;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return (long)a.AsInt >= b.AsLong;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return a.AsLong >= (long)b.AsInt;

        return GreaterEqualSlow(a, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool GreaterEqualSlow(VmValue aVal, VmValue bVal)
    {
        var a = aVal.ToObject();
        var b = bVal.ToObject();

        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_ge", b);
            if (result is bool boolResult)
                return boolResult;
            // 如果没有 _ge 方法，尝试使用 _gt 和 _eq
            return GreaterSlow(a, b) || Equals(a, b);
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

        return Greater(aVal, bVal) || Equals(aVal, bVal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool LessEqual(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return a.AsInt <= b.AsInt;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return a.AsDouble <= b.AsDouble || Math.Abs(a.AsDouble - b.AsDouble) < 1e-10;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return a.AsInt <= b.AsDouble;

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return a.AsDouble <= b.AsInt;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return a.AsLong <= b.AsLong;

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return (long)a.AsInt <= b.AsLong;

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return a.AsLong <= (long)b.AsInt;

        return LessEqualSlow(a, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool LessEqualSlow(VmValue aVal, VmValue bVal)
    {
        var a = aVal.ToObject();
        var b = bVal.ToObject();

        // 检查是否是 BytecodeObjectInstance（运算符重载）
        if (a is BytecodeObjectInstance objA)
        {
            var result = TryCallOperatorMethod(objA, "_le", b);
            if (result is bool boolResult)
                return boolResult;
            // 如果没有 _le 方法，尝试使用 _lt 和 _eq
            return LessSlow(a, b) || Equals(a, b);
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

        return Less(aVal, bVal) || Equals(aVal, bVal);
    }
}
