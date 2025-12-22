using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// bool
/// </summary>
/// <param name="value"></param>
/// <param name="position"></param>
public partial class BoolLangValue(bool value = false, SourcePosition position = default) : LangValueType(position), IPoolable
{
    public bool Value = value;
    public override string ToString() => Value ? "true" : "false";
    public override LangValueType Run(VariateManager manager) => this;

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is BoolLangValue b)
            return Value == b.Value;
        return false;
    }

    public override object GetValue() => Value;

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => IntLangValue.Create(Value ? 1 : 0),
            "Bool" or "bool" => this,
            "String" or "string" => StringLangValue.Create(Value.ToString()),
            "char" or "Char" => CharLangValue.Create(Value ? '1' : '0'),
            "Double" or "double" => DoubleLangValue.Create(Value ? 1.0 : 0.0),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
    }

    public override Type OutputType(LocalManager local) => Value.GetType();

    /// <summary>
    /// 重置对象状态，使其可以被复用
    /// </summary>
    public void Reset()
    {
        Value = false;
        // Position是只读属性，无法修改
    }

    /// <summary>
    /// 从对象池获取BoolLangValue实例
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="position">源码位置</param>
    /// <returns>BoolLangValue实例</returns>
    public static BoolLangValue Create(bool value, SourcePosition position = default)
    {
        var instance = ObjectPoolManager.Instance.BoolPool.Get();
        instance.Value = value;
        instance.Position = position;
        return instance;
    }

    /// <summary>
    /// 将实例归还到对象池
    /// </summary>
    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.BoolPool.Return(this);
    }
}