using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Zip(other, zipper?) - 拉链操作
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListZipMethod : BaseLangListMethod
{
    public override string[] Names => ["Zip", "zip"];
    public override string[]? ParameterNames => ["other", "zipper"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var otherValue = parameters[0].Run(manager);

        if (otherValue is not ILangList otherList)
        {
            throw new ArgumentException("Zip 方法的第一个参数必须实现 ILangList 接口");
        }

        var otherItems = otherList.GetItems().ToList();
        var zippedItems = new List<LangValueType>();

        // 如果提供了 zipper 函数
        if (parameters.Count == 2)
        {
            var zipperExpr = parameters[1].Run(manager);
            if (zipperExpr is not FuncLangValue zipper)
            {
                throw new ArgumentException("Zip 方法的第二个参数必须是函数");
            }

            int minLength = Math.Min(items.Count, otherItems.Count);
            for (int i = 0; i < minLength; i++)
            {
                var args = new List<LangExpression> { items[i], otherItems[i] };
                var result = zipper.Run(manager, args);
                zippedItems.Add(result);
            }
        }
        else
        {
            // 默认创建元组
            int minLength = Math.Min(items.Count, otherItems.Count);
            for (int i = 0; i < minLength; i++)
            {
                var tuple = new TupleLangValue(new List<LangExpression> { items[i], otherItems[i] }, position);
                tuple.ItemValues.Add(items[i]);
                tuple.ItemValues.Add(otherItems[i]);
                zippedItems.Add(tuple);
            }
        }

        return new ListLangValue(zippedItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        if (parameters.Count == 1)
        {
            instance.LoadIlValue(ilGenerator, local);
            parameters[0].LoadIlValue(ilGenerator, local);

            var helperMethod = typeof(LangListZipMethod).GetMethod(nameof(ZipHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            // 编译模式暂不支持带 zipper 函数的版本
            ilGenerator.Emit(OpCodes.Ldnull);
        }
    }

    public static ListLangValue ZipHelper(ILangList langList, ILangList other)
    {
        var items = langList.GetItems().ToList();
        var otherItems = other.GetItems().ToList();
        var zippedItems = new List<LangValueType>();

        int minLength = Math.Min(items.Count, otherItems.Count);
        for (int i = 0; i < minLength; i++)
        {
            var tuple = new TupleLangValue(new List<LangExpression> { items[i], otherItems[i] });
            tuple.ItemValues.Add(items[i]);
            tuple.ItemValues.Add(otherItems[i]);
            zippedItems.Add(tuple);
        }

        return new ListLangValue(zippedItems);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList && arguments[0] is ILangList other)
        {
            if (arguments.Length == 1)
            {
                return ZipHelper(langList, other);
            }
        }

        throw new ArgumentException($"实例和参数必须实现 ILangList 接口");
    }
}
