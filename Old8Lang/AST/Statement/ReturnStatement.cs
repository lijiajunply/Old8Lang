using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

public class ReturnStatement(LangExpression returnExpression, SourcePosition position = default) : OldStatement(position)
{
    
    public override void Run(VariateManager manager)
    {
        var result = returnExpression.Run(manager);

        // 如果当前函数有返回类型注解，进行类型检查
        if (!string.IsNullOrEmpty(manager.CurrentFunctionReturnType))
        {
            var functionName = "anonymous";
            if (manager.GetValue(new LangId("this")) is AnyLangValue anyValue)
            {
                functionName = anyValue.Id?.IdName ?? "anonymous";
            }
            TypeChecker.ValidateReturnType(manager.CurrentFunctionReturnType, result, this, functionName);
        }

        manager.Result = result;
        manager.IsReturn = true;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)        {
            // 检查是否在finally块中使用了return语句，这在.NET IL中是不允许的
            if (local.IsInFinallyBlock)
            {
                throw new Error.CompilerException("在finally块中不允许使用return语句", Position);
            }
            
            // 对于异步函数，我们需要特殊处理返回值
            // 异步函数返回Task<object>，所以需要将返回值包装
            
            // 首先获取返回值类型
            var returnType = returnExpression.OutputType(local);
            
            // 加载返回表达式的值
            returnExpression.LoadIlValue(ilGenerator, local);
            
            // 由于我们无法直接从ILGenerator获取当前方法类型
            // 我们假设所有异步函数都通过AsyncFuncInit生成，其返回类型为Task<object>
            // 所以我们需要检查当前返回值类型是否已经是Task<object>
            if (returnType != typeof(Task<object>))
            {
                // 如果不是Task<object>，则需要将返回值包装为Task<object>
                
                // 将返回值转换为object
                if (returnType != null && returnType != typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Box, returnType);
                }
                
                // 调用Task.FromResult<object>，将返回值包装为Task<object>
                var fromResultMethod = typeof(Task).GetMethod("FromResult", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!.MakeGenericMethod(typeof(object));
                ilGenerator.Emit(OpCodes.Call, fromResultMethod);
            }
            
            // 生成ret指令
            ilGenerator.Emit(OpCodes.Ret);
        }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public Type OutputType(LocalManager local) => returnExpression.OutputType(local)!;

    public override string ToString() => $"return {returnExpression}";
}