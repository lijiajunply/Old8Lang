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

public class StringEndsWithMethod : BaseInstanceMethod
{
    public override string[] Names => ["EndsWith", "endsWith"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[] ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var valueParam = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        if (valueParam is not StringLangValue value)
            throw new TypeError(position, $"EndsWith 方法的参数必须是字符串类型");
        return new BoolLangValue(str.Value.EndsWith(value.Value));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
        var helperMethod = typeof(StringEndsWithMethod).GetMethod(nameof(EndsWithHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool EndsWithHelper(StringLangValue str, StringLangValue value)
    {
        // IL 路径统一用原生 bool 返回：与其他返回 bool 的实例方法一致，
        // 这样 Assert.True(...) / if 条件等原生布尔上下文可以直接使用。
        return str.Value.EndsWith(value.Value);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length > 0 && arguments[0] is string value)
            return str.EndsWith(value);
        throw new ArgumentException("实例和参数必须是 string 类型");
    }
}
