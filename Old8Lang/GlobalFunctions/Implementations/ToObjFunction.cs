using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// ToObj 函数 - 将 JSON 字符串反序列化为对象
/// </summary>
public sealed class ToObjFunction : BaseGlobalFunction
{
    public override string[] Names => ["ToObj", "toObj"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        
        if (results[0] is not StringLangValue stringValue)
            throw new TypeError(parameters[0], "StringValue", results[0].GetType().Name);
            
        return stringValue.ToObj();
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持 JSON 反序列化
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(AnyLangValue);
    }
    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        // VM 模式下不支持 ToObj,返回 null
        return null;
    }
}