using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Join 方法 - 将列表元素连接成字符串
/// </summary>
public class ListJoinMethod : BaseInstanceMethod
{
    public override string[] Names => ["Join", "join"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["separator"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        string separator = ", ";

        if (parameters.Count > 0)
        {
            var separatorValue = parameters[0].Run(manager);
            if (separatorValue is not StringLangValue strValue)
            {
                throw new ArgumentError(position, "分隔符必须是字符串类型");
            }
            separator = strValue.Value;
        }

        var items = list.Values.Select(item => item.ToString() ?? "null");
        return new StringLangValue(string.Join(separator, items));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载分隔符（如果有）
        if (parameters.Count > 0)
        {
            parameters[0].LoadIlValue(ilGenerator, local);
        }
        else
        {
            // 默认分隔符 ", "
            ilGenerator.Emit(OpCodes.Ldstr, ", ");
            ilGenerator.Emit(OpCodes.Newobj, typeof(StringLangValue).GetConstructor([typeof(string)])!);
        }

        // 调用辅助方法
        var helperMethod = typeof(ListJoinMethod).GetMethod(nameof(JoinHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：连接字符串
    /// </summary>
    public static StringLangValue JoinHelper(ListLangValue list, StringLangValue separator)
    {
        var items = list.Values.Select(item => item.ToString() ?? "null");
        return new StringLangValue(string.Join(separator.Value, items));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            string separator = ", ";

            if (arguments.Length > 0 && arguments[0] is string sep)
            {
                separator = sep;
            }

            var items = list.Select(item => item?.ToString() ?? "null");
            return string.Join(separator, items);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
