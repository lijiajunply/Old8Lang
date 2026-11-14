using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

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
        // 保留原始值，以便异常包装对象也能使用其 ToString 实现。
        instance.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var instanceType = instance.Accept(new TypeInferenceVisitor(local));
        if (instance is not FuncLangValue && instanceType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, instanceType);
        }

        var helperMethod = typeof(ValueTypeToStrMethod).GetMethod(nameof(ConvertToStrHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static string ConvertToStrHelper(object? value)
    {
        return value is ExceptionWrapper exception
            ? exception.ToString()
            : IlValueBridge.Wrap(value).ToDisplayString();
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
