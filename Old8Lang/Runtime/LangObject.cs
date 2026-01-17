namespace Old8Lang.Runtime;

/// <summary>
/// Old8Lang 编译器模式下所有自定义类的基类
/// 提供运算符重载的基础支持
/// </summary>
public abstract class LangObject
{
    #region 算术运算符重载

    /// <summary>
    /// 加法运算符重载 (_add)
    /// </summary>
    public virtual object? _add(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持加法操作（未定义 _add 方法）");
    }

    /// <summary>
    /// 减法运算符重载 (_sub)
    /// </summary>
    public virtual object? _sub(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持减法操作（未定义 _sub 方法）");
    }

    /// <summary>
    /// 乘法运算符重载 (_mul)
    /// </summary>
    public virtual object? _mul(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持乘法操作（未定义 _mul 方法）");
    }

    /// <summary>
    /// 除法运算符重载 (_div)
    /// </summary>
    public virtual object? _div(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持除法操作（未定义 _div 方法）");
    }

    /// <summary>
    /// 取模运算符重载 (_mod)
    /// </summary>
    public virtual object? _mod(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持取模操作（未定义 _mod 方法）");
    }

    /// <summary>
    /// 幂运算符重载 (_pow)
    /// </summary>
    public virtual object? _pow(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持幂运算（未定义 _pow 方法）");
    }

    #endregion

    #region 比较运算符重载

    /// <summary>
    /// 相等比较运算符重载 (_eq)
    /// </summary>
    public virtual bool _eq(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持相等比较（未定义 _eq 方法）");
    }

    /// <summary>
    /// 小于比较运算符重载 (_lt)
    /// </summary>
    public virtual bool _lt(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持小于比较（未定义 _lt 方法）");
    }

    /// <summary>
    /// 大于比较运算符重载 (_gt)
    /// </summary>
    public virtual bool _gt(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持大于比较（未定义 _gt 方法）");
    }

    /// <summary>
    /// 小于等于比较运算符重载 (_le)
    /// </summary>
    public virtual bool _le(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持小于等于比较（未定义 _le 方法）");
    }

    /// <summary>
    /// 大于等于比较运算符重载 (_ge)
    /// </summary>
    public virtual bool _ge(object? other)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持大于等于比较（未定义 _ge 方法）");
    }

    #endregion

    #region 索引运算符重载

    /// <summary>
    /// 获取索引运算符重载 (_getitem)
    /// </summary>
    public virtual object? _getitem(object? index)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持索引访问（未定义 _getitem 方法）");
    }

    /// <summary>
    /// 设置索引运算符重载 (_setitem)
    /// </summary>
    public virtual void _setitem(object? index, object? value)
    {
        throw new InvalidOperationException($"类型 '{GetType().Name}' 不支持索引赋值（未定义 _setitem 方法）");
    }

    #endregion
}
