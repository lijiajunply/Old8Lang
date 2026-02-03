using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Distinct() - 移除列表中的重复元素
/// </summary>
/// <remarks>
/// 用法: list.Distinct()
/// 返回: 包含去重后元素的新列表
/// </remarks>
public sealed class ListDistinctFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Distinct"];
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

        var distinct = new List<LangValueType>();
        var seen = new HashSet<string>();

        foreach (var item in list.Values)
        {
            var key = item.ToDisplayString();
            if (seen.Add(key))
            {
                distinct.Add(item);
            }
        }

        return new ListLangValue(distinct);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存原列表
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 创建新列表
        var newListLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, newListLocal);

        // 创建 HashSet<string>
        var seenLocal = ilGenerator.DeclareLocal(typeof(HashSet<string>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(HashSet<string>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, seenLocal);

        // 循环遍历原列表
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(loopStart);

        // 检查 index < count
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Bge, loopEnd);

        // 获取当前元素
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        var itemLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, itemLocal);

        // 获取 key = item.ToString()
        ilGenerator.Emit(OpCodes.Ldloc, itemLocal);
        var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
        var keyLocal = ilGenerator.DeclareLocal(typeof(string));
        ilGenerator.Emit(OpCodes.Stloc, keyLocal);

        // 检查 seen.Add(key) - 如果返回 true，则添加到新列表
        ilGenerator.Emit(OpCodes.Ldloc, seenLocal);
        ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
        var addMethod = typeof(HashSet<string>).GetMethod("Add", [typeof(string)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        ilGenerator.Emit(OpCodes.Brfalse, continueLabel);

        // 添加到新列表
        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
        ilGenerator.Emit(OpCodes.Ldloc, itemLocal);
        var listAddMethod = typeof(List<object>).GetMethod("Add", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, listAddMethod);

        ilGenerator.MarkLabel(continueLabel);

        // index++
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(loopEnd);

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
        var distinct = new List<object>();
        var seen = new HashSet<string>();

        foreach (var item in list)
        {
            var key = item?.ToString() ?? "";
            if (seen.Add(key))
            {
                distinct.Add(item!);
            }
        }

        return distinct;
    }
}
