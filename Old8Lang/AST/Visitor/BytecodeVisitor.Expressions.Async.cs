using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 异步表达式
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitAwaitExpression(AwaitExpression node)
    {
        // 生成被 await 的表达式代码（应该返回 Task ID）
        node.Expression.Accept(this);

        // 发出 Await 指令
        Emit(OpCode.Await);

        return null;
    }


    public Instruction? VisitAsyncStreamExpression(AsyncStreamExpression node)
    {
        // 异步流表达式: async { block }
        // 创建一个匿名异步生成器函数来包装块

        // 获取块语句
        var block = GetPrimaryConstructorParameter<BlockStatement>(node, "Block");
        if (block == null)
        {
            return null;
        }

        // 编译为异步生成器函数
        var funcName = $"<async_stream_{GetCurrentPosition()}>";
        var parameters = new List<string>();
        var parameterTypes = new List<string>();
        var defaultValues = new List<object?>();

        // 编译异步生成器函数
        var function = _compiler.CompileAsyncGeneratorFunction(funcName, parameters, parameterTypes, defaultValues, block);

        // 查找函数在字节码文件中的索引
        var funcIndex = _compiler.GetFunctionIndex(funcName);

        // 调用函数（无参数）
        Emit(OpCode.Call, new object[] { 0, funcName });

        return null;
    }


}
