using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Any(predicate?) - 检查是否有任意元素满足条件
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListAnyMethod : BaseLangListMethod
{
    public override string[] Names => ["Any", "any"];
    public override string[]? ParameterNames => ["predicate"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        // 无参数：检查列表是否非空
        if (parameters.Count == 0)
        {
            return BoolLangValue.Create(items.Count > 0, position);
        }

        // 有参数：检查是否有元素满足条件
        var predicateExpr = parameters[0].Run(manager);
        if (predicateExpr is not FuncLangValue predicate)
        {
            throw new ArgumentException("Any 方法的参数必须是函数");
        }

        foreach (var item in items)
        {
            var args = new List<LangExpression> { item };
            var result = predicate.Run(manager, args);

            if (result is BoolLangValue boolResult && boolResult.Value)
            {
                return BoolLangValue.Create(true, position);
            }
        }

        return BoolLangValue.Create(false, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        if (parameters.Count == 0)
        {
            // 无参数版本：检查长度是否大于0
            instance.LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(LangListAnyMethod).GetMethod(nameof(AnyHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            // 有参数版本暂不支持
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
        }
    }

    public static bool AnyHelper(ILangList langList)
    {
        return langList.GetLength() > 0;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
        LocalManager local)
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

        // 无参数：检查列表是否非空
        if (arguments.Length == 0)
        {
            return items.Count > 0;
        }

        // 有参数：检查是否有元素满足条件
        var predicate = arguments[0];
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            throw new InvalidOperationException("VM 上下文未初始化");
        }

        foreach (var result in items.Select(item => vm.CallFunctionObject(predicate, [item])))
        {
            // 检查结果是否为 true
            if (result is true)
            {
                return true;
            }
        }

        return false;
    }
}