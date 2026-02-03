using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Clear() - 清空列表中的所有元素
/// </summary>
/// <remarks>
/// 用法: list.Clear()
/// 返回: void
/// </remarks>
public sealed class ListClearFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Clear"];
    public override string[]? ParameterNames => ["list"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];

        list.ClearInternal();
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 调用 List<object>.Clear()
        var clearMethod = typeof(List<object>).GetMethod("Clear", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, clearMethod);

        // Clear 返回 void，加载 null 作为返回值
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        list.Clear();
        return null;
    }
}
