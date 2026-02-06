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
public class DictMapMethod : BaseInstanceMethod
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

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Dictionary.Map 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(Dictionary<object, object>);
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
