using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Tuple 函数 - 创建空元组
/// </summary>
public sealed class TupleFunction : BaseGlobalFunction
{
    public override string[] Names => ["Tuple", "tuple"];
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        return new TupleLangValue([], position);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持元组创建
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TupleLangValue);
    }
    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return new TupleLangValue([], default);
    }
}