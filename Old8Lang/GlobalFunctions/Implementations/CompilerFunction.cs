using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Compiler 函数 - 编译代码字符串为中间代码
/// </summary>
public sealed class CompilerFunction : BaseGlobalFunction
{
    public override string[] Names => ["Compiler", "compiler"];
    public override string[]? ParameterNames => ["code"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        if (parameters.Count == 0) return new VoidLangValue();

        var results = EvaluateParameters(parameters, manager);
        string value;
        if (results[0] is StringLangValue sv)
        {
            value = sv.Value;
        }
        else
        {
            value = results[0].ToString();
        }

        var statement = manager.Interpreter.Build(code: value);
        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager();
        statement.GenerateIl(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ret);
        foreach (var info in local.DelegateVar)
        {
            manager.AddClassAndFunc(new FuncLangValue(info.Key, info.Value));
        }

        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        ilGenerator.Emit(OpCodes.Ldstr, "编译环境不需要使用Compiler方法");
        ilGenerator.Emit(OpCodes.Call,
            typeof(Console).GetMethod("WriteLine", [typeof(string)])!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        // VM 模式下不支持 Compiler,返回 null
        return null;
    }
}