using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Intersect(otherList) - 返回两个列表的交集
/// </summary>
/// <remarks>
/// 用法: list.Intersect(otherList)
/// 返回: 包含两个列表共有元素的新列表
/// </remarks>
public sealed class ListIntersectFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Intersect"];
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

        var result = new List<LangValueType>();
        var otherSet = new HashSet<string>(otherList.Values.Select(v => v.ToDisplayString()));
        var seen = new HashSet<string>();

        foreach (var item in list.Values)
        {
            var key = item.ToDisplayString();
            if (otherSet.Contains(key) && seen.Add(key))
            {
                result.Add(item);
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存第一个列表
        var list1Local = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, list1Local);

        // 加载第二个列表
        parameters[1].LoadIlValue(ilGenerator, local);
        var list2Local = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, list2Local);

        // 创建结果列表
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 创建 otherSet (HashSet<string>)
        var otherSetLocal = ilGenerator.DeclareLocal(typeof(HashSet<string>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(HashSet<string>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, otherSetLocal);

        // 填充 otherSet
        GenerateFillHashSet(ilGenerator, list2Local, otherSetLocal);

        // 创建 seen (HashSet<string>)
        var seenLocal = ilGenerator.DeclareLocal(typeof(HashSet<string>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(HashSet<string>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, seenLocal);

        // 遍历第一个列表，添加交集元素
        GenerateIntersectLoop(ilGenerator, list1Local, resultLocal, otherSetLocal, seenLocal);

        // 返回结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
    }

    private static void GenerateFillHashSet(
        ILGenerator ilGenerator,
        LocalBuilder listLocal,
        LocalBuilder setLocal)
    {
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(loopStart);

        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Bge, loopEnd);

        ilGenerator.Emit(OpCodes.Ldloc, setLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
        var addMethod = typeof(HashSet<string>).GetMethod("Add", [typeof(string)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        ilGenerator.Emit(OpCodes.Pop);

        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(loopEnd);
    }

    private static void GenerateIntersectLoop(
        ILGenerator ilGenerator,
        LocalBuilder listLocal,
        LocalBuilder resultLocal,
        LocalBuilder otherSetLocal,
        LocalBuilder seenLocal)
    {
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(loopStart);

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

        // 获取 key
        ilGenerator.Emit(OpCodes.Ldloc, itemLocal);
        var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
        var keyLocal = ilGenerator.DeclareLocal(typeof(string));
        ilGenerator.Emit(OpCodes.Stloc, keyLocal);

        // 检查 otherSet.Contains(key)
        ilGenerator.Emit(OpCodes.Ldloc, otherSetLocal);
        ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
        var containsMethod = typeof(HashSet<string>).GetMethod("Contains", [typeof(string)])!;
        ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
        ilGenerator.Emit(OpCodes.Brfalse, continueLabel);

        // 检查 seen.Add(key)
        ilGenerator.Emit(OpCodes.Ldloc, seenLocal);
        ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
        var addMethod = typeof(HashSet<string>).GetMethod("Add", [typeof(string)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        ilGenerator.Emit(OpCodes.Brfalse, continueLabel);

        // 添加到结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, itemLocal);
        var listAddMethod = typeof(List<object>).GetMethod("Add", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, listAddMethod);

        ilGenerator.MarkLabel(continueLabel);

        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(loopEnd);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object>);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list1 = (List<object>)arguments[0]!;
        var list2 = (List<object>)arguments[1]!;

        var result = new List<object>();
        var otherSet = new HashSet<string>(list2.Select(v => v?.ToString() ?? ""));
        var seen = new HashSet<string>();

        foreach (var item in list1)
        {
            var key = item?.ToString() ?? "";
            if (otherSet.Contains(key) && seen.Add(key))
            {
                result.Add(item!);
            }
        }

        return result;
    }
}
