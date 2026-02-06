using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Last() - 获取最后一个元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListLastMethod : BaseLangListMethod
{
    public override string[] Names => ["Last", "last"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    /// <summary>
    /// 参数类型：无参数
    /// </summary>
    public override Type?[]? ParameterTypes => [];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "获取列表的最后一个元素";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        if (items.Count == 0)
        {
            throw new InvalidOperationError(instance, "序列不包含任何元素");
        }

        return items[items.Count - 1];
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListLastMethod).GetMethod(nameof(LastHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static LangValueType LastHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        if (items.Count == 0)
        {
            throw new InvalidOperationException("序列不包含任何元素");
        }
        return items[items.Count - 1];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            var items = langList.GetItems().ToList();
            if (items.Count == 0)
            {
                throw new InvalidOperationException("序列不包含任何元素");
            }
            return items[items.Count - 1];
        }

        if (instance is System.Collections.IList list && list.Count > 0)
        {
            return list[list.Count - 1];
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
    }
}
