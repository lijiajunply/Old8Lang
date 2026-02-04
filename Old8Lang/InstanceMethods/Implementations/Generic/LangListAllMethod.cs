using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.All(predicate) - 检查是否所有元素都满足条件
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListAllMethod : BaseLangListMethod
{
    public override string[] Names => ["All", "all"];
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
            throw new ArgumentException("All 方法的参数必须是函数");
        }

        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var result = predicate.Run(manager, args);

            if (result is BoolLangValue boolResult && !boolResult.Value)
            {
                return BoolLangValue.Create(false, position);
            }
        }

        return BoolLangValue.Create(true, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持高阶函数
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
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
            throw new ArgumentException("All 方法需要一个 predicate 参数");
        }

        var predicate = arguments[0];
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("VM 上下文未初始化");
        }

        foreach (var item in items)
        {
            var result = vm.CallFunctionObject(predicate, [item]);

            // 检查结果是否为 false
            if (result is bool boolResult && !boolResult)
            {
                return false;
            }
        }

        return true;
    }
}
