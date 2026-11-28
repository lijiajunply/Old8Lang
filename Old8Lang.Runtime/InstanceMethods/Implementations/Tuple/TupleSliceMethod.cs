using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Tuple;

/// <summary>
/// Tuple.Slice(start, end) - 获取元组的切片
/// </summary>
public class TupleSliceMethod : BaseInstanceMethod
{
    public override string[] Names => ["Slice", "slice"];
    public override Type TargetType => typeof(TupleLangValue);
    public override string[]? ParameterNames => ["start", "end"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var tuple = (TupleLangValue)instance;
        var startParam = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        var endParam = parameters[1].Accept(RuntimeVisitors.CreateInterpreter(manager));

        if (startParam is not IntLangValue start)
        {
            throw new TypeError(position, "int", startParam.TypeToString());
        }

        if (endParam is not IntLangValue end)
        {
            throw new TypeError(position, "int", endParam.TypeToString());
        }

        return tuple.Slice(start.Value, end.Value);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlNativeCollectionBridge.EmitBridgeCall(this, instance, parameters, ilGenerator, local);
    }

    public static TupleLangValue SliceHelper(TupleLangValue tuple, IntLangValue start, IntLangValue end)
    {
        return tuple.Slice(start.Value, end.Value) as TupleLangValue ?? new TupleLangValue([]);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TupleLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 分派前会把原生嵌套元组转成 TupleLangValue，这里直接复用解释器语义。
        if (instance is TupleLangValue tupleValue && arguments[0] is int vmStart && arguments[1] is int vmEnd)
        {
            return tupleValue.Slice(vmStart, vmEnd);
        }

        if (instance is ITuple tuple && arguments[0] is int start && arguments[1] is int end)
        {
            int length = tuple.Length;

            // 处理负索引
            if (start < 0) start += length;
            if (end < 0) end += length;

            // 边界检查
            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            // 确保 start <= end
            if (start >= end) return new ValueTuple();

            // 提取元素
            var items = new object[end - start];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = tuple[start + i]!;
            }

            // 动态创建新的 ValueTuple
            return TupleLangValue.CreateValueTupleStatic(items);
        }

        throw new ArgumentException("参数类型不匹配");
    }
}
