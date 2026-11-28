using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 算术运算（类型化热路径 + 动态/重载慢路径）
/// </summary>
public partial class VirtualMachine
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue Add(VmValue a, VmValue b)
    {
        // 极热路径：32 位整数加法，溢出时升级为 long
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
        {
            long fastResult = (long)a.AsInt + (long)b.AsInt;
            if (fastResult > int.MaxValue || fastResult < int.MinValue)
            {
                return VmValue.FromLong(fastResult);
            }

            return VmValue.FromInt((int)fastResult);
        }

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsDouble + b.AsDouble);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsInt + b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return VmValue.FromDouble(a.AsDouble + b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return VmValue.FromLong(a.AsLong + b.AsLong);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return VmValue.FromLong((long)a.AsInt + b.AsLong);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return VmValue.FromLong(a.AsLong + (long)b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsLong + b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Long)
            return VmValue.FromDouble(a.AsDouble + b.AsLong);

        return VmValue.FromObject(AddSlow(a.ToObject(), b.ToObject()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? AddSlow(object? a, object? b)
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
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行加法: {ex.Message}");
            }
        }

        // 检查 b 是否是 LangValueType（处理基本类型 + LangValueType 的情况）
        if (b is LangValueType langValueB)
        {
            var aValue = ConvertToLangValueType(a);
            try
            {
                return aValue.Plus(langValueB);
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行加法: {ex.Message}");
            }
        }

        // 字符串 + 任意值：按语言显示规则转换（bool -> true、列表 -> [1, 2]、null -> null），
        // 与解释器/IL 一致；CLR ToString 会得到 True / System.Int32[]。
        if (a is string || b is string)
        {
            return Runtime.VMValueFormatter.ToDisplayString(a) +
                   Runtime.VMValueFormatter.ToDisplayString(b);
        }

        // 数组拼接：arr + [item] 或 [item] + arr
        if (a is object[] arrA && b is object[] arrB)
        {
            var result = new object[arrA.Length + arrB.Length];
            Array.Copy(arrA, 0, result, 0, arrA.Length);
            Array.Copy(arrB, 0, result, arrA.Length, arrB.Length);
            return result;
        }

        // 列表拼接：VM 的列表字面量是原生 List<object?>（与解释器 ListLangValue 对应）。
        if (a is List<object?> listA && b is List<object?> listB)
        {
            var result = new List<object?>(listA.Count + listB.Count);
            result.AddRange(listA);
            result.AddRange(listB);
            return result;
        }

        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行加法");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue Sub(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return VmValue.FromInt(a.AsInt - b.AsInt);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsDouble - b.AsDouble);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsInt - b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return VmValue.FromDouble(a.AsDouble - b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return VmValue.FromLong(a.AsLong - b.AsLong);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return VmValue.FromLong((long)a.AsInt - b.AsLong);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return VmValue.FromLong(a.AsLong - (long)b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsLong - b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Long)
            return VmValue.FromDouble(a.AsDouble - b.AsLong);

        return VmValue.FromObject(SubSlow(a.ToObject(), b.ToObject()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? SubSlow(object? a, object? b)
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
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行减法: {ex.Message}");
            }
        }

        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行减法");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue Mul(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
        {
            long fastResult = (long)a.AsInt * (long)b.AsInt;
            if (fastResult > int.MaxValue || fastResult < int.MinValue)
            {
                return VmValue.FromLong(fastResult);
            }

            return VmValue.FromInt((int)fastResult);
        }

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsDouble * b.AsDouble);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsInt * b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return VmValue.FromDouble(a.AsDouble * b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return VmValue.FromLong(a.AsLong * b.AsLong);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return VmValue.FromLong((long)a.AsInt * b.AsLong);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return VmValue.FromLong(a.AsLong * (long)b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsLong * b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Long)
            return VmValue.FromDouble(a.AsDouble * b.AsLong);

        return VmValue.FromObject(MulSlow(a.ToObject(), b.ToObject()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? MulSlow(object? a, object? b)
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
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行乘法: {ex.Message}");
            }
        }

        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行乘法");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue Div(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return VmValue.FromInt(a.AsInt / b.AsInt);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsDouble / b.AsDouble);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsInt / b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Int)
            return VmValue.FromDouble(a.AsDouble / b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Long)
            return VmValue.FromLong(a.AsLong / b.AsLong);

        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Long)
            return VmValue.FromLong((long)a.AsInt / b.AsLong);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Int)
            return VmValue.FromLong(a.AsLong / (long)b.AsInt);

        if (a.Kind == VmValueKind.Long && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsLong / b.AsDouble);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Long)
            return VmValue.FromDouble(a.AsDouble / b.AsLong);

        return VmValue.FromObject(DivSlow(a.ToObject(), b.ToObject()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? DivSlow(object? a, object? b)
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
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行除法: {ex.Message}");
            }
        }

        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行除法");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue Mod(VmValue a, VmValue b)
    {
        if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)
            return VmValue.FromInt(a.AsInt % b.AsInt);

        if (a.Kind == VmValueKind.Double && b.Kind == VmValueKind.Double)
            return VmValue.FromDouble(a.AsDouble % b.AsDouble);

        return VmValue.FromObject(ModSlow(a.ToObject(), b.ToObject()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? ModSlow(object? a, object? b)
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
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行取模: {ex.Message}");
            }
        }

        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行取模");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue Pow(VmValue a, VmValue b)
    {
        if (a.Kind is VmValueKind.Int or VmValueKind.Double or VmValueKind.Long &&
            b.Kind is VmValueKind.Int or VmValueKind.Double or VmValueKind.Long)
        {
            return VmValue.FromDouble(Math.Pow(a.ToDoubleValue(), b.ToDoubleValue()));
        }

        return VmValue.FromObject(PowSlow(a.ToObject(), b.ToObject()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object PowSlow(object? a, object? b)
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
                throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a)} 和 {DescribeValueType(b)} 执行幂运算: {ex.Message}");
            }
        }

        double da = ToDouble(a);
        double db = ToDouble(b);
        return Math.Pow(da, db);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VmValue Neg(VmValue a)
    {
        if (a.Kind == VmValueKind.Int) return VmValue.FromInt(-a.AsInt);
        if (a.Kind == VmValueKind.Double) return VmValue.FromDouble(-a.AsDouble);
        if (a.Kind == VmValueKind.Long) return VmValue.FromLong(-a.AsLong);
        throw new InvalidOperationError(new SourcePosition(), $"无法对类型 {DescribeValueType(a.ToObject())} 执行取反");
    }
}
