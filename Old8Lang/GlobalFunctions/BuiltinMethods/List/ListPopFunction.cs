using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Pop() - 移除并返回列表的最后一个元素
/// </summary>
/// <remarks>
/// 用法: list.Pop()
/// 返回: 被移除的最后一个元素
/// 异常: 当列表为空时抛出 InvalidOperationError
/// </remarks>
public sealed class ListPopFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Pop"];
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

        if (list.Values.Count == 0)
        {
            throw new InvalidOperationError(list, "无法从空列表中移除元素");
        }

        var lastIndex = list.Values.Count - 1;
        var lastElement = list.Values[lastIndex];
        list.Values.RemoveAt(lastIndex);
        return lastElement;
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存 list 到局部变量
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 获取 Count - 1 作为最后一个索引
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);

        // 保存 index 到局部变量
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        // 获取最后一个元素: list[index]
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);

        // 保存元素到局部变量
        var elementLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, elementLocal);

        // 调用 RemoveAt
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        var removeAtMethod = typeof(List<object>).GetMethod("RemoveAt", [typeof(int)])!;
        ilGenerator.Emit(OpCodes.Callvirt, removeAtMethod);

        // 返回被移除的元素
        ilGenerator.Emit(OpCodes.Ldloc, elementLocal);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        if (list.Count == 0)
        {
            throw new InvalidOperationException("无法从空列表中移除元素");
        }

        var lastIndex = list.Count - 1;
        var lastElement = list[lastIndex];
        list.RemoveAt(lastIndex);
        return lastElement;
    }
}
