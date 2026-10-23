using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToStr() - 转换为字符串表示
/// </summary>
public class ValueTypeToStrMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToStr", "toStr", "ToString", "toString"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(StringLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return new StringLangValue(instance.ToDisplayString());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 接收者可能是原生 int/string/...（IL 模式），helper 的形参是 LangValueType，
        // 必须先经过桥接装箱，否则会直接把 int32 传给引用类型形参，生成无效 IL。
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(ValueTypeToStrMethod).GetMethod(nameof(ConvertToStrHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static string ConvertToStrHelper(LangValueType type)
    {
        return type.ToDisplayString();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 与 PrintLine / Print / Error 共用同一套显示格式化，保证 print(x) 与 print(x.ToStr()) 一致
        return Old8Lang.Runtime.VMValueFormatter.ToDisplayString(instance);
    }
}
