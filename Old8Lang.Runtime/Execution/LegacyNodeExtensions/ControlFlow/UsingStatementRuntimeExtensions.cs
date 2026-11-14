using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Concurrency;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// UsingStatement 的解释执行与资源释放辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static class UsingStatementRuntimeExtensions
{
    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    internal static void RunWithGeneratorContext(this UsingStatement node, VariateManager manager)
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
                node.BlockStatement.Accept(new InterpreterVisitor(manager));
            }
            finally
            {
                // 清理资源
                if (node.VariableName is not null)
                {
                    var resource = manager.GetValue(new LangId(node.VariableName));
                    if (resource != null) DisposeResource(resource);
                }
            }
        }
        else
        {
            // 首次进入using语句
            var resource = node.ResourceExpression.Accept(new InterpreterVisitor(manager));

            if (node.VariableName is not null)
            {
                manager.Set(new LangId(node.VariableName), resource);
            }

            // 设置执行路径
            var oldPath = context.ExecutionPath;
            context.ExecutionPath = $"{oldPath}/using_block";

            try
            {
                node.BlockStatement.Accept(new InterpreterVisitor(manager));
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
    internal static void RunStandard(this UsingStatement node, VariateManager manager)
    {
        // 1. 执行资源表达式
        var resource = node.ResourceExpression.Accept(new InterpreterVisitor(manager));

        // 2. 如果有变量名，注册到作用域
        if (node.VariableName is not null)
        {
            manager.Set(new LangId(node.VariableName), resource);
        }

        try
        {
            // 3. 执行using块
            node.BlockStatement.Accept(new InterpreterVisitor(manager));
        }
        finally
        {
            // 4. 自动调用Dispose
            DisposeResource(resource);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    private static void DisposeResource(LangValueType resource)
    {
        // 1. 如果是整数值（资源ID），尝试通过ResourceManager释放
        if (resource is IntLangValue intVal)
        {
            ResourceManager.TryDispose(intVal.Value);
        }
        // 2. 如果是 AnyLangValue（用户自定义类实例），尝试调用 dispose 方法
        else if (resource is AnyLangValue anyValue)
        {
            anyValue.TryDispose();
        }
        // 3. 如果实现了IDisposable接口，直接调用Dispose
        else if (resource is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
