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
/// ILangList.Concat(other) - 连接两个列表
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListConcatMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Concat", "concat"];
    public override string[] ParameterNames => ["other"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var otherValue = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));

        if (otherValue is not ILangList otherList)
        {
            throw new ArgumentException("Concat 方法的参数必须实现 ILangList 接口");
        }

        var otherItems = otherList.GetItems();
        var concatenated = items.Concat(otherItems).ToList();

        return new ListLangValue(concatenated, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(LangListConcatMethod).GetMethod(nameof(ConcatNativeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> ConcatNativeHelper(LangValueType instance, LangValueType other)
    {
        if (instance is not ILangList langList || other is not ILangList otherList)
            throw new ArgumentException("实例和参数必须实现 ILangList 接口");
        return ConcatHelper(langList, otherList).GetItems().Select(IlValueBridge.Unwrap).ToList();
    }

    public static ListLangValue ConcatHelper(ILangList langList, ILangList other)
    {
        var items = langList.GetItems().Concat(other.GetItems()).ToList();
        return new ListLangValue(items);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return GetItemsForVM(instance).Concat(GetItemsForVM(arguments[0])).ToList();
    }
}
