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
/// List.Insert(index, element) - 在指定索引处插入元素
/// </summary>
/// <remarks>
/// 用法: list.Insert(index, element)
/// 返回: void
/// </remarks>
public sealed class ListInsertFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Insert"];
    public override string[]? ParameterNames => ["list", "index", "element"];
    public override int MinParameterCount => 3;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var index = ((IntLangValue)results[1]).Value;
        var element = results[2];

        var insertIndex = Math.Max(0, Math.Min(index, list.Values.Count));
        list.Values.Insert(insertIndex, element);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 index
        parameters[1].LoadIlValue(ilGenerator, local);

        // 加载 element
        parameters[2].LoadIlValue(ilGenerator, local);
        var elementType = parameters[2].OutputType(local);

        // 如果元素是值类型，需要装箱
        if (elementType is not null && elementType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, elementType);
        }

        // 调用 List<object>.Insert(int, object)
        var insertMethod = typeof(List<object>).GetMethod("Insert", [typeof(int), typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, insertMethod);

        // Insert 返回 void，加载 null 作为返回值
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var index = (int)arguments[1]!;
        var element = arguments[2];
        var insertIndex = Math.Max(0, Math.Min(index, list.Count));
        list.Insert(insertIndex, element!);
        return null;
    }
}
