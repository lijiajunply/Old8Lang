using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.GroupBy(keySelector) - 分组
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListGroupByMethod : BaseLangListMethod
{
    public override string[] Names => ["GroupBy", "groupBy"];
    public override string[] ParameterNames => ["keySelector"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var keySelectorExpr = parameters[0].Run(manager);

        if (keySelectorExpr is not FuncLangValue keySelector)
        {
            throw new ArgumentException("GroupBy 方法的参数必须是函数");
        }

        // 使用字典来分组
        var groups = new Dictionary<string, List<LangValueType>>();

        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var key = keySelector.Run(manager, args);
            var keyString = key.ToString() ?? "null";

            if (!groups.ContainsKey(keyString))
            {
                groups[keyString] = new List<LangValueType>();
            }
            groups[keyString].Add(item);
        }

        // 将分组结果转换为字典
        var resultDict = new DictionaryLangValue(null, null, position);
        foreach (var kvp in groups)
        {
            var keyValue = StringLangValue.Create(kvp.Key, position);
            var valueList = new ListLangValue(kvp.Value, null, position);
            resultDict.Set(keyValue, valueList);
        }

        return resultDict;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持高阶函数
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(DictionaryLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("VM 模式暂不支持 GroupBy 方法");
    }
}
