using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.GroupAdjacentBy(keySelector) - 使用键选择器将相邻的相同键元素分组
/// </summary>
public class ListGroupAdjacentByMethod : BaseInstanceMethod
{
    public override string[] Names => ["GroupAdjacentBy", "groupAdjacentBy"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["keySelector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var keySelector = parameters[0].Run(manager) as FuncLangValue;

        if (keySelector == null)
        {
            throw new ArgumentError(position, "keySelector 参数必须是函数类型");
        }

        var result = new List<LangValueType>();

        if (list.Values.Count == 0)
        {
            return new ListLangValue(result);
        }

        var currentGroup = new List<LangValueType> { list.Values[0] };
        var currentKey = keySelector.Run(manager, [list.Values[0]]);

        for (int i = 1; i < list.Values.Count; i++)
        {
            var key = keySelector.Run(manager, [list.Values[i]]);

            if (key.Equal(currentKey))
            {
                currentGroup.Add(list.Values[i]);
            }
            else
            {
                result.Add(new ListLangValue(currentGroup));
                currentGroup = new List<LangValueType> { list.Values[i] };
                currentKey = key;
            }
        }

        result.Add(new ListLangValue(currentGroup));
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.GroupAdjacentBy 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 支持 List<object?> 和 object?[] 两种类型
        List<object?> list;
        if (instance is List<object?> listInstance)
        {
            list = listInstance;
        }
        else if (instance is object?[] arrayInstance)
        {
            list = arrayInstance.ToList();
        }
        else
        {
            throw new ArgumentException("实例必须是 List<object?> 或 object?[] 类型");
        }

        if (arguments.Length > 0)
        {
            var keySelector = arguments[0];
            var vm = VMContext.CurrentVM;
            var result = new List<object?>();

            if (list.Count == 0)
            {
                return result;
            }

            var currentGroup = new List<object?> { list[0] };
            var currentKey = vm.CallFunctionObject(keySelector, [list[0]]);

            for (int i = 1; i < list.Count; i++)
            {
                var key = vm.CallFunctionObject(keySelector, [list[i]]);

                if (Equals(key, currentKey))
                {
                    currentGroup.Add(list[i]);
                }
                else
                {
                    result.Add(currentGroup.ToArray());
                    currentGroup = new List<object?> { list[i] };
                    currentKey = key;
                }
            }

            result.Add(currentGroup.ToArray());
            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 或 object?[] 类型");
    }
}
