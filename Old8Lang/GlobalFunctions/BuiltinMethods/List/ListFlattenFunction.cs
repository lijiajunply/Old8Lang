using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Flatten() - 展平嵌套列表（一层）
/// </summary>
/// <remarks>
/// 用法: list.Flatten()
/// 返回: 展平后的列表
/// </remarks>
public sealed class ListFlattenFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Flatten"];
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

        var result = new List<LangValueType>();

        foreach (var item in list.Values)
        {
            if (item is ListLangValue innerList)
            {
                result.AddRange(innerList.Values);
            }
            else if (item is ArrayLangValue innerArray)
            {
                result.AddRange(innerArray.RunResult);
            }
            else
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
        // 保存 list
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 创建结果列表
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 索引变量
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var addSingleItem = ilGenerator.DefineLabel();
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

        // 检查是否是 List<object>
        ilGenerator.Emit(OpCodes.Ldloc, itemLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(List<object>));
        ilGenerator.Emit(OpCodes.Brfalse, addSingleItem);

        // 是列表，添加所有元素
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, itemLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(List<object>));
        var addRangeMethod = typeof(List<object>).GetMethod("AddRange", [typeof(IEnumerable<object>)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addRangeMethod);
        ilGenerator.Emit(OpCodes.Br, continueLabel);

        // 不是列表，添加单个元素
        ilGenerator.MarkLabel(addSingleItem);
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
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

        // 返回结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object>);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;

        var result = new List<object>();

        foreach (var item in list)
        {
            if (item is List<object> innerList)
            {
                result.AddRange(innerList);
            }
            else if (item is object[] innerArray)
            {
                result.AddRange(innerArray);
            }
            else
            {
                result.Add(item!);
            }
        }

        return result;
    }
}
