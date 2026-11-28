using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL 模式下静态类型为 <c>object</c> 的操作数的运行期算术运算助手。
/// </summary>
/// <remarks>
/// 未注解的函数参数、类方法参数或动态集合元素在 IL 中静态类型为 <c>object</c>。
/// 此前直接按 <c>int</c> 拆箱会导致 <c>double</c> / <c>string</c> 等实参抛 <see cref="InvalidCastException"/>；
/// 这里按语言值语义分派并还原为原生值（<c>int</c> / <c>double</c> / <c>string</c> 等）。
/// </remarks>
public static class IlDynamicArithmetic
{
    public static object? Add(object? left, object? right)
    {
        if (left is int li && right is int ri)
        {
            try
            {
                return checked(li + ri);
            }
            catch (OverflowException)
            {
                return (double)li + ri;
            }
        }

        if ((left is int or double) && (right is int or double))
        {
            return Convert.ToDouble(left) + Convert.ToDouble(right);
        }

        return IlValueBridge.Unwrap(IlValueBridge.Wrap(left).Plus(IlValueBridge.Wrap(right)));
    }

    public static object? Subtract(object? left, object? right)
    {
        if (left is int li && right is int ri)
        {
            try
            {
                return checked(li - ri);
            }
            catch (OverflowException)
            {
                return (double)li - ri;
            }
        }

        if ((left is int or double) && (right is int or double))
        {
            return Convert.ToDouble(left) - Convert.ToDouble(right);
        }

        return IlValueBridge.Unwrap(IlValueBridge.Wrap(left).Minus(IlValueBridge.Wrap(right)));
    }

    public static object? Multiply(object? left, object? right)
    {
        if (left is int li && right is int ri)
        {
            try
            {
                return checked(li * ri);
            }
            catch (OverflowException)
            {
                return (double)li * ri;
            }
        }

        if ((left is int or double) && (right is int or double))
        {
            return Convert.ToDouble(left) * Convert.ToDouble(right);
        }

        return IlValueBridge.Unwrap(IlValueBridge.Wrap(left).Times(IlValueBridge.Wrap(right)));
    }

    public static object? Divide(object? left, object? right)
    {
        if (left is int li && right is int ri)
        {
            if (ri == 0)
            {
                throw new ZeroDivisionError(new SourcePosition());
            }

            return li / ri;
        }

        if ((left is int or double) && (right is int or double))
        {
            var rd = Convert.ToDouble(right);
            if (Math.Abs(rd) < 1e-12)
            {
                throw new ZeroDivisionError(new SourcePosition());
            }

            return Convert.ToDouble(left) / rd;
        }

        return IlValueBridge.Unwrap(IlValueBridge.Wrap(left).Divide(IlValueBridge.Wrap(right)));
    }

    public static object? Modulo(object? left, object? right)
    {
        if (left is int li && right is int ri)
        {
            if (ri == 0)
            {
                throw new ZeroDivisionError(new SourcePosition());
            }

            return li % ri;
        }

        if ((left is int or double) && (right is int or double))
        {
            var rd = Convert.ToDouble(right);
            if (Math.Abs(rd) < 1e-12)
            {
                throw new ZeroDivisionError(new SourcePosition());
            }

            return Convert.ToDouble(left) % rd;
        }

        return IlValueBridge.Unwrap(IlValueBridge.Wrap(left).Mod(IlValueBridge.Wrap(right)));
    }

    public static object? Power(object? left, object? right)
    {
        if (left is int li && right is int ri)
        {
            var res = Math.Pow(li, ri);
            if (ri >= 0 && res is >= int.MinValue and <= int.MaxValue && Math.Floor(res) == res)
            {
                return (int)res;
            }

            return res;
        }

        if ((left is int or double) && (right is int or double))
        {
            return Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right));
        }

        return IlValueBridge.Unwrap(IlValueBridge.Wrap(left).Power(IlValueBridge.Wrap(right)));
    }
}
