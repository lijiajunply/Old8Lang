using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ForEach(action) - 遍历执行
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListForEachMethod : BaseLangListMethod
{
    public override string[] Names => ["ForEach", "forEach", "Each", "each"];
    public override string[] ParameterNames => ["action"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var actionExpr = parameters[0].Run(manager);

        if (actionExpr is not FuncLangValue action)
        {
            throw new ArgumentException("ForEach 方法的参数必须是函数");
        }

        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            action.Run(manager, args);
        }

        // ForEach 返回原实例以支持链式调用
        return instance;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持高阶函数
        instance.LoadIlValue(ilGenerator, local);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return instanceType;
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("VM 模式暂不支持 ForEach 方法");
    }
}
