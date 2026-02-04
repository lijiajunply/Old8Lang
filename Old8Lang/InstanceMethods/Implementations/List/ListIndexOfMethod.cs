using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.IndexOf 方法 - 查找元素在列表中第一次出现的索引
/// </summary>
public class ListIndexOfMethod : BaseInstanceMethod
{
    public override string[] Names => ["IndexOf", "indexOf"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["element"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var element = parameters[0].Run(manager);

        for (var i = 0; i < list.Values.Count; i++)
        {
            var v = list.Values[i];
            bool isEqual = false;

            // 使用值比较
            if (v.GetType() == element.GetType())
            {
                if (v is IntLangValue intV && element is IntLangValue intItem)
                {
                    isEqual = intV.Value == intItem.Value;
                }
                else if (v is DoubleLangValue doubleV && element is DoubleLangValue doubleItem)
                {
                    isEqual = Math.Abs(doubleV.Value - doubleItem.Value) < 0.0000001;
                }
                else if (v is StringLangValue strV && element is StringLangValue strItem)
                {
                    isEqual = strV.Value == strItem.Value;
                }
                else if (v is BoolLangValue boolV && element is BoolLangValue boolItem)
                {
                    isEqual = boolV.Value == boolItem.Value;
                }
                else if (v is CharLangValue charV && element is CharLangValue charItem)
                {
                    isEqual = charV.Value == charItem.Value;
                }
                else
                {
                    isEqual = v.Equals(element);
                }
            }
            else
            {
                isEqual = v.Equals(element);
            }

            if (isEqual)
            {
                return new IntLangValue(i);
            }
        }

        return new IntLangValue(-1); // 未找到返回-1
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载要查找的元素
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var indexOfHelperMethod = typeof(ListIndexOfMethod).GetMethod(nameof(IndexOfHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, indexOfHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：查找元素索引
    /// </summary>
    public static IntLangValue IndexOfHelper(ListLangValue list, LangValueType element)
    {
        for (var i = 0; i < list.Values.Count; i++)
        {
            var v = list.Values[i];
            bool isEqual = false;

            if (v.GetType() == element.GetType())
            {
                if (v is IntLangValue intV && element is IntLangValue intItem)
                {
                    isEqual = intV.Value == intItem.Value;
                }
                else if (v is DoubleLangValue doubleV && element is DoubleLangValue doubleItem)
                {
                    isEqual = Math.Abs(doubleV.Value - doubleItem.Value) < 0.0000001;
                }
                else if (v is StringLangValue strV && element is StringLangValue strItem)
                {
                    isEqual = strV.Value == strItem.Value;
                }
                else if (v is BoolLangValue boolV && element is BoolLangValue boolItem)
                {
                    isEqual = boolV.Value == boolItem.Value;
                }
                else if (v is CharLangValue charV && element is CharLangValue charItem)
                {
                    isEqual = charV.Value == charItem.Value;
                }
                else
                {
                    isEqual = v.Equals(element);
                }
            }
            else
            {
                isEqual = v.Equals(element);
            }

            if (isEqual)
            {
                return new IntLangValue(i);
            }
        }

        return new IntLangValue(-1);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(IntLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            var element = arguments[0];
            return list.IndexOf(element);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
