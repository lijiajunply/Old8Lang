using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

public partial class ReturnStatement(LangExpression returnExpression, SourcePosition position = default) : OldStatement(position)
{
    
    public override void Run(VariateManager manager)
    {
        var result = returnExpression.Run(manager);

        // 如果当前函数有返回类型注解，进行类型检查和转换
        if (!string.IsNullOrEmpty(manager.CurrentFunctionReturnType))
        {
            var functionName = "anonymous";
            if (manager.GetValue(new LangId("this")) is AnyLangValue anyValue)
            {
                functionName = anyValue.ClassId.IdName;
            }
            // 使用新的带转换的验证方法，传递泛型类型参数映射
            result = TypeChecker.ValidateAndConvertReturnType(
                manager.CurrentFunctionReturnType,
                result,
                this,
                functionName,
                manager.CurrentFunctionTypeArgumentMapping);
        }

        manager.Result = result;
        manager.IsReturn = true;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 检查是否在finally块中使用了return语句，这在.NET IL中是不允许的
        if (local.IsInFinallyBlock)
        {
            throw new Error.CompilerException("在finally块中不允许使用return语句", Position);
        }

        // 获取返回值表达式的类型
        var returnType = returnExpression.OutputType(local);

        // 加载返回表达式的值到栈上
        returnExpression.LoadIlValue(ilGenerator, local);

        // 只有当返回值类型是 Task<object> 时，才进行异步包装
        // 这表明当前函数是异步函数
        if (returnType == typeof(Task<object>))
        {
            // 返回值已经是 Task<object>，直接返回
            ilGenerator.Emit(OpCodes.Ret);
        }
        else
        {
            // 对于同步函数，直接返回值，不进行任何包装
            ilGenerator.Emit(OpCodes.Ret);
        }
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public Type OutputType(LocalManager local) => returnExpression.OutputType(local)!;

    public override string ToString() => $"return {returnExpression}";
}