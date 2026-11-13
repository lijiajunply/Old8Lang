using Old8Lang.AST.Expression;
using Old8Lang.InstanceMethods.Core;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType 通用实例方法基类
/// 所有 LangValueType 类型都可以使用这些方法
/// </summary>
public abstract class BaseValueTypeMethod : BaseInstanceMethod
{
    /// <summary>
    /// 目标类型为 LangValueType 基类
    /// </summary>
    public override Type TargetType => typeof(LangValueType);

    /// <summary>
    /// 将 VM 模式下的 C# 原始类型转换为 LangValueType
    /// </summary>
    protected LangValueType ConvertToLangValueType(object? value)
    {
        return LangValueType.ObjToValue(value);
    }
}
