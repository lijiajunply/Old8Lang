using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Reverse() - 反转列表元素顺序
/// </summary>
/// <remarks>
/// 用法: list.Reverse()
/// 返回: 反转后的新列表
/// </remarks>
public sealed class ListReverseFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Reverse"];
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

        var reversedValues = new List<LangValueType>(list.Values);
        reversedValues.Reverse();
        return new ListLangValue(reversedValues);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 创建新列表
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 创建新列表副本: new List<object>(originalList)
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var listCtor = typeof(List<object>).GetConstructor([typeof(IEnumerable<object>)])!;
        ilGenerator.Emit(OpCodes.Newobj, listCtor);

        // 保存新列表
        var newListLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, newListLocal);

        // 调用 Reverse()
        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
        var reverseMethod = typeof(List<object>).GetMethod("Reverse", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, reverseMethod);

        // 返回新列表
        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object>);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var newList = new List<object>(list);
        newList.Reverse();
        return newList;
    }
}
