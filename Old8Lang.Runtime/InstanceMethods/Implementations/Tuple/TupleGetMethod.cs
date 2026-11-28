using System.Reflection.Emit;
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
/// Tuple.Get(index) - 获取元组指定索引的元素
/// </summary>
public class TupleGetMethod : BaseInstanceMethod
{
    public override string[] Names => ["Get", "get"];
    public override Type TargetType => typeof(TupleLangValue);
    public override string[]? ParameterNames => ["index"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var tuple = (TupleLangValue)instance;
        var indexValue = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));

        if (indexValue is not IntLangValue index)
        {
            throw new TypeError(parameters[0], "IntValue", indexValue.GetType().Name);
        }

        return tuple.Get(index);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlNativeCollectionBridge.EmitBridgeCall(this, instance, parameters, ilGenerator, local);
    }

    public static LangValueType GetHelper(TupleLangValue tuple, IntLangValue index)
    {
        return tuple.Get(index);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not TupleLangValue tuple)
        {
            throw new ArgumentException("实例必须是 Tuple 类型");
        }

        if (arguments.Length != 1 || arguments[0] is not int index)
        {
            throw new ArgumentException("Get 方法需要一个整数参数");
        }

        // VM 调用点按原生值使用结果（与列表 Get 一致），避免得到装箱的语言值。
        return IlValueBridge.Unwrap(tuple.Get(index));
    }
}
