using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Filter(predicate) - 过滤列表元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListFilterMethod : BaseLangListMethod
{
    public override string[] Names => ["Filter", "filter", "Where", "where"];
    public override string[] ParameterNames => ["predicate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var predicateExpr = parameters[0].Run(manager);

        if (predicateExpr is not FuncLangValue predicate)
        {
            throw new ArgumentException("Filter 方法的参数必须是函数");
        }

        var filteredItems = new List<LangValueType>();
        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var result = predicate.Run(manager, args);

            if (result is BoolLangValue boolResult && boolResult.Value)
            {
                filteredItems.Add(item);
            }
        }

        return new ListLangValue(filteredItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持高阶函数
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("VM 模式暂不支持 Filter 方法");
    }
}
