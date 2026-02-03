using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

public class DictRemoveMethod : BaseInstanceMethod
{
    public override string[] Names => ["Remove", "remove", "Delete", "delete"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[]? ParameterNames => ["key"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var key = parameters[0].Run(manager);

        // 查找并移除键值对
        var index = dict.Value.FindIndex(x => x.Key.Equal(key));
        if (index >= 0)
        {
            dict.Value.RemoveAt(index);
            return new BoolLangValue(true);
        }
        return new BoolLangValue(false);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictRemoveMethod).GetMethod(nameof(RemoveHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool RemoveHelper(Dictionary<object, object> dict, object key)
    {
        return dict.Remove(key);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 0)
        {
            var key = arguments[0];
            return dict.Remove(key!);
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
