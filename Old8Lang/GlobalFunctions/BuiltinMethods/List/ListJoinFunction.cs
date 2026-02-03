using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Join(separator) - 连接字符串列表为单个字符串
/// </summary>
/// <remarks>
/// 用法: list.Join(separator)
/// 返回: 连接后的字符串
/// </remarks>
public sealed class ListJoinFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Join"];
    public override string[]? ParameterNames => ["list", "separator"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var separator = ((StringLangValue)results[1]).Value;

        var joined = string.Join(separator, list.Values.Select(v => v.ToDisplayString()));
        return new StringLangValue(joined);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存 list
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 加载 separator
        parameters[1].LoadIlValue(ilGenerator, local);

        // 将 List<object> 转换为 string[]
        // 首先创建一个 string 数组
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Newarr, typeof(string));
        var stringArrayLocal = ilGenerator.DeclareLocal(typeof(string[]));
        ilGenerator.Emit(OpCodes.Stloc, stringArrayLocal);

        // 保存 separator
        var separatorLocal = ilGenerator.DeclareLocal(typeof(string));
        ilGenerator.Emit(OpCodes.Stloc, separatorLocal);

        // 循环填充数组
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(loopStart);

        // 检查 index < count
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Bge, loopEnd);

        // stringArray[index] = list[index].ToString()
        ilGenerator.Emit(OpCodes.Ldloc, stringArrayLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);

        // 调用 ToString()
        var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
        ilGenerator.Emit(OpCodes.Stelem_Ref);

        // index++
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(loopEnd);

        // 调用 string.Join(separator, stringArray)
        ilGenerator.Emit(OpCodes.Ldloc, separatorLocal);
        ilGenerator.Emit(OpCodes.Ldloc, stringArrayLocal);
        var joinMethod = typeof(string).GetMethod("Join", [typeof(string), typeof(string[])])!;
        ilGenerator.Emit(OpCodes.Call, joinMethod);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var separator = (string)arguments[1]!;
        return string.Join(separator, list.Select(v => v?.ToString() ?? ""));
    }
}
