using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Lock 函数 - 创建锁定变量以实现线程安全访问
/// </summary>
public sealed class LockFunction : BaseGlobalFunction
{
    public override string[] Names => ["Lock", "lock"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var expr = parameters[0];
        var result = expr.Run(manager);

        // 获取变量名（用于标识和调试）
        string varName;
        if (expr is LangId id)
        {
            varName = id.IdName;
        }
        else if (expr is ClassMemberId memberId)
        {
            varName = memberId.ToString();
        }
        else
        {
            varName = "anonymous";
        }

        // 创建锁定变量
        var lockedVar = new LockedVariableLangValue(result, varName, position);

        // 如果是变量引用，在管理器中注册
        if (expr is LangId idRef)
        {
            manager.RegisterLockedVariable(idRef.IdName, lockedVar);
        }

        return lockedVar;
    }

    protected override void GenerateILInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持锁定变量
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LockedVariableLangValue);
    }
}