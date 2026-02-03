using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Add(element) - 向列表中添加元素
/// </summary>
/// <remarks>
/// 用法: list.Add(value)
/// 返回: 添加的元素
/// </remarks>
public sealed class ListAddFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Add"];
    public override string[]? ParameterNames => ["list", "element"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var element = results[1];

        list.Values.Add(element);
        return element;
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 参数 0: list (已在栈上)
        // 参数 1: element

        // 加载 element
        parameters[1].LoadIlValue(ilGenerator, local);
        var elementType = parameters[1].OutputType(local);

        // 如果元素是值类型，需要装箱
        if (elementType is not null && elementType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, elementType);
        }

        // 调用 List<object>.Add(object)
        var addMethod = typeof(List<object>).GetMethod("Add", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addMethod);

        // Add 方法返回 void，但我们需要返回添加的元素
        // 重新加载 element 作为返回值
        parameters[1].LoadIlValue(ilGenerator, local);
        if (elementType is not null && elementType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, elementType);
        }
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        // 返回 object 类型，因为 IL 生成中我们装箱了元素
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var element = arguments[1];
        list.Add(element!);
        return element;
    }
}
