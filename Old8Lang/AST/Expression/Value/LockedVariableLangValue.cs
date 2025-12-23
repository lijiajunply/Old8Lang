using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 锁定变量类型，提供线程安全的变量访问
/// </summary>
/// <remarks>
/// 使用 lock(variable) 创建，可以在异步函数和线程中安全地访问和修改
/// 通过 .Value 属性读取内部值，通过 .Set(newValue) 方法设置内部值
/// 所有访问自动加锁，保证线程安全
///
/// 示例用法：
/// <code>
/// counter &lt;- 0
/// lockedCounter &lt;- lock(counter)
///
/// // 在异步函数中使用
/// async func increment() {
///     value &lt;- lockedCounter.Value  // 读取
///     lockedCounter.Set(value + 1)  // 写入
/// }
/// </code>
/// </remarks>
public class LockedVariableLangValue : LangValueType
{
    /// <summary>
    /// 用于同步访问的锁对象
    /// </summary>
    private readonly Lock _lock = new();

    /// <summary>
    /// 内部存储的值
    /// </summary>
    private LangValueType _value;

    /// <summary>
    /// 变量名（用于错误报告和调试）
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// 创建锁定变量
    /// </summary>
    /// <param name="value">要锁定的值</param>
    /// <param name="variableName">变量名</param>
    /// <param name="position">源代码位置</param>
    public LockedVariableLangValue(LangValueType value, string variableName, SourcePosition position = default)
        : base(position)
    {
        _value = value;
        VariableName = variableName;
    }

    /// <summary>
    /// 获取内部值（线程安全）- 用于内部操作
    /// </summary>
    public LangValueType GetLockedValue()
    {
        lock (_lock)
        {
            return _value;
        }
    }

    /// <summary>
    /// 设置内部值（线程安全）
    /// </summary>
    public void SetValue(LangValueType value)
    {
        lock (_lock)
        {
            _value = value;
        }
    }

    /// <summary>
    /// 点操作支持，处理 .Value 属性访问
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            // 支持 .Value 属性读取
            if (id.IdName == "Value")
            {
                return GetLockedValue();
            }

            // 其他属性转发到内部值
            var innerValue = GetLockedValue();
            return innerValue.Dot(dotExpression, manager);
        }

        if (dotExpression is Instance instance)
        {
            // 处理 .Set() 方法 - 直接设置值
            if (instance.Id.IdName == "Set" && instance.Ids.Count == 1)
            {
                var newValue = instance.Ids[0].Run(manager);
                SetValue(newValue);
                return new VoidLangValue();
            }

            // 处理 .Get() 方法 - 获取值
            if (instance.Id.IdName == "Get" && instance.Ids.Count == 0)
            {
                return GetLockedValue();
            }

            // 处理 .Update() 方法 - 原子更新：接受一个函数，在锁内执行
            if (instance.Id.IdName == "Update" && instance.Ids.Count == 1)
            {
                var funcExpr = instance.Ids[0].Run(manager);
                if (funcExpr is not FuncLangValue func)
                {
                    throw new TypeError(this, "Function", funcExpr.GetType().Name);
                }

                lock (_lock)
                {
                    // 在锁内调用函数，传入当前值
                    var currentValue = _value;
                    var newValue = func.Run(manager, [currentValue], null);
                    _value = newValue;
                    return newValue;
                }
            }

            // 处理 .Increment() 方法 - 原子递增
            if (instance.Id.IdName == "Increment" && instance.Ids.Count == 0)
            {
                lock (_lock)
                {
                    if (_value is not IntLangValue intValue)
                    {
                        throw new TypeError(this, "Int", _value.GetType().Name);
                    }
                    var newValue = IntLangValue.Create(intValue.Value + 1);
                    _value = newValue;
                    return newValue;
                }
            }

            // 处理 .Decrement() 方法 - 原子递减
            if (instance.Id.IdName == "Decrement" && instance.Ids.Count == 0)
            {
                lock (_lock)
                {
                    if (_value is not IntLangValue intValue)
                    {
                        throw new TypeError(this, "Int", _value.GetType().Name);
                    }
                    var newValue = IntLangValue.Create(intValue.Value - 1);
                    _value = newValue;
                    return newValue;
                }
            }

            // 处理 .Add() 方法 - 原子加法
            if (instance.Id.IdName == "Add" && instance.Ids.Count == 1)
            {
                var deltaExpr = instance.Ids[0].Run(manager);
                lock (_lock)
                {
                    var newValue = _value.Plus(deltaExpr);
                    _value = newValue;
                    return newValue;
                }
            }

            // 其他方法转发到内部值
            var innerValue = GetLockedValue();
            return innerValue.Dot(dotExpression, manager);
        }

        return base.Dot(dotExpression, manager);
    }

    public override string ToString()
    {
        lock (_lock)
        {
            return $"LockedVariable({VariableName}: {_value})";
        }
    }

    public override string TypeToString() => "LockedVariable";

    public override object GetValue() => _value;

    /// <summary>
    /// 相等比较（线程安全）
    /// </summary>
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is LockedVariableLangValue otherLocked)
        {
            return GetLockedValue().Equal(otherLocked.GetLockedValue());
        }

        return GetLockedValue().Equal(otherValueType);
    }

    /// <summary>
    /// 实现 Visitor 模式的 Accept 方法
    /// </summary>
    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        // 锁定变量目前不支持 Visitor 模式遍历
        // 如果需要，可以在 visitor 中添加 VisitLockedVariable 方法
        throw new NotSupportedException("LockedVariable 目前不支持 Visitor 模式");
    }
}
