using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Concat(otherList) - 连接两个列表，返回包含所有元素的新列表
/// </summary>
/// <remarks>
/// 用法: list.Concat(otherList)
/// 返回: 包含两个列表所有元素的新列表
/// </remarks>
public sealed class ListConcatFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Concat"];
    public override string[]? ParameterNames => ["list", "otherList"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var otherList = (ListLangValue)results[1];

        var result = new List<LangValueType>(list.Values);
        result.AddRange(otherList.Values);
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存第一个列表
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 创建新列表副本: new List<object>(originalList)
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var listCtor = typeof(List<object>).GetConstructor([typeof(IEnumerable<object>)])!;
        ilGenerator.Emit(OpCodes.Newobj, listCtor);

        // 保存新列表
        var newListLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, newListLocal);

        // 加载 otherList
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 AddRange
        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
        // 重新加载 otherList
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Pop);

        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
        parameters[1].LoadIlValue(ilGenerator, local);
        var addRangeMethod = typeof(List<object>).GetMethod("AddRange", [typeof(IEnumerable<object>)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addRangeMethod);

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
        var otherList = (List<object>)arguments[1]!;
        var newList = new List<object>(list);
        newList.AddRange(otherList);
        return newList;
    }
}
