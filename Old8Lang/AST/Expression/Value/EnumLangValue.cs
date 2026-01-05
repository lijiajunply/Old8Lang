using Old8Lang.AST.Visitor;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 枚举值类型，用于表示枚举成员的值，同时携带枚举类型信息
/// </summary>
/// <param name="enumTypeName">枚举类型名称</param>
/// <param name="memberName">枚举成员名称</param>
/// <param name="value">枚举成员的整数值</param>
/// <param name="position">源代码位置信息</param>
public partial class EnumLangValue(
    string enumTypeName,
    string memberName,
    int value,
    SourcePosition position = default) : LangValueType(position)
{
    /// <summary>
    /// 枚举类型名称
    /// </summary>
    public readonly string EnumTypeName = enumTypeName;

    /// <summary>
    /// 枚举成员名称
    /// </summary>
    public readonly string MemberName = memberName;

    /// <summary>
    /// 枚举成员的整数值
    /// </summary>
    public readonly int Value = value;

    /// <summary>
    /// 创建枚举值实例
    /// </summary>
    public static EnumLangValue Create(string enumTypeName, string memberName, int value, SourcePosition position = default)
    {
        return new EnumLangValue(enumTypeName, memberName, value, position);
    }

    public override string ToString() => Value.ToString();

    /// <summary>
    /// 获取枚举的完整表示（类型名.成员名）
    /// </summary>
    public string GetFullName() => $"{EnumTypeName}.{MemberName}";

    #region 算术运算（委托给底层整数值）

    public override LangValueType Plus(LangValueType otherLangValueType)
    {
        // 枚举值的加法运算：转换为整数后运算
        var intValue = IntLangValue.Create(Value);
        return intValue.Plus(otherLangValueType);
    }

    public override LangValueType Minus(LangValueType otherLangValueType)
    {
        // 枚举值的减法运算：转换为整数后运算
        var intValue = IntLangValue.Create(Value);
        return intValue.Minus(otherLangValueType);
    }

    public override LangValueType Times(LangValueType otherLangValueType)
    {
        // 枚举值的乘法运算：转换为整数后运算
        var intValue = IntLangValue.Create(Value);
        return intValue.Times(otherLangValueType);
    }

    public override LangValueType Divide(LangValueType otherLangValueType)
    {
        // 枚举值的除法运算：转换为整数后运算
        var intValue = IntLangValue.Create(Value);
        return intValue.Divide(otherLangValueType);
    }

    public override LangValueType Mod(LangValueType otherLangValueType)
    {
        // 枚举值的取模运算：转换为整数后运算
        var intValue = IntLangValue.Create(Value);
        return intValue.Mod(otherLangValueType);
    }

    public override LangValueType Power(LangValueType otherLangValueType)
    {
        // 枚举值的幂运算：转换为整数后运算
        var intValue = IntLangValue.Create(Value);
        return intValue.Power(otherLangValueType);
    }

    #endregion

    #region 比较运算

    public override bool Equal(LangValueType? otherLangValueType)
    {
        if (otherLangValueType == null)
            return false;

        // 枚举值相等性判断
        if (otherLangValueType is EnumLangValue otherEnum)
        {
            // 同类型枚举：比较值
            if (EnumTypeName == otherEnum.EnumTypeName)
            {
                return Value == otherEnum.Value;
            }
            // 不同类型枚举：不相等
            return false;
        }

        if (otherLangValueType is IntLangValue intValue)
        {
            // 与整数比较：比较值
            return Value == intValue.Value;
        }

        return false;
    }

    public override bool Less(LangValueType? otherLangValueType)
    {
        if (otherLangValueType == null)
            throw new InvalidOperationError(this, "不能与 null 进行比较");

        if (otherLangValueType is EnumLangValue otherEnum)
        {
            // 只有同类型枚举才能比较大小
            if (EnumTypeName == otherEnum.EnumTypeName)
            {
                return Value < otherEnum.Value;
            }
            throw new InvalidOperationError(this, $"不能比较不同枚举类型 '{EnumTypeName}' 和 '{otherEnum.EnumTypeName}'");
        }

        if (otherLangValueType is IntLangValue intValue)
        {
            return Value < intValue.Value;
        }

        throw new InvalidOperationError(this, $"不支持枚举与类型 '{otherLangValueType.GetType().Name}' 的比较操作");
    }

    public override bool LessEqual(LangValueType? otherLangValueType)
    {
        if (otherLangValueType == null)
            throw new InvalidOperationError(this, "不能与 null 进行比较");

        if (otherLangValueType is EnumLangValue otherEnum)
        {
            if (EnumTypeName == otherEnum.EnumTypeName)
            {
                return Value <= otherEnum.Value;
            }
            throw new InvalidOperationError(this, $"不能比较不同枚举类型 '{EnumTypeName}' 和 '{otherEnum.EnumTypeName}'");
        }

        if (otherLangValueType is IntLangValue intValue)
        {
            return Value <= intValue.Value;
        }

        throw new InvalidOperationError(this, $"不支持枚举与类型 '{otherLangValueType.GetType().Name}' 的比较操作");
    }

    public override bool Greater(LangValueType? otherLangValueType)
    {
        if (otherLangValueType == null)
            throw new InvalidOperationError(this, "不能与 null 进行比较");

        if (otherLangValueType is EnumLangValue otherEnum)
        {
            if (EnumTypeName == otherEnum.EnumTypeName)
            {
                return Value > otherEnum.Value;
            }
            throw new InvalidOperationError(this, $"不能比较不同枚举类型 '{EnumTypeName}' 和 '{otherEnum.EnumTypeName}'");
        }

        if (otherLangValueType is IntLangValue intValue)
        {
            return Value > intValue.Value;
        }

        throw new InvalidOperationError(this, $"不支持枚举与类型 '{otherLangValueType.GetType().Name}' 的比较操作");
    }

    public override bool GreaterEqual(LangValueType? otherLangValueType)
    {
        if (otherLangValueType == null)
            throw new InvalidOperationError(this, "不能与 null 进行比较");

        if (otherLangValueType is EnumLangValue otherEnum)
        {
            if (EnumTypeName == otherEnum.EnumTypeName)
            {
                return Value >= otherEnum.Value;
            }
            throw new InvalidOperationError(this, $"不能比较不同枚举类型 '{EnumTypeName}' 和 '{otherEnum.EnumTypeName}'");
        }

        if (otherLangValueType is IntLangValue intValue)
        {
            return Value >= intValue.Value;
        }

        throw new InvalidOperationError(this, $"不支持枚举与类型 '{otherLangValueType.GetType().Name}' 的比较操作");
    }

    #endregion
    
    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => IntLangValue.Create(Value),
            "Bool" or "bool" => throw new TypeError(this, "bool", "无法将字符转换为布尔值"),
            "String" or "string" => StringLangValue.Create(MemberName),
            "enum" or "Enum" => this,
            "double" or "Double" => DoubleLangValue.Create(Value),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }

    #region Visitor 模式

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException("EnumLangValue visitor not implemented");
    }

    #endregion
}
