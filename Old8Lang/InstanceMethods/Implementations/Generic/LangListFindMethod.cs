using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Find(predicate) - 查找满足条件的第一个元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListFindMethod : BaseLangListMethod
{
    public override string[] Names => ["Find", "find"];
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
            throw new ArgumentException("Find 方法的参数必须是函数");
        }

        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var result = predicate.Run(manager, args);

            if (result is BoolLangValue boolResult && boolResult.Value)
            {
                return item;
            }
        }

        // 未找到返回 null
        return NullLangValue.Instance;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持高阶函数
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("VM 模式暂不支持 Find 方法");
    }
}
