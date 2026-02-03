using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.RemoveAt(index) - 根据索引从列表中移除元素
/// </summary>
/// <remarks>
/// 用法: list.RemoveAt(index)
/// 返回: 被移除的元素
/// </remarks>
public sealed class ListRemoveAtFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.RemoveAt"];
    public override string[]? ParameterNames => ["list", "index"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var index = ((IntLangValue)results[1]).Value;

        var removed = list.Values[index];
        list.Values.RemoveAt(index);
        return removed;
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

        // 加载 index
        parameters[1].LoadIlValue(ilGenerator, local);

        // 保存 index 到局部变量
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        // 先获取要移除的元素: list[index]
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
        var index = (int)arguments[1]!;
        var removed = list[index];
        list.RemoveAt(index);
        return removed;
    }
}
