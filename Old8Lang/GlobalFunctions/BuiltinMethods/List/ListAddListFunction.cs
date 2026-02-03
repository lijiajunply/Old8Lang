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
/// List.AddList(otherList) - 将另一个列表的所有元素添加到当前列表
/// </summary>
/// <remarks>
/// 用法: list.AddList(otherList)
/// 返回: void
/// </remarks>
public sealed class ListAddListFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.AddList"];
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

        list.Values.AddRange(otherList.Values);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 otherList
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 List<object>.AddRange(IEnumerable<object>)
        var addRangeMethod = typeof(List<object>).GetMethod("AddRange", [typeof(IEnumerable<object>)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addRangeMethod);

        // AddRange 返回 void，加载 null 作为返回值
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var otherList = (List<object>)arguments[1]!;
        list.AddRange(otherList);
        return null;
    }
}
