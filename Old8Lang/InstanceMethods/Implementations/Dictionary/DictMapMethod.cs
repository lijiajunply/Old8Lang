using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.Map(func) - 使用函数转换字典中的所有值
/// </summary>
public class DictMapMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Map", "map"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["func"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var func = parameters[0].Run(manager) as FuncLangValue;

        if (func == null)
        {
            throw new ArgumentException("参数必须是函数类型");
        }

        var newDict = new DictionaryLangValue();

        foreach (var (key, value) in dict.Value)
        {
            try
            {
                var result = func.Run(manager, [value]);
                newDict.Value.Add((key, result));
            }
            catch
            {
                // 如果转换失败，保留原值
                newDict.Value.Add((key, value));
            }
        }

        return newDict;
    }

    /// <summary>
    /// IL 模式：接收者是原生 <c>Dictionary&lt;K,V&gt;</c>，函数参数是编译好的 .NET 委托。
    /// 先把两者转换成 Old8Lang 值调用 helper，再把结果按原生字典返回。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(DictMapMethod).GetMethod(nameof(MapHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：按函数转换字典中的所有值，返回原生字典。
    /// 与 <see cref="ExecuteInternal"/> 语义一致（转换失败时保留原值）。
    /// </summary>
    public static Dictionary<object, object?> MapHelper(LangValueType instance, LangValueType func)
    {
        if (instance is not DictionaryLangValue dict)
        {
            throw new ArgumentException($"实例必须是字典类型，实际是 {instance.GetType().Name}");
        }

        if (func is not FuncLangValue function)
        {
            throw new ArgumentException("参数必须是函数类型");
        }

        var manager = new VariateManager();
        // IL 模式下字典由 IlValueBridge.Wrap 构造，Value 列表尚未填充
        dict.Run(manager);

        var newDict = new Dictionary<object, object?>();

        foreach (var (key, value) in dict.Value)
        {
            try
            {
                var result = function.Run(manager, [value]);
                newDict[IlValueBridge.Unwrap(key)!] = IlValueBridge.Unwrap(result);
            }
            catch
            {
                // 如果转换失败，保留原值
                newDict[IlValueBridge.Unwrap(key)!] = IlValueBridge.Unwrap(value);
            }
        }

        return newDict;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(Dictionary<object, object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 0)
        {
            var func = arguments[0];
            var vm = VMContext.CurrentVM;
            var newDict = new Dictionary<object, object>();

            foreach (var (key, value) in dict)
            {
                try
                {
                    var result = vm.CallFunctionObject(func, [value]);
                    newDict[key] = result!;
                }
                catch
                {
                    // 如果转换失败，保留原值
                    newDict[key] = value;
                }
            }

            return newDict;
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
