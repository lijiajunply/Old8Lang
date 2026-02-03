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
/// List.Sum() - 计算列表中所有数值元素的和
/// </summary>
/// <remarks>
/// 用法: list.Sum()
/// 返回: 所有元素的和（int 或 double）
/// 异常: 当列表为空或包含非数值元素时抛出
/// </remarks>
public sealed class ListSumFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Sum"];
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

        if (list.Values.Count == 0)
        {
            throw new InvalidOperationError(list, "无法对空列表求和");
        }

        double sum = 0;
        bool hasDouble = false;

        foreach (var item in list.Values)
        {
            switch (item)
            {
                case IntLangValue intVal:
                    sum += intVal.Value;
                    break;
                case DoubleLangValue doubleVal:
                    sum += doubleVal.Value;
                    hasDouble = true;
                    break;
                default:
                    throw new InvalidOperationError(list, $"无法对非数值类型 {item.TypeToString()} 求和");
            }
        }

        return hasDouble ? new DoubleLangValue(sum) : new IntLangValue((int)sum);
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

        // sum 变量
        var sumLocal = ilGenerator.DeclareLocal(typeof(double));
        ilGenerator.Emit(OpCodes.Ldc_R8, 0.0);
        ilGenerator.Emit(OpCodes.Stloc, sumLocal);

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

        // 获取当前元素
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);

        // 转换为 double 并累加
        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(double));
        ilGenerator.Emit(OpCodes.Ldloc, sumLocal);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, sumLocal);

        // index++
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(loopEnd);

        // 返回 sum
        ilGenerator.Emit(OpCodes.Ldloc, sumLocal);
        ilGenerator.Emit(OpCodes.Box, typeof(double));
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;

        if (list.Count == 0)
        {
            throw new InvalidOperationException("无法对空列表求和");
        }

        double sum = 0;
        bool hasDouble = false;

        foreach (var item in list)
        {
            switch (item)
            {
                case int intVal:
                    sum += intVal;
                    break;
                case double doubleVal:
                    sum += doubleVal;
                    hasDouble = true;
                    break;
                default:
                    throw new InvalidOperationException($"无法对非数值类型求和");
            }
        }

        return hasDouble ? sum : (int)sum;
    }
}
