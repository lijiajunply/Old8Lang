using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Dict 函数 - 创建空字典
/// </summary>
public sealed class DictFunction : BaseGlobalFunction
{
    public override string[] Names => ["Dict", "dict"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        return new DictionaryLangValue();
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 创建新的 DictionaryLangValue
        var dictConstructor = typeof(DictionaryLangValue).GetConstructor(Type.EmptyTypes);
        ilGenerator.Emit(OpCodes.Newobj, dictConstructor!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(DictionaryLangValue);
    }
    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return new DictionaryLangValue();
    }
}