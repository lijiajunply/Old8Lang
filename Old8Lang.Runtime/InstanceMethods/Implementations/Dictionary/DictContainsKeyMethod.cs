using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

public class DictContainsKeyMethod : BaseInstanceMethod
{
    public override string[] Names => ["ContainsKey", "containsKey", "HasKey", "hasKey"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["key"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var key = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        var contains = dict.Value.Any(x => x.Key.Equal(key));
        return new BoolLangValue(contains);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlNativeCollectionBridge.EmitBridgeCall(this, instance, parameters, ilGenerator, local);
    }

protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (arguments.Length > 0 && instance is DictionaryLangValue dictLangValue)
        {
            var key = ConvertToLangValue(arguments[0]);
            return dictLangValue.Value.Any(x => x.Key.Equal(key));
        }

        if (instance is System.Collections.IDictionary dict && arguments.Length > 0)
        {
            var key = arguments[0];
            return dict.Contains(key!);
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }

    private static LangValueType ConvertToLangValue(object? value)
    {
        return value switch
        {
            null => NullLangValue.Instance,
            LangValueType langValue => langValue,
            int i => new IntLangValue(i),
            long l => new IntLangValue((int)l),
            double d => new DoubleLangValue(d),
            bool b => new BoolLangValue(b),
            string s => new StringLangValue(s),
            char c => new CharLangValue(c),
            _ => new StringLangValue(value.ToString() ?? string.Empty)
        };
    }
}
