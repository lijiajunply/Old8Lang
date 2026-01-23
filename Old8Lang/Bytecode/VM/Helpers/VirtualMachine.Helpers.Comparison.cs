using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 比较运算
/// </summary>
public partial class VirtualMachine
{
    private new bool Equals(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

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
            if (result is bool boolResult)
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
            if (result is bool boolResult)
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


}
