using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

public class DictKeysMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Keys", "keys"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        return new ListLangValue(dict.Value.Select(x => x.Key).ToList());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(DictKeysMethod).GetMethod(nameof(KeysHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：从桥接后的字典取出键并返回原生列表。
    /// </summary>
    public static List<object?> KeysHelper(LangValueType instance)
    {
        if (instance is not DictionaryLangValue dict)
        {
            throw new ArgumentException($"实例必须是字典类型，实际是 {instance.GetType().Name}");
        }

        var manager = new VariateManager();
        dict.Accept(RuntimeVisitors.CreateInterpreter(manager));
        return dict.Value.Select(pair => IlValueBridge.Unwrap(pair.Key)).ToList();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict)
        {
            return dict.Keys.Cast<object?>().ToList();
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
