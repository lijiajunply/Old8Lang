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
/// Dictionary.Filter(predicate) - 使用条件过滤字典的键值对
/// </summary>
public class DictFilterMethod : BaseInstanceMethod
{
    public override string[] Names => ["Filter", "filter"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["predicate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var predicate = parameters[0].Run(manager) as FuncLangValue;

        if (predicate == null)
        {
            throw new ArgumentException("参数必须是函数类型");
        }

        var newDict = new DictionaryLangValue();

        foreach (var (key, value) in dict.Value)
        {
            try
            {
                var result = predicate.Run(manager, [key, value]);
                if (result is BoolLangValue { Value: true })
                {
                    newDict.Value.Add((key, value));
                }
            }
            catch
            {
                // 如果过滤函数失败，保留该项
                newDict.Value.Add((key, value));
            }
        }

        return newDict;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Dictionary.Filter 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(Dictionary<object, object>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 0)
        {
            var predicate = arguments[0];
            var vm = VMContext.CurrentVM;
            var newDict = new Dictionary<object, object>();

            foreach (var (key, value) in dict)
            {
                try
                {
                    var result = vm.CallFunctionObject(predicate, [key, value]);
                    if (result is bool boolResult && boolResult)
                    {
                        newDict[key] = value;
                    }
                }
                catch
                {
                    // 如果过滤函数失败，保留该项
                    newDict[key] = value;
                }
            }

            return newDict;
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
