using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.String;

/// <summary>
/// String.Replace 方法 - 替换字符串
/// </summary>
public class StringReplaceMethod : BaseInstanceMethod
{
    public override string[] Names => ["Replace", "replace"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[] ParameterNames => ["oldValue", "newValue"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var oldValue = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        var newValue = parameters[1].Accept(RuntimeVisitors.CreateInterpreter(manager));

        if (oldValue is not StringLangValue oldStr)
        {
            throw new TypeError(position, $"Replace 方法的第一个参数必须是字符串类型，但实际是 {oldValue.GetType().Name}");
        }

        if (newValue is not StringLangValue newStr)
        {
            throw new TypeError(position, $"Replace 方法的第二个参数必须是字符串类型，但实际是 {newValue.GetType().Name}");
        }

        return new StringLangValue(str.Value.Replace(oldStr.Value, newStr.Value));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载字符串实例
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        // 加载旧值
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        // 加载新值
        IlValueBridge.EmitLoadWrapped(parameters[1], ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(StringReplaceMethod).GetMethod(nameof(ReplaceHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：替换字符串
    /// </summary>
    public static string ReplaceHelper(StringLangValue str, StringLangValue oldValue, StringLangValue newValue)
    {
        return str.Value.Replace(oldValue.Value, newValue.Value);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length >= 2 &&
            arguments[0] is string oldValue && arguments[1] is string newValue)
        {
            return str.Replace(oldValue, newValue);
        }

        throw new ArgumentException("实例和参数必须是 string 类型");
    }
}
