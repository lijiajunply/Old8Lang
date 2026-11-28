using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToHash() - 获取值的哈希码
/// </summary>
public class ValueTypeToHashMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToHash", "toHash"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(IntLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return new IntLangValue(GetHashCodeHelper(instance));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        InstanceMethods.Core.IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(ValueTypeToHashMethod).GetMethod(nameof(GetHashCodeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static int GetHashCodeHelper(LangValueType type)
    {
        return type switch
        {
            IntLangValue i => i.Value.GetHashCode(),
            DoubleLangValue d => d.Value.GetHashCode(),
            StringLangValue s => s.Value.GetHashCode(),
            BoolLangValue b => b.Value.GetHashCode(),
            CharLangValue c => c.Value.GetHashCode(),
            NullLangValue => 0,
            _ => type.GetHashCode()
        };
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return (long)(instance?.GetHashCode() ?? 0);
    }
}
