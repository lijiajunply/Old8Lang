using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Concurrency;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// UsingStatement 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class UsingStatement
{
    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    internal void RunWithGeneratorContext(VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 检查是否从using块内恢复
        bool isResumingFromUsingBlock = !string.IsNullOrEmpty(context.ExecutionPath) &&
                                        context.ExecutionPath.Contains("/using_block");

        if (isResumingFromUsingBlock)
        {
            // 从using块内恢复，继续执行
            try
            {
                blockStatement.Accept(new InterpreterVisitor(manager));
            }
            finally
            {
                // 清理资源
                if (variableName is not null)
                {
                    var resource = manager.GetValue(new LangId(variableName));
                    if (resource != null) DisposeResource(resource);
                }
            }
        }
        else
        {
            // 首次进入using语句
            var resource = resourceExpression.Accept(new InterpreterVisitor(manager));

            if (variableName is not null)
            {
                manager.Set(new LangId(variableName), resource);
            }

            // 设置执行路径
            var oldPath = context.ExecutionPath;
            context.ExecutionPath = $"{oldPath}/using_block";

            try
            {
                blockStatement.Accept(new InterpreterVisitor(manager));
            }
            finally
            {
                // 恢复执行路径
                context.ExecutionPath = oldPath;

                // 清理资源
                DisposeResource(resource);
            }
        }
    }
    /// <summary>
    /// 标准模式执行（非生成器）
    /// </summary>
    internal void RunStandard(VariateManager manager)
    {
        // 1. 执行资源表达式
        var resource = resourceExpression.Accept(new InterpreterVisitor(manager));

        // 2. 如果有变量名，注册到作用域
        if (variableName is not null)
        {
            manager.Set(new LangId(variableName), resource);
        }

        try
        {
            // 3. 执行using块
            blockStatement.Accept(new InterpreterVisitor(manager));
        }
        finally
        {
            // 4. 自动调用Dispose
            DisposeResource(resource);
        }
    }
}
