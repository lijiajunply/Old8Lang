using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.IndexOf(item) - 查找元素索引
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListIndexOfMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["IndexOf", "indexOf"];
    public override string[] ParameterNames => ["item"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var searchItem = parameters[0].Accept(new InterpreterVisitor(manager));

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Equal(searchItem))
            {
                return IntLangValue.Create(i, position);
            }
        }

        // 未找到返回 -1
        return IntLangValue.Create(-1, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(LangListIndexOfMethod).GetMethod(nameof(IndexOfNativeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static int IndexOfNativeHelper(LangValueType instance, LangValueType item)
    {
        if (instance is not ILangList langList)
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        return IndexOfHelper(langList, item);
    }

    public static int IndexOfHelper(ILangList langList, LangValueType item)
    {
        var items = langList.GetItems().ToList();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Equal(item))
            {
                return i;
            }
        }
        return -1;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            var items = langList.GetItems().ToList();
            var searchItem = arguments[0];

            if (searchItem is LangValueType langValue)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Equal(langValue))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        if (instance is System.Collections.IList list)
        {
            return list.IndexOf(arguments[0]);
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
    }
}
