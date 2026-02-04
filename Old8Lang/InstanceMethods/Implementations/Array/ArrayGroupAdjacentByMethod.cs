using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.GroupAdjacentBy(keySelector) - 使用键选择器将相邻的相同键元素分组
/// </summary>
public class ArrayGroupAdjacentByMethod : BaseInstanceMethod
{
    public override string[] Names => ["GroupAdjacentBy", "groupAdjacentBy"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[] ParameterNames => ["keySelector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var keySelector = parameters[0].Run(manager) as FuncLangValue;

        if (keySelector == null)
        {
            throw new ArgumentError(position, "keySelector 参数必须是函数类型");
        }

        var result = new List<LangValueType>();

        if (array.RunResult.Length == 0)
        {
            return new ListLangValue(result);
        }

        var currentGroup = new List<LangValueType> { array.RunResult[0] };
        var currentKey = keySelector.Run(manager, [array.RunResult[0]]);

        for (int i = 1; i < array.RunResult.Length; i++)
        {
            var key = keySelector.Run(manager, [array.RunResult[i]]);

            if (key.Equal(currentKey))
            {
                currentGroup.Add(array.RunResult[i]);
            }
            else
            {
                result.Add(new ArrayLangValue(currentGroup.ToArray()));
                currentGroup = new List<LangValueType> { array.RunResult[i] };
                currentKey = key;
            }
        }

        result.Add(new ArrayLangValue(currentGroup.ToArray()));
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Array.GroupAdjacentBy 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is object?[] array && arguments.Length > 0)
        {
            var keySelector = arguments[0];
            var vm = VMContext.CurrentVM;
            var result = new List<object?>();

            if (array.Length == 0)
            {
                return result;
            }

            var currentGroup = new List<object?> { array[0] };
            var currentKey = vm.CallFunctionObject(keySelector, [array[0]]);

            for (int i = 1; i < array.Length; i++)
            {
                var key = vm.CallFunctionObject(keySelector, [array[i]]);

                if (Equals(key, currentKey))
                {
                    currentGroup.Add(array[i]);
                }
                else
                {
                    result.Add(currentGroup.ToArray());
                    currentGroup = new List<object?> { array[i] };
                    currentKey = key;
                }
            }

            result.Add(currentGroup.ToArray());
            return result;
        }

        throw new ArgumentException("实例必须是 object?[] 类型");
    }
}
