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
public class DictForEachMethod : BaseInstanceMethod
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

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Dictionary.ForEach 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
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
