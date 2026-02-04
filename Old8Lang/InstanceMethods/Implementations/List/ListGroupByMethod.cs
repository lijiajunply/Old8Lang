using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.GroupBy 方法 - 按键分组
/// </summary>
public class ListGroupByMethod : BaseInstanceMethod
{
    public override string[] Names => ["GroupBy", "groupBy"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["keySelector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var keySelectorParam = parameters[0].Run(manager);

        if (keySelectorParam is not FuncLangValue keySelector)
        {
            throw new TypeError(position, $"GroupBy 方法的参数必须是函数类型，但实际是 {keySelectorParam.GetType().Name}");
        }

        // 使用字典来分组
        var groups = new Dictionary<string, List<LangValueType>>();

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var key = keySelector.Run(tempManager, [item]);
            var keyString = key.ToString() ?? "null";

            if (!groups.ContainsKey(keyString))
            {
                groups[keyString] = new List<LangValueType>();
            }

            groups[keyString].Add(item);
        }

        // 将分组结果转换为列表的列表
        var result = new List<LangValueType>();
        foreach (var group in groups)
        {
            // 创建一个包含键和值列表的元组
            var groupList = new ListLangValue([
                new StringLangValue(group.Key),
                new ListLangValue(group.Value)
            ]);
            result.Add(groupList);
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载键选择器函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListGroupByMethod).GetMethod(nameof(GroupByHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：分组操作
    /// </summary>
    public static ListLangValue GroupByHelper(ListLangValue list, LangValueType keySelectorParam)
    {
        if (keySelectorParam is not FuncLangValue keySelector)
        {
            throw new Exception("GroupBy 方法的参数必须是函数类型");
        }

        // 使用字典来分组
        var groups = new Dictionary<string, List<LangValueType>>();

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var key = keySelector.Run(tempManager, [item]);
            var keyString = key.ToString() ?? "null";

            if (!groups.ContainsKey(keyString))
            {
                groups[keyString] = new List<LangValueType>();
            }

            groups[keyString].Add(item);
        }

        // 将分组结果转换为列表的列表
        var result = new List<LangValueType>();
        foreach (var group in groups)
        {
            // 创建一个包含键和值列表的元组
            var groupList = new ListLangValue([
                new StringLangValue(group.Key),
                new ListLangValue(group.Value)
            ]);
            result.Add(groupList);
        }

        return new ListLangValue(result);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            var keySelector = arguments[0] as Func<object?, object?>;
            if (keySelector == null)
            {
                throw new ArgumentException("参数必须是键选择器函数");
            }

            var groups = list.GroupBy(keySelector);
            var result = new List<object?>();

            foreach (var group in groups)
            {
                result.Add(new List<object?> { group.Key, group.ToList() });
            }

            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
