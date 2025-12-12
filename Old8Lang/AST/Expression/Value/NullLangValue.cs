using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Null值
/// </summary>
/// <param name="position">位置</param>
public class NullLangValue(SourcePosition position = default) : LangValueType(position)
{
    public override string ToString() => "Null";
    
    public override LangValueType Run(VariateManager manager) => this;

    public override bool Equal(LangValueType? otherValueType)
    {
        return otherValueType is NullLangValue;
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Bool" or "bool" => new BoolLangValue(false),
            "String" or "string" => new StringLangValue("null"),
            "Int" or "int" => new IntLangValue(0),
            "Double" or "double" => new DoubleLangValue(0.0),
            "Char" or "char" => new CharLangValue('\0'),
            _ => throw new TypeError(this, "不支持的类型转换")
        };
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    public override Type OutputType(LocalManager local) => typeof(object);
}