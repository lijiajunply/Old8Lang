using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 字符
/// </summary>
/// <param name="value"></param>
/// <param name="position"></param>
public class CharLangValue(char value = default, SourcePosition position = default) : LangValueType(position), IPoolable
{
    public char Value = value;

    public override LangValueType Plus(LangValueType otherLangValueType) =>
        StringLangValue.Create(Value + (string)otherLangValueType.GetValue());

    public override LangValueType Times(LangValueType otherLangValueType)
    {
        return StringLangValue.Create(Value + otherLangValueType.ToString());
    }

    public override string ToString() => Value.ToString();

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is CharLangValue b)
            return Value == b.Value;
        return false;
    }

    public override bool Greater(LangValueType? otherValue)
    {
        if (otherValue is CharLangValue c)
            return Value > c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool Less(LangValueType? otherValue)
    {
        if (otherValue is CharLangValue c)
            return Value < c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool GreaterEqual(LangValueType? otherValue)
    {
        if (otherValue is CharLangValue c)
            return Value >= c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override bool LessEqual(LangValueType? otherValue)
    {
        if (otherValue is CharLangValue c)
            return Value <= c.Value;
        throw new InvalidOperationError(this, $"不支持与 {otherValue?.TypeToString()} 类型进行比较");
    }

    public override object GetValue() => Value;

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => IntLangValue.Create(Convert.ToInt32(Value)),
            "Bool" or "bool" => throw new TypeError(this, "bool", "无法将字符转换为布尔值"),
            "String" or "string" => StringLangValue.Create(Value.ToString()),
            "char" or "Char" => this,
            "Double" or "double" => DoubleLangValue.Create(Convert.ToDouble(Value)),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, Convert.ToInt32(Value));
    }

    public override Type OutputType(LocalManager local) => Value.GetType();

    /// <summary>
    /// 重置对象状态，使其可以被复用
    /// </summary>
    public void Reset()
    {
        Value = '\0';
        // Position是只读属性，无法修改
    }

    /// <summary>
    /// 从对象池获取CharLangValue实例
    /// </summary>
    /// <param name="value">字符值</param>
    /// <param name="position">源码位置</param>
    /// <returns>CharLangValue实例</returns>
    public static CharLangValue Create(char value, SourcePosition position = default)
    {
        var instance = ObjectPoolManager.Instance.CharPool.Get();
        instance.Value = value;
        instance.Position = position;
        return instance;
    }

    /// <summary>
    /// 将实例归还到对象池
    /// </summary>
    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.CharPool.Return(this);
    }
}