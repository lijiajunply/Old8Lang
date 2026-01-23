using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 算术运算
/// </summary>
public partial class VirtualMachine
{
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


}
