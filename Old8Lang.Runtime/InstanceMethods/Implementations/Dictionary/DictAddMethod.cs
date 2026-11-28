using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.Add(key, value) - 向字典中添加键值对
/// </summary>
public class DictAddMethod : BaseInstanceMethod
{
    public override string[] Names => ["Add", "add"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["key", "value"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var key = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        var value = parameters[1].Accept(RuntimeVisitors.CreateInterpreter(manager));

        // 若键已存在则原地更新，与 IL/VM 原生字典及 dict[k] <- v 语义保持一致
        for (var i = 0; i < dict.Value.Count; i++)
        {
            if (dict.Value[i].Key.Equal(key))
            {
                dict.Value[i] = (key, value);
                return new TupleLangValue(key, value);
            }
        }

        dict.Value.Add((key, value));
        return new TupleLangValue(key, value);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlNativeCollectionBridge.EmitBridgeCall(this, instance, parameters, ilGenerator, local);
    }

protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TupleLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 1)
        {
            var key = arguments[0];
            var value = arguments[1];
            dict[key!] = value!;
            return new TupleLangValue(IlValueBridge.Wrap(key), IlValueBridge.Wrap(value));
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
