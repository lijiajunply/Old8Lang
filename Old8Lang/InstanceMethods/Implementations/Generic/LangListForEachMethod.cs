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
        // 获取集合元素
        List<object?> items;
        if (instance is ILangList langList)
        {
            items = langList.GetItems().Cast<object?>().ToList();
        }
        else if (instance is System.Collections.IList list)
        {
            items = list.Cast<object?>().ToList();
        }
        else
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
        }

        if (arguments.Length == 0)
        {
            throw new ArgumentException("ForEach 方法需要一个 action 参数");
        }

        var action = arguments[0];
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("VM 上下文未初始化");
        }

        foreach (var item in items)
        {
            vm.CallFunctionObject(action, [item]);
        }

        // ForEach 返回原实例以支持链式调用
        return instance;
    }
}
