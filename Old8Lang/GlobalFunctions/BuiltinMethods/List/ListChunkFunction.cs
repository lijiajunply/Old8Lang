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
/// List.Chunk(size) - 将列表分成指定大小的块
/// </summary>
/// <remarks>
/// 用法: list.Chunk(size)
/// 返回: 包含块的列表
/// 异常: 当 size <= 0 时抛出
/// </remarks>
public sealed class ListChunkFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Chunk"];
    public override string[]? ParameterNames => ["list", "size"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var size = ((IntLangValue)results[1]).Value;

        if (size <= 0)
        {
            throw new InvalidOperationError(list, "块大小必须大于0");
        }

        var result = new List<LangValueType>();
        for (int i = 0; i < list.Values.Count; i += size)
        {
            var chunk = list.Values.Skip(i).Take(size).ToList();
            result.Add(new ListLangValue(chunk));
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

        // 加载 size
        parameters[1].LoadIlValue(ilGenerator, local);
        var sizeLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, sizeLocal);

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

        ilGenerator.MarkLabel(loopStart);

        // 检查 index < count
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Bge, loopEnd);

        // 计算当前块的大小
        // chunkSize = Math.Min(size, count - index)
        ilGenerator.Emit(OpCodes.Ldloc, sizeLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Sub);
        var mathMinMethod = typeof(Math).GetMethod("Min", [typeof(int), typeof(int)])!;
        ilGenerator.Emit(OpCodes.Call, mathMinMethod);
        var chunkSizeLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, chunkSizeLocal);

        // 获取块: list.GetRange(index, chunkSize)
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, chunkSizeLocal);
        var getRangeMethod = typeof(List<object>).GetMethod("GetRange", [typeof(int), typeof(int)])!;
        ilGenerator.Emit(OpCodes.Callvirt, getRangeMethod);

        // 添加到结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Pop);

        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, chunkSizeLocal);
        ilGenerator.Emit(OpCodes.Callvirt, getRangeMethod);
        var listAddMethod = typeof(List<object>).GetMethod("Add", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, listAddMethod);

        // index += size
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, sizeLocal);
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
        var size = (int)arguments[1]!;

        if (size <= 0)
        {
            throw new InvalidOperationException("块大小必须大于0");
        }

        var result = new List<object>();
        for (int i = 0; i < list.Count; i += size)
        {
            var chunk = list.GetRange(i, Math.Min(size, list.Count - i));
            result.Add(chunk);
        }

        return result;
    }
}
