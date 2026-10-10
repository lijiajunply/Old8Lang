using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.ForEach(action) - 对字典中的每个键值对执行指定的操作
/// </summary>
public class DictForEachMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["ForEach", "forEach"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["action"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var action = parameters[0].Run(manager) as FuncLangValue;

        if (action == null)
        {
            throw new ArgumentException("参数必须是函数类型");
        }

        foreach (var (key, value) in dict.Value)
        {
            try
            {
                action.Run(manager, [key, value]);
            }
            catch
            {
                // 忽略执行错误，继续处理下一项
            }
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// IL 模式：接收者是原生 <c>Dictionary&lt;K,V&gt;</c>，动作是编译好的 .NET 委托。
    /// helper 返回 <see cref="VoidLangValue"/>（不能返回 C# void，否则调用点栈不平衡）。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(DictForEachMethod).GetMethod(nameof(ForEachHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：对字典中的每个键值对执行动作，返回空值。
    /// 与 <see cref="ExecuteInternal"/> 语义一致（忽略执行错误，继续处理下一项）。
    /// </summary>
    public static LangValueType ForEachHelper(LangValueType instance, LangValueType action)
    {
        if (instance is not DictionaryLangValue dict)
        {
            throw new ArgumentException($"实例必须是字典类型，实际是 {instance.GetType().Name}");
        }

        if (action is not FuncLangValue function)
        {
            throw new ArgumentException("参数必须是函数类型");
        }

        var manager = new VariateManager();
        // IL 模式下字典由 IlValueBridge.Wrap 构造，Value 列表尚未填充
        dict.Run(manager);

        foreach (var (key, value) in dict.Value)
        {
            try
            {
                function.Run(manager, [key, value]);
            }
            catch
            {
                // 忽略执行错误，继续处理下一项
            }
        }

        return new VoidLangValue();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 0)
        {
            var action = arguments[0];
            var vm = VMContext.CurrentVM;

            foreach (var (key, value) in dict)
            {
                try
                {
                    vm.CallFunctionObject(action, [key, value]);
                }
                catch
                {
                    // 忽略执行错误，继续处理下一项
                }
            }

            return null;
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
