using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Map(mapper) - 映射列表元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListMapMethod : BaseLangListMethod
{
    public override string[] Names => ["Map", "map", "Select", "select"];
    public override string[] ParameterNames => ["mapper"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var mapperExpr = parameters[0].Run(manager);

        if (mapperExpr is not FuncLangValue mapper)
        {
            throw new ArgumentException("Map 方法的参数必须是函数");
        }

        var mappedItems = new List<LangValueType>();
        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var result = mapper.Run(manager, args);
            mappedItems.Add(result);
        }

        return new ListLangValue(mappedItems, null, position);
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
        throw new NotSupportedException("VM 模式暂不支持 Map 方法");
    }
}
